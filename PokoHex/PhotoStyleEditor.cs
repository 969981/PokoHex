using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

public partial class PhotoStyleEditor : UserControl
{
    private SaveFile? SAV;
    private bool Filters;
    private int SelectedFrame;
    private int SelectedFilter;
    private int SelectedStyle => Filters ? SelectedFilter : SelectedFrame;

    public PhotoStyleEditor() => InitializeComponent();

    public void LoadSave(SaveFile save, bool filters)
    {
        var resetSearch = SAV != save || Filters != filters;
        if (SAV != save)
            SelectedFrame = SelectedFilter = 0;
        SAV = save;
        Filters = filters;
        if (resetSearch)
            TB_Search.Text = string.Empty;
        LoadStyles();
    }

    public void RefreshLanguage() => LoadStyles();

    private void LoadStyles()
    {
        FLP_Styles.SuspendLayout();
        while (FLP_Styles.Controls.Count != 0)
            FLP_Styles.Controls[0].Dispose();
        if (IL_Styles.Images.Count == 0)
        {
            foreach (var item in GameInfo.Strings.PhotoFrameDataSource)
                IL_Styles.Images.Add($"frame_{item.Value}", PhotoSprite.GetStyle(item.Value)!);
            foreach (var item in GameInfo.Strings.PhotoFilterDataSource)
                IL_Styles.Images.Add($"filter_{item.Value}", PhotoSprite.GetStyle(item.Value, true)!);
        }
        var search = TB_Search.Text.Trim();
        var choices = Filters ? GameInfo.Strings.PhotoFilterDataSource : GameInfo.Strings.PhotoFrameDataSource;
        foreach (var item in choices.Where(z => z.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) || z.Value.ToString().Contains(search)))
        {
            var selected = item.Value == SelectedStyle;
            var button = new Button
            {
                Size = new Size(152, 122), Margin = new Padding(2), Tag = item.Value,
                Text = item.Text, AccessibleName = item.Text,
                ImageList = IL_Styles, ImageKey = $"{(Filters ? "filter" : "frame")}_{item.Value}",
                TextImageRelation = TextImageRelation.ImageAboveText,
                BackColor = selected ? SystemColors.Highlight : SystemColors.Control,
                ForeColor = selected ? SystemColors.HighlightText : SystemColors.ControlText,
            };
            Tip.SetToolTip(button, item.Text);
            button.Click += B_Style_Click;
            FLP_Styles.Controls.Add(button);
        }
        FLP_Styles.ResumeLayout();
        var current = FLP_Styles.Controls.Cast<Button>().FirstOrDefault(z => (ushort)z.Tag! == SelectedStyle);
        if (current != null)
            FLP_Styles.ScrollControlIntoView(current);
        LoadPreview();
    }

    private void LoadPreview()
    {
        ClearPreview();
        GB_Style.Text = PhotoStyleInfo.GetName(SelectedStyle, Filters);
        PB_Style.SizeMode = Filters ? PictureBoxSizeMode.CenterImage : PictureBoxSizeMode.Zoom;
        PB_Style.BackColor = Filters ? SystemColors.Control : Color.DimGray;
        PB_Style.Image = !Filters && SelectedStyle != 0 ? PhotoSprite.GetFramePreview(SelectedStyle) : new Bitmap(PhotoSprite.GetStyle(SelectedStyle, Filters)!);
        L_Status.Text = SAV?.PhotoStyles == null ? "Unlocks aren’t available for this save version." : SAV.PhotoStyles.IsUnlocked(SelectedStyle, Filters) ? "Unlocked" : "Locked";
    }

    private void ClearPreview()
    {
        var image = PB_Style.Image;
        PB_Style.Image = null;
        image?.Dispose();
    }

    private void B_Style_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: ushort value })
            return;
        if (Filters)
            SelectedFilter = value;
        else
            SelectedFrame = value;
        foreach (Button button in FLP_Styles.Controls)
        {
            var selected = (ushort)button.Tag! == value;
            button.BackColor = selected ? SystemColors.Highlight : SystemColors.Control;
            button.ForeColor = selected ? SystemColors.HighlightText : SystemColors.ControlText;
        }
        LoadPreview();
    }

    private void TB_Search_TextChanged(object sender, EventArgs e) => LoadStyles();
}
