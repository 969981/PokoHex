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
    private ushort Color1;
    private ushort Color2;
    private ushort Color3;
    private ushort Pattern;
    private int PaintArea;
    private bool GalleryOpen;
    private int SwatchCount => PaintArea == 3 ? AllowsPattern ? ItemPaintInfo.PatternCount : 0 : ItemPaintInfo.ColorCount;
    private int SelectedSwatch => PaintArea switch { 0 => Pattern == 0 ? Color1 : -1, 1 => Color2, 2 => Color3, _ => Pattern };
    private ItemPaint LoadedPaint;
    private ItemPaint Paint => new(Color1, Color2, Color3, Pattern);
    private int PaintCount => SAV.Inventory[SelectedSlot].PaintEditable ? ItemPaintInfo.GetColorCount(ItemId) : 0;
    private bool AllowsPattern => SAV.Inventory[SelectedSlot].PaintEditable && ItemPaintInfo.AllowsPattern(ItemId);
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
    public bool HasChanges => SAV.Inventory[SelectedSlot].ItemId != ItemId || SAV.Inventory[SelectedSlot].Count != Count ||
        PaintCount != 0 && Paint != LoadedPaint;

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
        LoadPaint(item);
    }

    private void LoadPaint(Item item)
    {
        LoadedPaint = item.Paint;
        Color1 = LoadedPaint.Color1;
        Color2 = LoadedPaint.Color2;
        Color3 = LoadedPaint.Color3;
        Pattern = LoadedPaint.Pattern;
        if (PaintArea == 3 ? !AllowsPattern && Pattern == 0 : PaintArea >= PaintCount)
            PaintArea = 0;
    }

    private string GetSwatchName(int value) => PaintArea == 3
        ? value == 0 ? "None" : $"Pattern {value}" : ItemPaintInfo.GetColorName(value);

    private async Task SelectSwatch(ushort value)
    {
        if (PaintCount == 0 || value > SwatchCount)
            return;
        switch (PaintArea)
        {
            case 0: Pattern = 0; Color1 = value; break;
            case 1: Color2 = value; break;
            case 2: Color3 = value; break;
            case 3:
                Pattern = value;
                if (!AllowsPattern && value == 0)
                    PaintArea = 0;
                break;
        }
        await NotifyChanged();
    }

    public void SaveChanges()
    {
        if (Count is < 0 or > Item.MaxCount)
            throw new ArgumentException($"Enter a count between 0 and {Item.MaxCount}.");
        if (HasChanges)
        {
            var previous = SAV.Inventory[SelectedSlot].ItemId;
            var slot = SAV.Inventory[SelectedSlot];
            var pending = new Item(new byte[Item.SIZE], slot.PaintEditable) { RawValue = slot.RawValue, Count = slot.Count };
            pending.Set(ItemId, Count);
            if (!pending.IsEmpty && PaintCount != 0 && Paint != LoadedPaint)
                pending.Paint = Paint;
            slot.RawValue = pending.RawValue;
            slot.Count = pending.Count;
            LoadPaint(slot);
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
        var slot = SAV.Inventory[SelectedSlot];
        var pending = new Item(new byte[Item.SIZE], slot.PaintEditable) { RawValue = slot.RawValue, Count = slot.Count };
        pending.Set(ItemId, Count);
        LoadPaint(pending);
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
