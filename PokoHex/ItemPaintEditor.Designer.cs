using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PokoHex;

partial class ItemPaintEditor
{
    private IContainer components = null!;
    private ComboBox CB_Color1 = null!;
    private ComboBox CB_Color2 = null!;
    private ComboBox CB_Color3 = null!;
    private ComboBox CB_Pattern = null!;
    private Button B_Gallery = null!;
    private ComboBox CB_PaintArea = null!;
    private Panel P_Gallery = null!;
    private FlowLayoutPanel FLP_Swatches = null!;
    private ImageList IL_Swatches = null!;
    private ToolTip Tip = null!;
    private GroupBox GB_Paint = null!;
    private TableLayoutPanel TLP_Paint = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        GB_Paint = new GroupBox();
        TLP_Paint = new TableLayoutPanel();
        CB_Color1 = new ComboBox();
        CB_Color2 = new ComboBox();
        CB_Color3 = new ComboBox();
        CB_Pattern = new ComboBox();
        B_Gallery = new Button();
        CB_PaintArea = new ComboBox();
        P_Gallery = new Panel();
        FLP_Swatches = new FlowLayoutPanel();
        IL_Swatches = new ImageList(components);
        Tip = new ToolTip(components);
        SuspendLayout();
        GB_Paint.Text = "Paint";
        GB_Paint.Dock = DockStyle.Top;
        GB_Paint.Height = 240;
        GB_Paint.Padding = new Padding(8);
        TLP_Paint.Dock = DockStyle.Fill;
        TLP_Paint.ColumnCount = 2;
        TLP_Paint.RowCount = 4;
        TLP_Paint.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        TLP_Paint.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var boxes = new[] { CB_Color1, CB_Color2, CB_Color3, CB_Pattern };
        var names = new[] { "Color 1", "Color 2", "Color 3", "Pattern" };
        for (int i = 0; i < boxes.Length; i++)
        {
            TLP_Paint.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            var box = boxes[i];
            box.Name = i == 3 ? "CB_Pattern" : $"CB_Color{i + 1}";
            box.Dock = DockStyle.Fill;
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.DrawMode = DrawMode.OwnerDrawFixed;
            box.ItemHeight = 30;
            box.IntegralHeight = false;
            box.DropDownHeight = 320;
            box.DrawItem += CB_Paint_DrawItem;
            TLP_Paint.Controls.Add(new Label { Text = names[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, i);
            TLP_Paint.Controls.Add(box, 1, i);
        }
        CB_Pattern.SelectedIndexChanged += CB_Pattern_SelectedIndexChanged;
        CB_Color1.SelectedIndexChanged += CB_Color_SelectedIndexChanged;
        CB_Color2.SelectedIndexChanged += CB_Color_SelectedIndexChanged;
        CB_Color3.SelectedIndexChanged += CB_Color_SelectedIndexChanged;
        B_Gallery.Text = "Gallery";
        B_Gallery.Dock = DockStyle.Bottom;
        B_Gallery.Height = 28;
        B_Gallery.Click += B_Gallery_Click;
        IL_Swatches.ImageSize = new Size(40, 40);
        IL_Swatches.ColorDepth = ColorDepth.Depth32Bit;
        CB_PaintArea.Dock = DockStyle.Top;
        CB_PaintArea.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_PaintArea.SelectedIndexChanged += CB_PaintArea_SelectedIndexChanged;
        FLP_Swatches.Dock = DockStyle.Fill;
        FLP_Swatches.AutoScroll = true;
        P_Gallery.Dock = DockStyle.Fill;
        P_Gallery.Padding = new Padding(8);
        P_Gallery.Visible = false;
        P_Gallery.Controls.Add(FLP_Swatches);
        P_Gallery.Controls.Add(CB_PaintArea);
        GB_Paint.Controls.Add(TLP_Paint);
        GB_Paint.Controls.Add(B_Gallery);
        Controls.Add(P_Gallery);
        Controls.Add(GB_Paint);
        Size = new Size(352, 240);
        Visible = false;
        ResumeLayout(false);
    }
}
