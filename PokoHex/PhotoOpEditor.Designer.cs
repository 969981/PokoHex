using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PokoHex;

partial class PhotoOpEditor
{
    private IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearPreview();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        var split = new SplitContainer();
        var filters = new Panel();
        var search = new Label();
        var buttons = new FlowLayoutPanel();
        var views = new FlowLayoutPanel();
        var camera = new PictureBox();
        var viewLabel = new Label();
        var capture = new FlowLayoutPanel();
        var photoLabel = new Label();
        CB_View = new ComboBox();
        CB_Photo = new ComboBox();
        B_ExportPhoto = new Button();
        P_Photos = new Panel();
        Editor_Styles = new PhotoStyleEditor();
        TB_Search = new TextBox();
        DGV_Photos = new DataGridView();
        GB_Photo = new GroupBox();
        PB_Photo = new PictureBox();
        L_Preview = new Label();
        L_Status = new Label();
        B_Reset = new Button();
        B_Undo = new Button();
        B_Redo = new Button();
        SuspendLayout();
        split.Dock = DockStyle.Fill;
        split.Size = new Size(1000, 580);
        split.SplitterDistance = 500;
        split.Panel1MinSize = 300;
        split.Panel2MinSize = 260;
        filters.Dock = DockStyle.Top;
        filters.Height = 38;
        search.Text = "Search:";
        search.AutoSize = true;
        search.Location = new Point(0, 8);
        TB_Search.Name = "TB_Search";
        TB_Search.Location = new Point(60, 4);
        TB_Search.Size = new Size(430, 23);
        TB_Search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        TB_Search.TextChanged += TB_Search_TextChanged;
        filters.Controls.AddRange([search, TB_Search]);
        DGV_Photos.Name = "DGV_Photos";
        DGV_Photos.Dock = DockStyle.Fill;
        DGV_Photos.AllowUserToAddRows = false;
        DGV_Photos.AllowUserToDeleteRows = false;
        DGV_Photos.AllowUserToResizeRows = false;
        DGV_Photos.ReadOnly = true;
        DGV_Photos.MultiSelect = false;
        DGV_Photos.RowHeadersVisible = false;
        DGV_Photos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        DGV_Photos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        DGV_Photos.BackgroundColor = SystemColors.Window;
        DGV_Photos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Photos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Completed", Width = 90, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Photos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Photos", Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Photos.CurrentCellChanged += DGV_Photos_CurrentCellChanged;
        split.Panel1.Controls.Add(DGV_Photos);
        split.Panel1.Controls.Add(filters);
        GB_Photo.Name = "GB_Photo";
        GB_Photo.Text = "Photo Op";
        GB_Photo.Dock = DockStyle.Fill;
        GB_Photo.Padding = new Padding(10, 20, 10, 10);
        PB_Photo.Name = "PB_Photo";
        PB_Photo.Dock = DockStyle.Fill;
        PB_Photo.SizeMode = PictureBoxSizeMode.Zoom;
        L_Preview.Dock = DockStyle.Bottom;
        L_Preview.Height = 30;
        L_Preview.TextAlign = ContentAlignment.MiddleCenter;
        buttons.Dock = DockStyle.Bottom;
        buttons.Height = 80;
        buttons.WrapContents = true;
        B_Reset.Name = "B_Reset";
        B_Reset.Text = "Reset";
        B_Reset.Size = new Size(80, 30);
        B_Reset.Click += B_Reset_Click;
        B_Undo.Name = "B_Undo";
        B_Undo.Text = "Undo";
        B_Undo.Size = new Size(80, 30);
        B_Undo.Click += B_Undo_Click;
        B_Redo.Name = "B_Redo";
        B_Redo.Text = "Redo";
        B_Redo.Size = new Size(80, 30);
        B_Redo.Click += B_Redo_Click;
        B_ExportPhoto.Name = "B_ExportPhoto";
        B_ExportPhoto.Text = "Export photo…";
        B_ExportPhoto.Size = new Size(110, 30);
        B_ExportPhoto.Click += B_ExportPhoto_Click;
        buttons.Controls.AddRange([B_Reset, B_Undo, B_Redo, B_ExportPhoto]);
        capture.Dock = DockStyle.Top;
        capture.Height = 40;
        photoLabel.Text = "Photo:";
        photoLabel.AutoSize = true;
        photoLabel.Margin = new Padding(0, 8, 8, 0);
        CB_Photo.Name = "CB_Photo";
        CB_Photo.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_Photo.Width = 160;
        CB_Photo.DisplayMember = "Text";
        CB_Photo.ValueMember = "Value";
        CB_Photo.SelectedIndexChanged += CB_Photo_SelectedIndexChanged;
        capture.Controls.AddRange([photoLabel, CB_Photo]);
        GB_Photo.Controls.Add(PB_Photo);
        GB_Photo.Controls.Add(L_Preview);
        GB_Photo.Controls.Add(buttons);
        GB_Photo.Controls.Add(capture);
        split.Panel2.Controls.Add(GB_Photo);
        L_Status.Dock = DockStyle.Bottom;
        L_Status.Height = 40;
        views.Dock = DockStyle.Top;
        views.Height = 44;
        camera.Image = PhotoSprite.GetCamera();
        camera.SizeMode = PictureBoxSizeMode.Zoom;
        camera.Size = new Size(28, 28);
        viewLabel.Text = "View:";
        viewLabel.AutoSize = true;
        viewLabel.Margin = new Padding(6, 8, 8, 0);
        CB_View.Name = "CB_View";
        CB_View.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_View.Width = 160;
        CB_View.Items.AddRange(["Photo Ops", "Frames", "Filters"]);
        CB_View.SelectedIndex = 0;
        CB_View.SelectedIndexChanged += CB_View_SelectedIndexChanged;
        views.Controls.AddRange([camera, viewLabel, CB_View]);
        P_Photos.Dock = DockStyle.Fill;
        P_Photos.Controls.Add(split);
        Editor_Styles.Dock = DockStyle.Fill;
        Editor_Styles.Visible = false;
        Controls.Add(P_Photos);
        Controls.Add(Editor_Styles);
        Controls.Add(L_Status);
        Controls.Add(views);
        Padding = new Padding(10);
        Size = new Size(1000, 600);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ResumeLayout(false);
    }

    private TextBox TB_Search = null!;
    private DataGridView DGV_Photos = null!;
    private GroupBox GB_Photo = null!;
    private PictureBox PB_Photo = null!;
    private Label L_Preview = null!;
    private Label L_Status = null!;
    private Button B_Reset = null!;
    private Button B_Undo = null!;
    private Button B_Redo = null!;
    private Button B_ExportPhoto = null!;
    private ComboBox CB_View = null!;
    private ComboBox CB_Photo = null!;
    private Panel P_Photos = null!;
    private PhotoStyleEditor Editor_Styles = null!;
}
