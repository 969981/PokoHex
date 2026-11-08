using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class FlagEditor
{
    [Parameter, EditorRequired] public SaveFile SAV { get; set; } = null!;
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private SaveFile? Loaded;
    private int PreviousLanguage = -1;
    private bool WasActive;
    private string Search = string.Empty;
    private bool KnownOnly;
    private int SelectedFlag = -1;
    private bool Enabled;
    private bool LoadedValue;
    private int[] Entries = [];
    private FunctionFlags? Flags => SAV.Flags;
    public bool HasChanges => Flags != null && SelectedFlag > 0 && Enabled != LoadedValue;

    protected override bool ShouldRender() => Active;
    private Task NotifyChanged() => Changed.InvokeAsync();

    protected override void OnParametersSet()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            SelectedFlag = -1;
            Search = string.Empty;
            KnownOnly = false;
            LoadFlags();
        }
        if (PreviousLanguage != Language)
        {
            SaveChanges();
            LoadFlags();
        }
        if (Active && !WasActive)
        {
            SaveChanges();
            LoadFlag();
        }
        PreviousLanguage = Language;
        WasActive = Active;
    }

    private void LoadFlags()
    {
        var search = Search.Trim();
        Entries = Flags == null ? [] : Enumerable.Range(1, FunctionFlags.Count - 1)
            .Where(i => (!KnownOnly || FunctionFlagInfo.IsKnown(i)) &&
                (FunctionFlagInfo.GetName(i).Contains(search, StringComparison.CurrentCultureIgnoreCase) || i.ToString().Contains(search))).ToArray();
        if (!Entries.Contains(SelectedFlag))
            SelectedFlag = Entries.FirstOrDefault(-1);
        LoadFlag();
    }

    private void LoadFlag()
    {
        LoadedValue = Flags != null && SelectedFlag > 0 && Flags.GetFlag(SelectedFlag);
        Enabled = LoadedValue;
    }

    public void SaveChanges()
    {
        if (!HasChanges)
            return;
        Flags!.SetFlag(SelectedFlag, Enabled);
        LoadedValue = Enabled;
    }

    private async Task SelectFlag(int index)
    {
        if (!Entries.Contains(index))
            return;
        try
        {
            SaveChanges();
            SelectedFlag = index;
            LoadFlag();
            await NotifyChanged();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private Task TB_Search_Changed(string? value) => FilterChanged(() => Search = value ?? string.Empty);
    private Task CHK_Known_Changed(bool value) => FilterChanged(() => KnownOnly = value);

    private async Task FilterChanged(Action change)
    {
        try
        {
            SaveChanges();
            change();
            LoadFlags();
            await NotifyChanged();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private async Task B_Set_Click()
    {
        try
        {
            SaveChanges();
            await NotifyChanged();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }
}
