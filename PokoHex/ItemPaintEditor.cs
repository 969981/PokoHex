using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

public partial class ItemPaintEditor : UserControl
{
    private ItemPaint Loaded;
    private bool Loading;
    private int ColorCount;
    private bool AllowsPattern;
    private bool GalleryOpen;
    public bool HasChanges => ColorCount != 0 && Value != Loaded;
    public ItemPaint Value => new(GetValue(CB_Color1), GetValue(CB_Color2), GetValue(CB_Color3), GetValue(CB_Pattern));

    public ItemPaintEditor() => InitializeComponent();

    public void LoadItem(Item item)
    {
        Loading = true;
        Loaded = item.Paint;
        ColorCount = item.PaintCount;
        AllowsPattern = item.AllowsPattern;
        Visible = ColorCount != 0;
        Bind(CB_Color1, ItemPaintInfo.ColorCount, Loaded.Color1, false);
        Bind(CB_Color2, ItemPaintInfo.ColorCount, Loaded.Color2, false);
        Bind(CB_Color3, ItemPaintInfo.ColorCount, Loaded.Color3, false);
        Bind(CB_Pattern, item.AllowsPattern ? ItemPaintInfo.PatternCount : 0, Loaded.Pattern, true);
        CB_Color1.Enabled = ColorCount > 0 && Loaded.Pattern == 0;
        CB_Color2.Enabled = ColorCount > 1;
        CB_Color3.Enabled = ColorCount > 2;
        CB_Pattern.Enabled = item.AllowsPattern || Loaded.Pattern != 0;
        var area = GetValue(CB_PaintArea);
        var areas = Enumerable.Range(0, ColorCount).Select(i => new ItemInfo((ushort)i, $"Color {i + 1}")).ToList();
        if (AllowsPattern || Loaded.Pattern != 0)
            areas.Add(new(3, "Pattern"));
        CB_PaintArea.DataSource = areas;
        CB_PaintArea.DisplayMember = nameof(ItemInfo.Text);
        CB_PaintArea.ValueMember = nameof(ItemInfo.Value);
        CB_PaintArea.SelectedValue = areas.Any(z => z.Value == area) ? area : (ushort)0;
        Height = GalleryOpen && ColorCount != 0 ? 316 : 240;
        Loading = false;
        LoadGallery();
    }

    private static ushort GetValue(ComboBox box) => (box.SelectedItem as ItemInfo)?.Value ?? 0;

    private static void Bind(ComboBox box, int count, ushort value, bool pattern)
    {
        var choices = Enumerable.Range(0, count + 1)
            .Select(i => new ItemInfo((ushort)i, pattern ? i == 0 ? "None" : $"Pattern {i}" : ItemPaintInfo.GetColorName(i))).ToList();
        if (value > count)
            choices.Add(new(value, $"Unknown ({value})"));
        box.DataSource = choices;
        box.DisplayMember = nameof(ItemInfo.Text);
        box.ValueMember = nameof(ItemInfo.Value);
        box.SelectedValue = value;
    }

    private void CB_Pattern_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!Loading)
        {
            CB_Color1.Enabled = ColorCount > 0 && GetValue(CB_Pattern) == 0;
            RefreshGallery();
        }
    }

    private void CB_Color_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!Loading)
            RefreshGallery();
    }

    private void B_Gallery_Click(object sender, EventArgs e)
    {
        GalleryOpen = !GalleryOpen;
        P_Gallery.Visible = GalleryOpen;
        TLP_Paint.Visible = !GalleryOpen;
        GB_Paint.Height = GalleryOpen ? 56 : 240;
        Height = GalleryOpen ? 316 : 240;
        B_Gallery.Text = GalleryOpen ? "Hide gallery" : "Gallery";
        LoadGallery();
    }

    private void CB_PaintArea_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!Loading)
            LoadGallery();
    }

    private void LoadGallery()
    {
        if (!GalleryOpen || ColorCount == 0)
            return;
        if (IL_Swatches.Images.Count == 0)
        {
            for (ushort i = 1; i <= ItemPaintInfo.ColorCount; i++)
                IL_Swatches.Images.Add(PaintSprite.GetColor(i)!);
            for (ushort i = 1; i <= ItemPaintInfo.PatternCount; i++)
                IL_Swatches.Images.Add(PatternSprite.GetPattern(i)!);
        }
        var pattern = GetValue(CB_PaintArea) == 3;
        var count = pattern ? AllowsPattern ? ItemPaintInfo.PatternCount : 0 : ItemPaintInfo.ColorCount;
        FLP_Swatches.SuspendLayout();
        while (FLP_Swatches.Controls.Count != 0)
            FLP_Swatches.Controls[0].Dispose();
        for (ushort i = 0; i <= count; i++)
        {
            var name = pattern ? i == 0 ? "None" : $"Pattern {i}" : ItemPaintInfo.GetColorName(i);
            var button = new Button
            {
                Size = new Size(72, 70), Margin = new Padding(1), Tag = i,
                Text = pattern && i != 0 ? i.ToString() : name, AccessibleName = name,
                ImageList = IL_Swatches, ImageIndex = i == 0 ? -1 : pattern ? ItemPaintInfo.ColorCount + i - 1 : i - 1,
                TextImageRelation = TextImageRelation.ImageAboveText,
            };
            Tip.SetToolTip(button, name);
            button.Click += B_Swatch_Click;
            FLP_Swatches.Controls.Add(button);
        }
        FLP_Swatches.ResumeLayout();
        RefreshGallery();
        var selected = FLP_Swatches.Controls.Cast<Button>().FirstOrDefault(z => (ushort)z.Tag! == SelectedSwatch);
        if (selected != null)
            FLP_Swatches.ScrollControlIntoView(selected);
    }

    private int SelectedSwatch => GetValue(CB_PaintArea) switch
    {
        0 => GetValue(CB_Pattern) == 0 ? GetValue(CB_Color1) : -1,
        1 => GetValue(CB_Color2),
        2 => GetValue(CB_Color3),
        _ => GetValue(CB_Pattern),
    };

    private void RefreshGallery()
    {
        foreach (Button button in FLP_Swatches.Controls)
        {
            var selected = (ushort)button.Tag! == SelectedSwatch;
            button.BackColor = selected ? SystemColors.Highlight : SystemColors.Control;
            button.ForeColor = selected ? SystemColors.HighlightText : SystemColors.ControlText;
        }
    }

    private void B_Swatch_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: ushort value })
            return;
        switch (GetValue(CB_PaintArea))
        {
            case 0: CB_Pattern.SelectedValue = (ushort)0; CB_Color1.SelectedValue = value; break;
            case 1: CB_Color2.SelectedValue = value; break;
            case 2: CB_Color3.SelectedValue = value; break;
            case 3: CB_Pattern.SelectedValue = value; break;
        }
        RefreshGallery();
    }

    private void CB_Paint_DrawItem(object sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (sender is not ComboBox box || e.Index < 0 || box.Items[e.Index] is not ItemInfo choice)
            return;
        var icon = new Rectangle(e.Bounds.Left + 3, e.Bounds.Top + 2, 28, e.Bounds.Height - 4);
        if (box == CB_Pattern)
        {
            var image = PatternSprite.GetPattern(choice.Value);
            if (image != null)
                e.Graphics.DrawImage(image, icon);
        }
        else if (choice.Value is > 0 and <= ItemPaintInfo.ColorCount)
        {
            e.Graphics.FillRectangle(Brushes.Gainsboro, icon);
            var image = PaintSprite.GetColor(choice.Value);
            if (image != null)
                e.Graphics.DrawImage(image, icon);
            e.Graphics.DrawRectangle(Pens.Gray, icon);
        }
        var text = new Rectangle(e.Bounds.Left + 38, e.Bounds.Top, e.Bounds.Width - 38, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, choice.Text, e.Font ?? box.Font, text, e.ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }
}
