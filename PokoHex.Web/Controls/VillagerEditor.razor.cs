using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class VillagerEditor
{
    [Parameter, EditorRequired] public SaveFile SAV { get; set; } = null!;
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private Task NotifyChanged() => Changed.InvokeAsync();

    private SaveFile? Loaded;
    private int PreviousLanguage = -1;
    private int SelectedVillager = -1;
    private int Area = -1;
    private int[] Areas = [];
    private string Search = string.Empty;
    private int Friendship;
    private int FriendshipMaximum;
    private bool NameConversation;
    private Villager[] Entries = [];
    private GameStrings? Catalog;
    private readonly Dictionary<int, string> SearchLabels = new();
    private Villager[] SortedVillagers = [];
    private int TotalVillagers;
    private Villager? Selected => SelectedVillager < 0 ? null : SAV.Villagers[SelectedVillager];
    public bool HasChanges => Selected is { } villager && (Friendship != villager.Friendship || NameConversation != villager.NameConversationFinished);

    protected override bool ShouldRender() => Active;

    protected override void OnParametersSet()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            Catalog = null;
            SelectedVillager = -1;
            Search = string.Empty;
            Area = -1;
            Areas = SAV.Villagers.Where(z => !z.IsEmpty).Select(z => (int)z.Home.StageId).Distinct().Order().ToArray();
            TotalVillagers = SAV.Villagers.Count(z => !z.IsEmpty);
            LoadVillagers();
        }
        if (PreviousLanguage != Language)
        {
            Search = string.Empty;
            // Reorder translated labels without replacing the pending field values.
            Entries = FilterVillagers();
        }
        PreviousLanguage = Language;
    }

    private Villager[] FilterVillagers()
    {
        if (Catalog != GameInfo.Strings)
        {
            SortedVillagers = SAV.Villagers.Where(z => !z.IsEmpty)
                .OrderBy(z => SpriteUtil.GetVillagerName(z.Index)).ThenBy(z => z.Index).ToArray();
            SearchLabels.Clear();
            foreach (var villager in SortedVillagers)
                SearchLabels.Add(villager.Index, $"{SpriteUtil.GetVillagerName(villager.Index)} ({villager.Index:D4})");
            Catalog = GameInfo.Strings;
        }
        return Area < 0 && Search.Length == 0 ? SortedVillagers : SortedVillagers
            .Where(z => (Area < 0 || z.Home.StageId == Area) && SearchLabels[z.Index].Contains(Search, StringComparison.CurrentCultureIgnoreCase)).ToArray();
    }

    private void LoadVillagers()
    {
        Entries = FilterVillagers();
        if (!Entries.Any(z => z.Index == SelectedVillager))
            SelectedVillager = Entries.FirstOrDefault()?.Index ?? -1;
        LoadVillager();
    }

    private void LoadVillager()
    {
        if (Selected is not { } villager)
            return;
        Friendship = villager.Friendship;
        FriendshipMaximum = Math.Max(Villager.FriendshipTierMaximum, Friendship);
        NameConversation = villager.NameConversationFinished;
    }

    public void SaveChanges()
    {
        if (Selected is not { } villager)
            return;
        if (Friendship < 0 || Friendship > FriendshipMaximum)
            throw new ArgumentException($"Enter friendship points between 0 and {FriendshipMaximum}.");
        if (Friendship != villager.Friendship)
            villager.Friendship = (ushort)Friendship;
        if (NameConversation != villager.NameConversationFinished)
            villager.NameConversationFinished = NameConversation;
    }

    private void SelectVillager(int index)
    {
        try { SaveChanges(); SelectedVillager = index; LoadVillager(); }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private void TB_Search_Changed(string? value) => FilterChanged(() => Search = value ?? string.Empty);
    private void CB_Area_Changed(int area) => FilterChanged(() => Area = area);

    private void FilterChanged(Action change)
    {
        try { SaveChanges(); change(); LoadVillagers(); }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private void B_Set_Click()
    {
        try { SaveChanges(); }
        catch (ArgumentException ex) { Error(ex.Message); }
    }
}
