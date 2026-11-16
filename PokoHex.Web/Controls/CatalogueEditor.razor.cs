using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class CatalogueEditor
{
    [Parameter, EditorRequired] public SaveFile SAV { get; set; } = null!;
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private SaveFile? Loaded;
    private int PreviousLanguage = -1;
    private CatalogueType View;
    private string Search = string.Empty;
    private readonly int[] Selection = [-1, -1, -1, -1];
    private ItemInfo[] Entries = [];
    private ItemInfo[] Filtered = [];
    private int SelectedEntry => Selection[(int)View];
    private Catalogue? Catalogue => SAV.Catalogue;
    private bool CanUnlock => Catalogue != null && SelectedEntry > 0 && Catalogue.CanUnlock(View, SelectedEntry);
    private bool CanPartial => Catalogue != null && SelectedEntry > 0 && Catalogue.CanUnlock(View, SelectedEntry, true);
    private bool CanUnlockAll => Catalogue != null && Entries.Any(z => Catalogue.CanUnlock(View, z.Value));
    private int Complete => Catalogue == null ? 0 : Entries.Count(z => Catalogue.GetState(View, z.Value) == 2);
    protected override bool ShouldRender() => Active;

    protected override void OnParametersSet()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            Array.Fill(Selection, -1);
            Search = string.Empty;
            LoadEntries();
        }
        if (PreviousLanguage != Language)
            LoadEntries();
        PreviousLanguage = Language;
    }

    private void LoadEntries()
    {
        Entries = CatalogueInfo.GetEntries(View);
        var search = Search.Trim();
        Filtered = Catalogue == null ? [] : Entries.Where(z => z.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) || z.Value.ToString().Contains(search)).ToArray();
        if (!Filtered.Any(z => z.Value == SelectedEntry))
            Selection[(int)View] = Filtered.Length == 0 ? -1 : Filtered[0].Value;
    }

    private void CB_View_Changed(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var index) || index is < 0 or > 3)
            return;
        View = (CatalogueType)index;
        Search = string.Empty;
        LoadEntries();
    }

    private void TB_Search_Changed(string? value)
    {
        Search = value ?? string.Empty;
        LoadEntries();
    }

    private void SelectEntry(int index)
    {
        if (Filtered.Any(z => z.Value == index))
            Selection[(int)View] = index;
    }

    private async Task Apply(Action<Catalogue> change)
    {
        if (Catalogue == null)
            return;
        try
        {
            change(Catalogue);
            await Changed.InvokeAsync();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private Task B_Unlock_Click() => Apply(z => z.Unlock(View, SelectedEntry));
    private Task B_Partial_Click() => Apply(z => z.Unlock(View, SelectedEntry, true));
    private Task B_All_Click() => Apply(z => z.UnlockAll(View));
    private Task B_Undo_Click() => Apply(z => z.UndoEdit());
    private Task B_Redo_Click() => Apply(z => z.RedoEdit());
}
