using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using PokoHex.Core;

namespace PokoHex;

public static class PaintSprite
{
    private static readonly ResourceManager Resources = new("PokoHex.Properties.Resources", typeof(PaintSprite).Assembly);
    private static readonly Dictionary<ushort, Image?> Images = new();

    public static Image? GetColor(ushort color)
    {
        if (color is 0 or > ItemPaintInfo.ColorCount)
            return null;
        if (Images.TryGetValue(color, out var image))
            return image;
        image = Resources.GetObject($"paint_{color:D2}") as Image;
        Images.Add(color, image);
        return image;
    }
}
