using System;
using System.Linq;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

public partial class CatalogueEditor : UserControl
{
    private Catalogue? SAV;
    private bool Loading;
    private readonly int[] Selection = [-1, -1, -1, -1];
    private CatalogueType View => (CatalogueType)CB_View.SelectedIndex;
    private int SelectedEntry => Selection[(int)View];
    private ItemInfo[] Entries = [];

    public CatalogueEditor()
    {
        InitializeComponent();
        CB_View.SelectedIndex = 0;
    }

    public void LoadSave(SaveFile save)
    {
        SAV = save.Catalogue;
        Array.Fill(Selection, -1);
        Loading = true;
        TB_Search.Text = string.Empty;
        CB_View.Enabled = TB_Search.Enabled = DGV_Entries.Enabled = SAV != null;
        Loading = false;
        RefreshLanguage();
    }

    public void RefreshLanguage()
    {
        Entries = CatalogueInfo.GetEntries(View);
        LoadEntries();
    }

    public void RefreshValues()
    {
        if (SAV == null)
            return;
        foreach (DataGridViewRow row in DGV_Entries.Rows)
            row.Cells[3].Value = CatalogueInfo.GetStatus(View, SAV.GetState(View, (int)row.Tag!));
        LoadEntry();
    }

    private void LoadEntries()
    {
        Loading = true;
        DGV_Entries.Rows.Clear();
        if (SAV != null)
        {
            var search = TB_Search.Text.Trim();
            foreach (var entry in Entries)
            {
                if (!entry.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) && !entry.Value.ToString().Contains(search))
                    continue;
                var row = DGV_Entries.Rows[DGV_Entries.Rows.Add(CatalogueSprite.GetSprite(View, entry.Value)!, entry.Value, entry.Text, CatalogueInfo.GetStatus(View, SAV.GetState(View, entry.Value)))];
                row.Tag = (int)entry.Value;
            }
        }
        var selected = DGV_Entries.Rows.Cast<DataGridViewRow>().FirstOrDefault(z => (int)z.Tag! == SelectedEntry) ?? DGV_Entries.Rows.Cast<DataGridViewRow>().FirstOrDefault();
        if (selected != null)
            DGV_Entries.CurrentCell = selected.Cells[2];
        Selection[(int)View] = selected?.Tag is int index ? index : -1;
        Loading = false;
        LoadEntry();
    }

    private void LoadEntry()
    {
        var valid = SAV != null && SelectedEntry > 0;
        GB_Entry.Text = CatalogueInfo.GetTitle(View);
        L_Name.Text = valid ? CatalogueInfo.GetName(View, SelectedEntry) : string.Empty;
        L_State.Text = valid ? CatalogueInfo.GetStatus(View, SAV!.GetState(View, SelectedEntry)) : string.Empty;
        PB_Entry.Image = valid ? CatalogueSprite.GetSprite(View, SelectedEntry) : null;
        B_Unlock.Text = CatalogueInfo.GetAction(View);
        B_All.Text = CatalogueInfo.GetAction(View, true);
        B_Partial.Visible = View is CatalogueType.Pokedex or CatalogueType.Habitats;
        B_Partial.Text = View == CatalogueType.Pokedex ? "Mark seen" : "Discover";
        B_Unlock.Enabled = valid && SAV!.CanUnlock(View, SelectedEntry);
        B_Partial.Enabled = valid && SAV!.CanUnlock(View, SelectedEntry, true);
        B_All.Enabled = SAV != null && Entries.Any(z => SAV.CanUnlock(View, z.Value));
        B_Undo.Enabled = SAV?.CanUndo == true;
        B_Redo.Enabled = SAV?.CanRedo == true;
        L_Count.Text = SAV == null ? "Catalogue isn’t available for this save version." : $"{DGV_Entries.Rows.Count} of {Entries.Length} entries · {Entries.Count(z => SAV.GetState(View, z.Value) == 2)} complete";
    }

    private void DGV_Entries_CurrentCellChanged(object sender, EventArgs e)
    {
        if (Loading)
            return;
        Selection[(int)View] = DGV_Entries.CurrentRow?.Tag is int index ? index : -1;
        LoadEntry();
    }

    private void CB_View_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (Loading)
            return;
        Loading = true;
        TB_Search.Text = string.Empty;
        Loading = false;
        RefreshLanguage();
    }

    private void TB_Search_TextChanged(object sender, EventArgs e)
    {
        if (!Loading)
            LoadEntries();
    }

    private void Apply(Action<Catalogue> change)
    {
        if (SAV == null)
            return;
        try
        {
            change(SAV);
            RefreshValues();
        }
        catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, "PokoHex", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void B_Unlock_Click(object sender, EventArgs e) => Apply(z => z.Unlock(View, SelectedEntry));
    private void B_Partial_Click(object sender, EventArgs e) => Apply(z => z.Unlock(View, SelectedEntry, true));
    private void B_All_Click(object sender, EventArgs e) => Apply(z => z.UnlockAll(View));
    private void B_Undo_Click(object sender, EventArgs e) => Apply(z => z.UndoEdit());
    private void B_Redo_Click(object sender, EventArgs e) => Apply(z => z.RedoEdit());
}
