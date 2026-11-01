using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class InventoryEditor
{
    [Parameter, EditorRequired] public SaveFile SAV { get; set; } = null!;
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private Task NotifyChanged() => Changed.InvokeAsync();

    private SaveFile? Loaded;
    private int SelectedSlot;
    private int InventoryStart;
    private ushort ItemId;
    private int Count;
    private int Revision;
    private int ChoiceRevision;
    private GameStrings? Catalog;
    private HashSet<ushort> CatalogIds = [];
    private (SaveFile? Save, GameStrings? Strings, int Revision, ushort Extra) ChoiceState;
    private ItemInfo[] CachedChoices = [];
    private int InventoryCount => InventoryStart == 0 ? SaveLayout.MainInventoryCount : SAV.Inventory.Length - InventoryStart;
    private ItemInfo[] Choices
    {
        get
        {
            var strings = GameInfo.Strings;
            if (Catalog != strings)
            {
                Catalog = strings;
                CatalogIds = strings.ItemDataSource.Select(z => z.Value).ToHashSet();
            }
            var extra = CatalogIds.Contains(ItemId) ? (ushort)0 : ItemId;
            var state = (SAV, strings, ChoiceRevision, extra);
            if (ChoiceState == state)
                return CachedChoices;
            CachedChoices = strings.ItemDataSource.Concat(SAV.Inventory.Select(z => z.ItemId).Append(ItemId).Distinct()
                    .Where(id => !CatalogIds.Contains(id)).Select(id => new ItemInfo(id, SpriteUtil.GetItemName(id))))
                .OrderBy(z => z.Value != 0).ThenBy(z => z.Text).ThenBy(z => z.Value).ToArray();
            ChoiceState = state;
            return CachedChoices;
        }
    }
    public bool HasChanges => SAV.Inventory[SelectedSlot].ItemId != ItemId || SAV.Inventory[SelectedSlot].Count != Count;

    protected override bool ShouldRender() => Active;

    protected override void OnParametersSet()
    {
        if (Loaded == SAV)
            return;
        Loaded = SAV;
        Revision = 0;
        ChoiceRevision = 0;
        SelectedSlot = InventoryStart = 0;
        LoadSlot();
    }

    private void LoadSlot()
    {
        var item = SAV.Inventory[SelectedSlot];
        ItemId = item.ItemId;
        Count = item.Count;
    }

    public void SaveChanges()
    {
        if (Count is < 0 or > Item.MaxCount)
            throw new ArgumentException($"Enter a count between 0 and {Item.MaxCount}.");
        if (HasChanges)
        {
            var previous = SAV.Inventory[SelectedSlot].ItemId;
            SAV.Inventory[SelectedSlot].Set(ItemId, Count);
            Revision++;
            if (previous != SAV.Inventory[SelectedSlot].ItemId)
                ChoiceRevision++;
        }
    }

    private void SelectSlot(int slot)
    {
        try { SaveChanges(); SelectedSlot = slot; LoadSlot(); }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private void CB_Inventory_Changed(int start)
    {
        try
        {
            SaveChanges();
            InventoryStart = start == SaveLayout.MainInventoryCount ? SaveLayout.MainInventoryCount : 0;
            SelectedSlot = InventoryStart;
            LoadSlot();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private void CB_Item_Changed(ushort id)
    {
        ItemId = id;
        if (id == 0)
            Count = 0;
        else if (Count == 0)
            Count = 1;
    }

    private void B_Set_Click()
    {
        try { SaveChanges(); LoadSlot(); }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private void B_Clear_Click()
    {
        if (SAV.Inventory[SelectedSlot].ItemId != 0)
            ChoiceRevision++;
        SAV.Inventory[SelectedSlot].Set(0, 0);
        Revision++;
        LoadSlot();
    }
}
