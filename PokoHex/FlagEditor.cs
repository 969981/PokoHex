using System;
using System.Linq;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

public partial class FlagEditor : UserControl
{
    private FunctionFlags? SAV;
    private bool Loading;
    private int SelectedFlag = -1;
    private bool LoadedValue;
    public bool HasChanges => SAV != null && SelectedFlag > 0 && CHK_Enabled.Checked != LoadedValue;

    public FlagEditor() => InitializeComponent();

    public void LoadSave(SaveFile save)
    {
        Loading = true;
        SAV = save.Flags;
        SelectedFlag = -1;
        TB_Search.Text = string.Empty;
        CHK_Known.Checked = false;
        TB_Search.Enabled = CHK_Known.Enabled = DGV_Flags.Enabled = SAV != null;
        Loading = false;
        LoadFlags();
    }

    public void RefreshLanguage()
    {
        SaveChanges();
        LoadFlags();
    }

    public void RefreshValues()
    {
        SaveChanges();
        Loading = true;
        foreach (DataGridViewRow row in DGV_Flags.Rows)
            row.Cells[2].Value = SAV!.GetFlag((int)row.Tag!);
        Loading = false;
        LoadFlag();
    }

    private void LoadFlags()
    {
        Loading = true;
        DGV_Flags.Rows.Clear();
        if (SAV != null)
        {
            var search = TB_Search.Text.Trim();
            for (int i = 1; i < FunctionFlags.Count; i++)
            {
                if (CHK_Known.Checked && !FunctionFlagInfo.IsKnown(i))
                    continue;
                var name = FunctionFlagInfo.GetName(i);
                if (!name.Contains(search, StringComparison.CurrentCultureIgnoreCase) && !i.ToString().Contains(search))
                    continue;
                var row = DGV_Flags.Rows[DGV_Flags.Rows.Add(i, name, SAV.GetFlag(i))];
                row.Tag = i;
                if (i == SelectedFlag)
                    DGV_Flags.CurrentCell = row.Cells[1];
            }
        }
        SelectedFlag = DGV_Flags.CurrentRow?.Tag is int index ? index : -1;
        L_Count.Text = SAV == null ? "Flags aren’t available for this save version." : $"{DGV_Flags.Rows.Count} of {FunctionFlags.Count - 1} flags";
        Loading = false;
        LoadFlag();
    }

    private void LoadFlag()
    {
        Loading = true;
        GB_Flag.Enabled = SAV != null && SelectedFlag > 0;
        L_Name.Text = SelectedFlag > 0 ? FunctionFlagInfo.GetName(SelectedFlag) : "Flag";
        LoadedValue = SAV != null && SelectedFlag > 0 && SAV.GetFlag(SelectedFlag);
        CHK_Enabled.Checked = LoadedValue;
        B_Set.Enabled = false;
        Loading = false;
    }

    public void SaveChanges()
    {
        if (Loading || !HasChanges)
            return;
        SAV!.SetFlag(SelectedFlag, CHK_Enabled.Checked);
        LoadedValue = CHK_Enabled.Checked;
        UpdateRow(SelectedFlag, LoadedValue);
        B_Set.Enabled = false;
    }

    private void UpdateRow(int index, bool value)
    {
        var row = DGV_Flags.Rows.Cast<DataGridViewRow>().FirstOrDefault(z => (int)z.Tag! == index);
        if (row == null)
            return;
        var loading = Loading;
        Loading = true;
        row.Cells[2].Value = value;
        Loading = loading;
    }

    private void DGV_Flags_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
        if (!Loading && DGV_Flags.IsCurrentCellDirty && DGV_Flags.CurrentCell is DataGridViewCheckBoxCell)
            DGV_Flags.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void DGV_Flags_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if (Loading || SAV == null || e.ColumnIndex != 2 || e.RowIndex < 0 || e.RowIndex >= DGV_Flags.Rows.Count)
            return;
        var row = DGV_Flags.Rows[e.RowIndex];
        if (row.Tag is not int index || index is < 1 or >= FunctionFlags.Count || row.Cells[2].Value is not bool value)
            return;
        SaveChanges();
        SAV.SetFlag(index, value);
        UpdateRow(index, value);
        if (SelectedFlag == index)
            LoadFlag();
    }

    private void DGV_Flags_CurrentCellChanged(object sender, EventArgs e)
    {
        if (Loading)
            return;
        SaveChanges();
        SelectedFlag = DGV_Flags.CurrentRow?.Tag is int index ? index : -1;
        LoadFlag();
    }

    private void Filter_Changed(object sender, EventArgs e)
    {
        if (Loading)
            return;
        SaveChanges();
        LoadFlags();
    }

    private void CHK_Enabled_CheckedChanged(object sender, EventArgs e)
    {
        if (!Loading)
        {
            B_Set.Enabled = HasChanges;
            UpdateRow(SelectedFlag, CHK_Enabled.Checked);
        }
    }

    private void B_Set_Click(object sender, EventArgs e) => SaveChanges();
}
