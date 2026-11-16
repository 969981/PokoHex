using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PokoHex;

partial class PhotoStyleEditor
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
        var search = new Panel();
        var label = new Label();
        TB_Search = new TextBox();
        FLP_Styles = new FlowLayoutPanel();
        GB_Style = new GroupBox();
        PB_Style = new PictureBox();
        L_Status = new Label();
        IL_Styles = new ImageList(components);
        Tip = new ToolTip(components);
        SuspendLayout();
        split.Dock = DockStyle.Fill;
        split.Size = new Size(1000, 580);
        split.SplitterDistance = 500;
        split.Panel1MinSize = 300;
        split.Panel2MinSize = 260;
        search.Dock = DockStyle.Top;
        search.Height = 38;
        label.Text = "Search:";
        label.AutoSize = true;
        label.Location = new Point(0, 8);
        TB_Search.Name = "TB_Search";
        TB_Search.Location = new Point(60, 4);
        TB_Search.Size = new Size(430, 23);
        TB_Search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        TB_Search.TextChanged += TB_Search_TextChanged;
        search.Controls.AddRange([label, TB_Search]);
        FLP_Styles.Name = "FLP_Styles";
        FLP_Styles.Dock = DockStyle.Fill;
        FLP_Styles.AutoScroll = true;
        IL_Styles.ColorDepth = ColorDepth.Depth32Bit;
        IL_Styles.ImageSize = new Size(128, 73);
        split.Panel1.Controls.Add(FLP_Styles);
        split.Panel1.Controls.Add(search);
        GB_Style.Name = "GB_Style";
        GB_Style.Dock = DockStyle.Fill;
        GB_Style.Padding = new Padding(10, 20, 10, 10);
        PB_Style.Name = "PB_Style";
        PB_Style.Dock = DockStyle.Fill;
        PB_Style.SizeMode = PictureBoxSizeMode.Zoom;
        PB_Style.BackColor = Color.DimGray;
        L_Status.Dock = DockStyle.Bottom;
        L_Status.Height = 32;
        L_Status.TextAlign = ContentAlignment.MiddleLeft;
        GB_Style.Controls.Add(PB_Style);
        GB_Style.Controls.Add(L_Status);
        split.Panel2.Controls.Add(GB_Style);
        Controls.Add(split);
        Size = new Size(1000, 580);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ResumeLayout(false);
    }

    private TextBox TB_Search = null!;
    private FlowLayoutPanel FLP_Styles = null!;
    private GroupBox GB_Style = null!;
    private PictureBox PB_Style = null!;
    private Label L_Status = null!;
    private ImageList IL_Styles = null!;
    private ToolTip Tip = null!;
}
