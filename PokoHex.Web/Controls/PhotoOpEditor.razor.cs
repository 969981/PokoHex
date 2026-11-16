using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class PhotoOpEditor
{
    [Parameter, EditorRequired] public SaveFolder SAV { get; set; } = null!;
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private SaveFolder? Loaded;
    private PhotoOps? Photos;
    private int PreviousLanguage = -1;
    private int SelectedOp = -1;
    private int SelectedPhoto = -1;
    private PhotoEntry[] Captures = [];
    private int View;
    private bool StylesOpened;
    private int[] Entries = [];
    private bool Loading;
    private bool WasActive;
    private string Search = string.Empty;
    private string LoadError = string.Empty;
    private string Preview = string.Empty;

    protected override bool ShouldRender() => Active;

    protected override async Task OnParametersSetAsync()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            Photos = null;
            SelectedOp = -1;
            SelectedPhoto = -1;
            Captures = [];
            Search = string.Empty;
            Preview = string.Empty;
            LoadError = string.Empty;
            Entries = [];
            Loading = true;
            StateHasChanged();
            await Task.Delay(30);
            try { Photos = SAV.GetPhotoOps(); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
            {
                LoadError = ex.Message;
            }
            Loading = false;
            LoadEntries();
        }
        if (PreviousLanguage != Language)
            LoadEntries();
        else if (Active && !WasActive)
            LoadPreview();
        if (!Active)
            Preview = string.Empty;
        PreviousLanguage = Language;
        WasActive = Active;
    }

    private void LoadEntries()
    {
        var search = Search.Trim();
        Entries = Photos == null ? [] : GameInfo.Strings.PhotoOpDataSource
            .Where(z => z.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) || z.Value.ToString().Contains(search))
            .Select(z => (int)z.Value).ToArray();
        if (!Entries.Contains(SelectedOp))
            SelectedOp = Entries.FirstOrDefault(-1);
        LoadCaptures();
    }

    private void LoadCaptures()
    {
        Captures = Photos == null || SelectedOp < 1 ? [] : Photos.GetPhotos(SelectedOp).OrderBy(z => z.Kind == 4 ? 0 : 1).ThenBy(z => z.Index).ToArray();
        if (!Captures.Any(z => z.Index == SelectedPhoto))
            SelectedPhoto = Captures.FirstOrDefault(z => !Photos!.Screenshots.GetImage(z).IsEmpty)?.Index ?? Captures.FirstOrDefault()?.Index ?? -1;
        LoadPreview();
    }

    private ReadOnlyMemory<byte> SelectedImage => Photos == null || Captures.FirstOrDefault(z => z.Index == SelectedPhoto) is not { } entry
        ? ReadOnlyMemory<byte>.Empty : Photos.Screenshots.GetImage(entry);

    private void LoadPreview()
    {
        Preview = string.Empty;
        if (Photos == null || SelectedOp < 1 || !Active || View != 0)
            return;
        var data = SelectedImage;
        if (!data.IsEmpty)
            Preview = "data:image/jpeg;base64," + Convert.ToBase64String(data.Span);
    }

    private void SelectPhoto(int index)
    {
        if (!Entries.Contains(index))
            return;
        SelectedOp = index;
        LoadCaptures();
    }

    private void SelectCapture(int index)
    {
        if (!Captures.Any(z => z.Index == index))
            return;
        SelectedPhoto = index;
        LoadPreview();
    }

    private void CB_View_Changed(int value)
    {
        if (value is < 0 or > 2)
            return;
        View = value;
        StylesOpened |= value != 0;
        LoadPreview();
    }

    private async Task B_ExportPhoto_Click()
    {
        var data = SelectedImage;
        if (data.IsEmpty)
            return;
        try
        {
            using var stream = new MemoryStream(data.ToArray(), false);
            using var reference = new DotNetStreamReference(stream);
            var number = Array.FindIndex(Captures, z => z.Index == SelectedPhoto) + 1;
            await JS.InvokeVoidAsync("pokohex.download", $"PhotoOp-{SelectedOp}-{number}.jpg", reference, "image/jpeg");
        }
        catch (Exception ex) when (ex is IOException or JSException or ArgumentException)
        {
            Error(ex.Message);
        }
    }

    private void TB_Search_Changed(string? value)
    {
        Search = value ?? string.Empty;
        LoadEntries();
    }

    private async Task B_Reset_Click()
    {
        if (Photos == null || SelectedOp < 1)
            return;
        try { Photos.Reset(SelectedOp); }
        catch (ArgumentException ex) { Error(ex.Message); return; }
        LoadEntries();
        await Changed.InvokeAsync();
    }

    private async Task B_Undo_Click()
    {
        Photos?.UndoEdit();
        LoadEntries();
        await Changed.InvokeAsync();
    }

    private async Task B_Redo_Click()
    {
        Photos?.RedoEdit();
        LoadEntries();
        await Changed.InvokeAsync();
    }
}
