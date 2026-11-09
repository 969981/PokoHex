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
        foreach (DataGridViewRow row in DGV_Flags.Rows)
            row.Cells[2].Value = SAV!.GetFlag((int)row.Tag!);
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
        var row = DGV_Flags.Rows.Cast<DataGridViewRow>().FirstOrDefault(z => (int)z.Tag! == SelectedFlag);
        if (row != null)
            row.Cells[2].Value = LoadedValue;
        B_Set.Enabled = false;
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
            B_Set.Enabled = HasChanges;
    }

    private void B_Set_Click(object sender, EventArgs e) => SaveChanges();
}
