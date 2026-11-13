using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using PokoHex.Core;

namespace PokoHex;

public static class PatternSprite
{
    private static readonly ResourceManager Resources = new("PokoHex.Properties.Resources", typeof(PatternSprite).Assembly);
    private static readonly Dictionary<ushort, Image?> Images = new();

    public static Image? GetPattern(ushort pattern)
    {
        if (pattern is 0 or > ItemPaintInfo.PatternCount)
            return null;
        if (Images.TryGetValue(pattern, out var image))
            return image;
        image = Resources.GetObject($"pattern_{pattern}") as Image;
        Images.Add(pattern, image);
        return image;
    }
}
