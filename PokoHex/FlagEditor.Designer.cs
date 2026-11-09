using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PokoHex;

partial class FlagEditor
{
    private IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        var split = new SplitContainer();
        var filters = new Panel();
        var search = new Label();
        var warning = new Label();
        TB_Search = new TextBox();
        CHK_Known = new CheckBox();
        DGV_Flags = new DataGridView();
        L_Count = new Label();
        GB_Flag = new GroupBox();
        L_Name = new Label();
        CHK_Enabled = new CheckBox();
        B_Set = new Button();
        SuspendLayout();
        split.Dock = DockStyle.Fill;
        split.Size = new Size(920, 580);
        split.SplitterDistance = 620;
        split.Panel1MinSize = 260;
        split.Panel2MinSize = 240;
        split.FixedPanel = FixedPanel.Panel2;
        filters.Dock = DockStyle.Top;
        filters.Size = new Size(620, 68);
        search.Text = "Search:";
        search.AutoSize = true;
        search.Location = new Point(0, 8);
        TB_Search.Name = "TB_Search";
        TB_Search.Location = new Point(60, 4);
        TB_Search.Size = new Size(550, 23);
        TB_Search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        TB_Search.TextChanged += Filter_Changed;
        CHK_Known.Name = "CHK_Known";
        CHK_Known.Text = "Known only";
        CHK_Known.AutoSize = true;
        CHK_Known.Location = new Point(60, 38);
        CHK_Known.CheckedChanged += Filter_Changed;
        filters.Controls.AddRange([search, TB_Search, CHK_Known]);
        DGV_Flags.Name = "DGV_Flags";
        DGV_Flags.Dock = DockStyle.Fill;
        DGV_Flags.AllowUserToAddRows = false;
        DGV_Flags.AllowUserToDeleteRows = false;
        DGV_Flags.AllowUserToResizeRows = false;
        DGV_Flags.ReadOnly = true;
        DGV_Flags.MultiSelect = false;
        DGV_Flags.RowHeadersVisible = false;
        DGV_Flags.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        DGV_Flags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        DGV_Flags.BackgroundColor = SystemColors.Window;
        DGV_Flags.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 55, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Flags.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Flags.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Enabled", Width = 70, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        DGV_Flags.CurrentCellChanged += DGV_Flags_CurrentCellChanged;
        L_Count.Dock = DockStyle.Bottom;
        L_Count.Height = 28;
        L_Count.TextAlign = ContentAlignment.MiddleLeft;
        split.Panel1.Controls.Add(DGV_Flags);
        split.Panel1.Controls.Add(filters);
        split.Panel1.Controls.Add(L_Count);
        GB_Flag.Name = "GB_Flag";
        GB_Flag.Text = "Flag";
        GB_Flag.Dock = DockStyle.Fill;
        GB_Flag.Size = new Size(296, 580);
        L_Name.Name = "L_Name";
        L_Name.Location = new Point(16, 28);
        L_Name.Size = new Size(260, 70);
        L_Name.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        CHK_Enabled.Name = "CHK_Enabled";
        CHK_Enabled.Text = "Enabled";
        CHK_Enabled.AutoSize = true;
        CHK_Enabled.Location = new Point(16, 112);
        CHK_Enabled.CheckedChanged += CHK_Enabled_CheckedChanged;
        B_Set.Name = "B_Set";
        B_Set.Text = "Set";
        B_Set.Location = new Point(16, 150);
        B_Set.Size = new Size(100, 30);
        B_Set.Click += B_Set_Click;
        GB_Flag.Controls.AddRange([L_Name, CHK_Enabled, B_Set]);
        split.Panel2.Controls.Add(GB_Flag);
        warning.Dock = DockStyle.Top;
        warning.Height = 40;
        warning.Text = "Warning: Flags may affect gameplay progression, toggle these at your own risk.";
        Controls.Add(split);
        Controls.Add(warning);
        Padding = new Padding(10);
        Size = new Size(920, 600);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ResumeLayout(false);
    }

    private TextBox TB_Search = null!;
    private CheckBox CHK_Known = null!;
    private DataGridView DGV_Flags = null!;
    private Label L_Count = null!;
    private GroupBox GB_Flag = null!;
    private Label L_Name = null!;
    private CheckBox CHK_Enabled = null!;
    private Button B_Set = null!;
}
