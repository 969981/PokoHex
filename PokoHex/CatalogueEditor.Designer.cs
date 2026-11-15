using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PokoHex;

partial class CatalogueEditor
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
        var view = new Label();
        var search = new Label();
        var tips = new ToolTip(components);
        CB_View = new ComboBox();
        TB_Search = new TextBox();
        DGV_Entries = new DataGridView();
        L_Count = new Label();
        GB_Entry = new GroupBox();
        PB_Entry = new PictureBox();
        L_Name = new Label();
        L_State = new Label();
        B_Unlock = new Button();
        B_Partial = new Button();
        B_All = new Button();
        B_Undo = new Button();
        B_Redo = new Button();
        SuspendLayout();
        split.Dock = DockStyle.Fill;
        split.Size = new Size(920, 580);
        split.SplitterDistance = 600;
        split.Panel1MinSize = 260;
        split.Panel2MinSize = 300;
        split.FixedPanel = FixedPanel.Panel2;
        split.Panel2.AutoScroll = true;
        filters.Dock = DockStyle.Top;
        filters.Size = new Size(600, 72);
        view.Text = "View:";
        view.AutoSize = true;
        view.Location = new Point(0, 8);
        CB_View.Name = "CB_View";
        CB_View.Location = new Point(60, 4);
        CB_View.Size = new Size(240, 23);
        CB_View.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_View.Items.AddRange(["Pokédex", "Habitats", "Crafting", "Cooking"]);
        CB_View.SelectedIndexChanged += CB_View_SelectedIndexChanged;
        search.Text = "Search:";
        search.AutoSize = true;
        search.Location = new Point(0, 42);
        TB_Search.Name = "TB_Search";
        TB_Search.Location = new Point(60, 38);
        TB_Search.Size = new Size(530, 23);
        TB_Search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        TB_Search.TextChanged += TB_Search_TextChanged;
        filters.Controls.AddRange([view, CB_View, search, TB_Search]);
        DGV_Entries.Name = "DGV_Entries";
        DGV_Entries.Dock = DockStyle.Fill;
        DGV_Entries.ReadOnly = true;
        DGV_Entries.AllowUserToAddRows = false;
        DGV_Entries.AllowUserToDeleteRows = false;
        DGV_Entries.AllowUserToResizeRows = false;
        DGV_Entries.MultiSelect = false;
        DGV_Entries.RowHeadersVisible = false;
        DGV_Entries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        DGV_Entries.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        DGV_Entries.BackgroundColor = SystemColors.Window;
        DGV_Entries.RowTemplate.Height = 44;
        DGV_Entries.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 44, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ImageLayout = DataGridViewImageCellLayout.Zoom, DefaultCellStyle = new DataGridViewCellStyle { NullValue = null } });
        DGV_Entries.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Entries.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Entries.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Width = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Entries.CurrentCellChanged += DGV_Entries_CurrentCellChanged;
        L_Count.Dock = DockStyle.Bottom;
        L_Count.Height = 28;
        L_Count.TextAlign = ContentAlignment.MiddleLeft;
        split.Panel1.Controls.Add(DGV_Entries);
        split.Panel1.Controls.Add(filters);
        split.Panel1.Controls.Add(L_Count);
        GB_Entry.Name = "GB_Entry";
        GB_Entry.Dock = DockStyle.Top;
        GB_Entry.Size = new Size(316, 450);
        PB_Entry.Name = "PB_Entry";
        PB_Entry.Location = new Point(16, 28);
        PB_Entry.Size = new Size(156, 156);
        PB_Entry.SizeMode = PictureBoxSizeMode.Zoom;
        L_Name.Name = "L_Name";
        L_Name.Location = new Point(16, 192);
        L_Name.Size = new Size(260, 50);
        L_State.Name = "L_State";
        L_State.Location = new Point(16, 248);
        L_State.Size = new Size(260, 24);
        B_Unlock.Name = "B_Unlock";
        B_Unlock.Location = new Point(16, 282);
        B_Unlock.Size = new Size(126, 32);
        B_Unlock.Click += B_Unlock_Click;
        B_Partial.Name = "B_Partial";
        B_Partial.Location = new Point(150, 282);
        B_Partial.Size = new Size(126, 32);
        B_Partial.Click += B_Partial_Click;
        B_All.Name = "B_All";
        B_All.Location = new Point(16, 336);
        B_All.Size = new Size(260, 32);
        B_All.Click += B_All_Click;
        tips.SetToolTip(B_All, "Applies to the whole selected catalogue.");
        B_Undo.Name = "B_Undo";
        B_Undo.Text = "Undo";
        B_Undo.Location = new Point(16, 392);
        B_Undo.Size = new Size(126, 32);
        B_Undo.Click += B_Undo_Click;
        B_Redo.Name = "B_Redo";
        B_Redo.Text = "Redo";
        B_Redo.Location = new Point(150, 392);
        B_Redo.Size = new Size(126, 32);
        B_Redo.Click += B_Redo_Click;
        GB_Entry.Controls.AddRange([PB_Entry, L_Name, L_State, B_Unlock, B_Partial, B_All, B_Undo, B_Redo]);
        split.Panel2.Controls.Add(GB_Entry);
        Controls.Add(split);
        Padding = new Padding(10);
        Size = new Size(940, 600);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ResumeLayout(false);
    }

    private ComboBox CB_View = null!;
    private TextBox TB_Search = null!;
    private DataGridView DGV_Entries = null!;
    private Label L_Count = null!;
    private GroupBox GB_Entry = null!;
    private PictureBox PB_Entry = null!;
    private Label L_Name = null!;
    private Label L_State = null!;
    private Button B_Unlock = null!;
    private Button B_Partial = null!;
    private Button B_All = null!;
    private Button B_Undo = null!;
    private Button B_Redo = null!;
}
