using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class PhotoStyleEditor
{
    [Parameter, EditorRequired] public SaveFile SAV { get; set; } = null!;
    [Parameter] public bool Filters { get; set; }
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    private SaveFile? Loaded;
    private bool PreviousFilters;
    private int PreviousLanguage = -1;
    private int SelectedFrame;
    private int SelectedFilter;
    private string Search = string.Empty;
    private ItemInfo[] Entries = [];
    private int SelectedStyle => Filters ? SelectedFilter : SelectedFrame;
    private string Preview => SpriteUtil.GetPhotoStyle(SelectedStyle, Filters, true)!;
    private string Status => SAV.PhotoStyles == null ? "Unlocks aren’t available for this save version." : SAV.PhotoStyles.IsUnlocked(SelectedStyle, Filters) ? "Unlocked" : "Locked";

    protected override bool ShouldRender() => Active;

    protected override void OnParametersSet()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            SelectedFrame = SelectedFilter = 0;
            Search = string.Empty;
        }
        if (PreviousFilters != Filters)
            Search = string.Empty;
        if (PreviousLanguage != Language || PreviousFilters != Filters || Entries.Length == 0)
            LoadStyles();
        PreviousLanguage = Language;
        PreviousFilters = Filters;
    }

    private void LoadStyles()
    {
        var search = Search.Trim();
        var choices = Filters ? GameInfo.Strings.PhotoFilterDataSource : GameInfo.Strings.PhotoFrameDataSource;
        Entries = choices.Where(z => z.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) || z.Value.ToString().Contains(search)).ToArray();
    }

    private void SelectStyle(int index)
    {
        if (!Entries.Any(z => z.Value == index))
            return;
        if (Filters)
            SelectedFilter = index;
        else
            SelectedFrame = index;
    }

    private void TB_Search_Changed(string? value)
    {
        Search = value ?? string.Empty;
        LoadStyles();
    }
}
