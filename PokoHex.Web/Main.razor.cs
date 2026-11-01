using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PokoHex.Core;
using PokoHex.Web.Controls;

namespace PokoHex.Web;

public partial class Main : IAsyncDisposable
{
    private static readonly string[] Tabs = ["Player", "Items", "Villagers", "Map"];
    private static readonly string[] LanguageNames = ["English", "日本語", "Deutsch", "Español", "Français", "Italiano", "한국어", "简体中文", "繁體中文"];
    private SaveFolder? SAV;
    private InventoryEditor? Editor_Items;
    private VillagerEditor? Editor_Villagers;
    private MapEditor? Editor_Map;
    private ElementReference EditorWindow;
    private DotNetObjectReference<Main>? DropReference;
    private string PlayerName = string.Empty;
    private string SaveName = string.Empty;
    private string SelectedTab = "Player";
    private string Status = "Open or drop a save folder or ZIP to begin.";
    private string Error = string.Empty;
    private int Language;
    private bool Busy;
    private bool MapOpened;
    private bool? BrowserDirty;
    private RenderState? LastRender;
    private readonly record struct RenderState(SaveFolder? Save, string Name, string SaveName, string Tab,
        string Status, string Error, int Language, bool Busy, bool MapOpened, bool Dirty);
    private bool HasChanges => SAV != null && (PlayerName != SAV.Common.PlayerName ||
        Editor_Items?.HasChanges == true || Editor_Villagers?.HasChanges == true || Editor_Map?.HasChanges == true || SAV.IsEdited);

    private RenderState GetRenderState() => new(SAV, PlayerName, SaveName, SelectedTab, Status, Error, Language, Busy, MapOpened, HasChanges);
    protected override bool ShouldRender() => LastRender != GetRenderState();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            DropReference = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("pokohexDrop.initialize", EditorWindow, DropReference, SaveArchive.MaximumFiles);
        }
        var state = GetRenderState();
        LastRender = state;
        if (BrowserDirty != state.Dirty)
        {
            await JS.InvokeVoidAsync("pokohex.setDirty", state.Dirty);
            BrowserDirty = state.Dirty;
        }
    }

    [JSInvokable]
    public void DropFailed(string message) => ShowError(message);

    public async ValueTask DisposeAsync()
    {
        if (DropReference == null)
            return;
        try { await JS.InvokeVoidAsync("pokohexDrop.dispose", EditorWindow); }
        catch (JSDisconnectedException) { }
        finally { DropReference?.Dispose(); }
    }

    private async Task<bool> ConfirmDiscard() => !HasChanges || await JS.InvokeAsync<bool>("confirm", "Discard changes to the opened save?");

    private async Task OpenFolder(InputFileChangeEventArgs e)
    {
        if (Busy || !await ConfirmDiscard())
            return;
        await RunBusy("Reading save folder…", async () =>
        {
            var files = e.GetMultipleFiles(SaveArchive.MaximumFiles);
            SaveArchive.ValidateSize(files.Count, files.Select(z => z.Size));
            var paths = await JS.InvokeAsync<string[]>("pokohex.getPaths", "open-folder");
            if (paths.Length != files.Count)
                throw new InvalidDataException("The browser could not read the folder paths. Open a ZIP instead.");
            var data = new List<KeyValuePair<string, byte[]>>();
            for (int i = 0; i < files.Count; i++)
            {
                await using var stream = files[i].OpenReadStream(SaveArchive.MaximumFileSize);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                data.Add(new(paths[i], buffer.ToArray()));
            }
            LoadSave(SaveArchive.ReadFiles(data), paths[0].Split('/')[0]);
        });
    }

    private async Task OpenZip(InputFileChangeEventArgs e)
    {
        if (Busy || !await ConfirmDiscard())
            return;
        await RunBusy("Reading save ZIP…", async () =>
        {
            await using var input = e.File.OpenReadStream(SaveArchive.MaximumTotalSize);
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer);
            buffer.Position = 0;
            LoadSave(SaveArchive.ReadZip(buffer), Path.GetFileNameWithoutExtension(e.File.Name));
        });
    }

    private void LoadSave(SaveFolder save, string name)
    {
        SAV = save;
        SaveName = name;
        PlayerName = save.Common.PlayerName;
        Editor_Items = null;
        Editor_Villagers = null;
        Editor_Map = null;
        MapOpened = false;
        SelectedTab = "Player";
        Status = $"Loaded: {PlayerName}.";
    }

    private async Task Menu_Export_Click()
    {
        if (SAV == null || Busy)
            return;
        await RunBusy("Verifying and exporting save…", async () =>
        {
            // Validate the name before applying any pending control values.
            if (PlayerName != SAV.Common.PlayerName)
            {
                var check = SaveFile.Read(SAV.Common.Write());
                check.PlayerName = PlayerName;
            }
            Editor_Map?.SaveChanges();
            Editor_Items?.SaveChanges();
            Editor_Villagers?.SaveChanges();
            if (PlayerName != SAV.Common.PlayerName)
                SAV.Common.PlayerName = PlayerName;
            var files = SAV.ExportFiles();
            using var output = SaveArchive.WriteZip(files);
            using var reference = new DotNetStreamReference(output);
            await JS.InvokeVoidAsync("pokohex.download", "PokoHex-edited.zip", reference);
            var tab = SelectedTab;
            LoadSave(SaveFolder.ReadFiles(files), SaveName);
            SelectedTab = tab;
            MapOpened = tab == "Map";
            Status = "Exported and verified: PokoHex-edited.zip.";
        });
    }

    private void SelectTab(string tab)
    {
        SelectedTab = tab;
        MapOpened |= tab == "Map";
    }

    private void Menu_Language_Changed(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var language) || (uint)language >= GameLanguage.LanguageCount)
            return;
        Language = language;
        GameInfo.SetLanguage2Char(language);
    }

    private async Task RunBusy(string message, Func<Task> action)
    {
        Busy = true;
        Error = string.Empty;
        var previous = Status;
        Status = message;
        StateHasChanged();
        await Task.Delay(30);
        try { await action(); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or InvalidOperationException or JSException or OverflowException)
        {
            ShowError(ex.Message);
            Status = previous;
        }
        finally { Busy = false; }
    }

    private void Controls_Changed() => StateHasChanged();
    private void ShowError(string message)
    {
        Error = message;
        StateHasChanged();
    }
}
