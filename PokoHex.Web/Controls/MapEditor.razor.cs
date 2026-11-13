using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class MapEditor
{
    [Parameter, EditorRequired] public SaveFolder SAV { get; set; } = null!;
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private Task NotifyChanged() => Changed.InvokeAsync();

    private SaveFolder? Loaded;
    private StageSave? Stage;
    private MapPart? SelectedPart;
    private ElementReference PB_Map;
    private MapPart[] Entries = [];
    private MapPartInfo[] Choices = [];
    private int Revision;
    private PlacementState? PreviewState;
    private string? PreviewError;
    private ViewState? DrawingState;
    private MapPart[] DrawnParts = [];
    private string GroundPath = string.Empty;
    private (StageSave? Stage, int Revision, GameStrings? Strings) PartState;
    private readonly Dictionary<int, string> PartLabels = new();
    private MapPart[] SortedParts = [];
    private RenderState? LastRender;
    private bool RefreshControls;
    private readonly record struct PlacementState(StageSave? Stage, int Revision, MapPart? Part, ushort Variation,
        int X, int Y, int Z, int Direction);
    private readonly record struct ViewState(StageSave? Stage, int Revision, int Chunk, int Height, bool AllHeights, int Interior, bool VisibleOnly);
    private readonly record struct RenderState(SaveFolder Save, PlacementState Placement, ViewState View,
        int Area, string Search, GameStrings Strings, bool Loading, string Error);
    private int PreviousLanguage = -1;
    private int Area;
    private int Chunk;
    private int Height;
    private int X;
    private int Y;
    private int Z;
    private int Direction;
    private ushort Variation;
    private bool AllHeights = true;
    private bool VisibleOnly;
    private int InteriorId;
    private MapInterior? Interior => Stage?.Interiors.FirstOrDefault(z => z.Id == InteriorId && z.Chunk == Chunk);
    private bool CanEdit => SelectedPart != null && Stage?.CanEdit(SelectedPart) == true;
    private bool Loading;
    private string Search = string.Empty;
    private string LoadError = "No areas are available.";
    private int OriginX => (Chunk & 63) * 32;
    private int OriginZ => (Chunk >> 6) * 32;
    private int Layer => Math.Clamp(Height, 0, 127);
    public bool HasChanges => SelectedPart is { } part && (Variation != part.Variation || X != part.X || Y != part.Y || Z != part.Z || Direction != part.Direction);
    private string? EditError
    {
        get
        {
            var state = GetPlacementState();
            if (PreviewState == state)
                return PreviewError;
            PreviewError = Stage == null || SelectedPart == null ? "Select an object." : Stage.GetEditError(SelectedPart, Variation, X, Y, Z, Direction);
            PreviewState = state;
            return PreviewError;
        }
    }
    private MapBounds? PendingBounds => !CanEdit && SelectedPart != null ? GetVisibleBounds(SelectedPart) :
        Direction is >= 0 and <= 3 ? MapPartInfo.Get(Variation).GetBounds(X, Y, Z, Direction) : null;
    private MapPart[] VisibleParts { get { PrepareDrawing(); return DrawnParts; } }

    private PlacementState GetPlacementState() => new(Stage, Revision, SelectedPart, Variation, X, Y, Z, Direction);
    private ViewState GetViewState() => new(Stage, Revision, Chunk, Height, AllHeights, InteriorId, VisibleOnly);
    private RenderState GetRenderState() => new(SAV, GetPlacementState(), GetViewState(), Area, Search, GameInfo.Strings, Loading, LoadError);
    protected override bool ShouldRender() => Active && (RefreshControls || LastRender != GetRenderState());
    protected override void OnAfterRender(bool firstRender)
    {
        LastRender = GetRenderState();
        RefreshControls = false;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            Area = SAV.StageIds.FirstOrDefault();
            await LoadArea();
        }
        if (PreviousLanguage != Language)
        {
            Search = string.Empty;
            LoadParts();
            BindChoices();
        }
        PreviousLanguage = Language;
    }

    private async Task LoadArea()
    {
        Stage = null;
        Revision++;
        SelectedPart = null;
        InteriorId = 0;
        Entries = [];
        if (!SAV.StageIds.Contains(Area))
            return;
        Loading = true;
        StateHasChanged();
        await Task.Delay(30);
        try
        {
            Stage = SAV.GetStage(Area);
            Chunk = Stage.Chunks[0];
            Search = string.Empty;
            LoadParts();
            SelectedPart = Entries.FirstOrDefault(Stage.CanEdit) ?? Entries.FirstOrDefault();
            LoadPart(true);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
        {
            LoadError = ex.Message;
        }
        finally { Loading = false; }
    }

    private string PartText(MapPart part)
    {
        PrepareParts();
        return PartLabels[part.Index];
    }

    private void PrepareParts()
    {
        var state = (Stage, Revision, GameInfo.Strings);
        if (PartState == state)
            return;
        PartLabels.Clear();
        foreach (var part in Stage?.Parts ?? [])
            PartLabels.Add(part.Index, $"{SpriteUtil.GetItemName(part.Info.ItemId)} [{part.X}, {part.Y}, {part.Z}] (#{part.Index})");
        SortedParts = Stage?.Parts.OrderBy(z => SpriteUtil.GetItemName(z.Info.ItemId)).ThenBy(z => z.Index).ToArray() ?? [];
        PartState = state;
    }

    private void LoadParts()
    {
        PrepareParts();
        Entries = SortedParts.Where(z => (Search.Length == 0 || PartLabels[z.Index].Contains(Search, StringComparison.CurrentCultureIgnoreCase)) &&
            (!VisibleOnly && Interior == null || IsVisible(z))).ToArray();
    }

    private void BindChoices() => Choices = SelectedPart == null ? [] : MapPartInfo.All.ToArray().Where(z => z.CanEdit || z.Value == SelectedPart.Variation)
        .OrderBy(z => SpriteUtil.GetItemName(z.ItemId)).ThenBy(z => z.Value).ToArray();

    private void LoadPart(bool center)
    {
        BindChoices();
        if (SelectedPart is not { } part)
            return;
        Variation = part.Variation;
        X = part.X;
        Y = part.Y;
        Z = part.Z;
        Direction = part.Direction;
        if (center)
        {
            Chunk = part.Chunk;
            Height = part.Y;
            InteriorId = Stage?.GetInterior(part)?.Id ?? 0;
            LoadParts();
        }
    }

    private void CB_Chunk_Changed(int chunk)
    {
        if (Stage == null || !Stage.Chunks.Contains(chunk) || !TrySaveChanges())
            return;
        Chunk = chunk;
        InteriorId = Stage.Interiors.FirstOrDefault(z => z.Chunk == chunk)?.Id ?? 0;
        if (Interior is { } room)
            Height = room.Floor + 1;
        LoadParts();
        if (Interior != null || VisibleOnly)
        {
            SelectedPart = Entries.FirstOrDefault();
            LoadPart(false);
        }
    }

    private void CB_Interior_Changed(int id)
    {
        if (Stage == null || !Stage.Interiors.Any(z => z.Id == id && z.Chunk == Chunk) || !TrySaveChanges())
            return;
        InteriorId = id;
        Height = Interior!.Floor + 1;
        LoadParts();
        SelectedPart = Entries.FirstOrDefault();
        LoadPart(false);
    }

    private void B_Interiors_Click()
    {
        var room = Stage?.Interiors.FirstOrDefault();
        if (room != null)
            CB_Chunk_Changed(room.Chunk);
    }

    private async Task View_Changed()
    {
        if (TrySaveChanges())
            LoadParts();
        await NotifyChanged();
    }

    public void SaveChanges()
    {
        if (Stage != null && SelectedPart != null && HasChanges)
        {
            // SetPart always performs the full Core validation; only the preview is cached.
            Stage.SetPart(SelectedPart, Variation, X, Y, Z, Direction);
            Revision++;
            LoadParts();
        }
    }

    private bool TrySaveChanges()
    {
        try { SaveChanges(); return true; }
        catch (ArgumentException ex) { RefreshControls = true; Error(ex.Message); return false; }
    }

    private async Task CB_Area_Changed(int area)
    {
        if (Loading || !TrySaveChanges())
            return;
        Area = area;
        await LoadArea();
    }

    private void SelectPart(MapPart part)
    {
        if (!TrySaveChanges())
            return;
        SelectedPart = part;
        LoadPart(true);
    }

    private void TB_Search_Changed(string? value)
    {
        if (!TrySaveChanges())
            return;
        Search = value ?? string.Empty;
        LoadParts();
        if (!Entries.Contains(SelectedPart))
        {
            SelectedPart = Entries.FirstOrDefault();
            LoadPart(true);
        }
    }

    private void B_Set_Click()
    {
        if (!TrySaveChanges())
            return;
        LoadPart(false);
    }

    private void B_Undo_Click() { Stage?.UndoEdit(); Revision++; LoadParts(); LoadPart(false); }
    private void B_Redo_Click() { Stage?.RedoEdit(); Revision++; LoadParts(); LoadPart(false); }

    private async Task PB_Map_MouseDown(MouseEventArgs e)
    {
        if (Stage == null || e.Button is not (0 or 2))
            return;
        var point = await JS.InvokeAsync<int[]>("pokohex.mapPoint", PB_Map, e.ClientX, e.ClientY);
        if (point.Length != 2 || point.Any(z => z is < 0 or > 31))
            return;
        var x = OriginX + point[0];
        var z = OriginZ + point[1];
        if (e.Button == 2 && CanEdit)
        {
            X = x;
            Z = z;
        }
        else if (e.Button == 0 && TrySaveChanges())
        {
            var part = VisibleParts.LastOrDefault(p => GetVisibleBounds(p).Contains(x, AllHeights ? p.Y : Layer, z));
            if (part == null)
                return;
            Search = string.Empty;
            SelectedPart = part;
            LoadParts();
            LoadPart(false);
        }
        await NotifyChanged();
    }

    private MapBounds GetVisibleBounds(MapPart part) => Stage!.GetDisplayBounds(part);
    private (int X, int Z, int Width, int Height) GetRectangle(MapBounds bounds) => ((bounds.X - OriginX) * 16, (bounds.Z - OriginZ) * 16, bounds.Width * 16, bounds.Depth * 16);

    private string GetGroundPath() { PrepareDrawing(); return GroundPath; }

    private void PrepareDrawing()
    {
        var state = GetViewState();
        if (DrawingState == state)
            return;
        DrawnParts = Stage?.Parts.Where(IsVisible).OrderBy(z => z.Y).ThenBy(z => z.Index).ToArray() ?? [];
        var path = new StringBuilder();
        if (Stage != null)
        {
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
            {
                var layer = Interior is { } room && AllHeights ? Math.Clamp(Layer, room.Floor + 1, room.Ceiling - 1) : Layer;
                if (Stage.GetCell(OriginX + x, layer, OriginZ + z) != 0)
                    path.Append($"M{x * 16} {z * 16}h16v16h-16z");
            }
        }
        GroundPath = path.ToString();
        DrawingState = state;
    }

    private bool IsVisible(MapPart part)
    {
        var bounds = GetVisibleBounds(part);
        if (Interior is { } room && (!room.Contains(part) || part.Y <= room.Floor || part.Y >= room.Ceiling || part.Info.ItemId == 0))
            return false;
        return bounds.Intersects(new(OriginX, 0, OriginZ, 32, 128, 32)) &&
            (AllHeights || Layer >= bounds.Y && Layer < bounds.Y + bounds.Height);
    }
}
