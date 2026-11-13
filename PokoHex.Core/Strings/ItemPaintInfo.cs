using System;

namespace PokoHex.Core;

public static class ItemPaintInfo
{
    public const int ColorCount = 18;
    public const int PatternCount = 100;
    public const int ItemCount = 4600;
    private static readonly byte[] Items = ReadItems();
    private static readonly string[] Names =
    [
        "Default", "White", "Red", "Blue", "Yellow", "Green", "Aquamarine", "Orange", "Purple",
        "Brown", "Cyan", "Rose", "Lime", "Beige", "Plum", "Navy", "Black", "Gray", "Pink",
    ];

    public static int GetColorCount(ushort item) => item < Items.Length ? Items[item] & 3 : 0;
    public static bool AllowsPattern(ushort item) => item < Items.Length && (Items[item] & 4) != 0;
    public static string GetColorName(int color) => (uint)color <= ColorCount ? Names[color] : $"Unknown ({color})";

    public static string GetDescription(Item item)
    {
        var paint = item.Paint;
        var count = item.PaintCount;
        if (item.IsEmpty || count == 0 || paint.Pattern == 0 && paint.Color1 == 0 &&
            (count < 2 || paint.Color2 == 0) && (count < 3 || paint.Color3 == 0))
            return string.Empty;
        string[] colors =
        [
            paint.Pattern == 0 ? GetColorName(paint.Color1) : $"Pattern {paint.Pattern}",
            GetColorName(paint.Color2), GetColorName(paint.Color3),
        ];
        return string.Join(" / ", colors, 0, count);
    }

    private static byte[] ReadItems()
    {
        var data = ResourceUtil.GetBinaryResource("item_paint.bin");
        if (data.Length != ItemCount || Array.Exists(data, z => (z & ~7) != 0 || z == 4))
            throw new InvalidOperationException("The paint catalog is invalid.");
        return data;
    }
}
