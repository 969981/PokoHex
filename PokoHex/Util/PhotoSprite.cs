using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Resources;
using PokoHex.Core;

namespace PokoHex;

public static class PhotoSprite
{
    private static readonly ResourceManager Resources = new("PokoHex.Properties.Resources", typeof(PhotoSprite).Assembly);
    private static readonly Dictionary<string, Image?> Images = new();

    public static Image? GetCamera() => GetSprite("photo_camera");
    public static Image? GetPlaceholder(bool unavailable = false) => GetSprite(unavailable ? "photo_unavailable" : "photo_empty");
    public static Image? GetStyle(int index, bool filters = false) => PhotoStyleInfo.IsPublic(index, filters) ? GetSprite($"photo_{(filters ? "filter" : "frame")}_{index:D2}") : null;

    public static Image? GetFramePreview(int index)
    {
        if (!PhotoStyleInfo.IsPublic(index) || index == 0 || Resources.GetObject($"photo_frame_full_{index:D2}") is not byte[] data)
            return null;
        using var stream = new MemoryStream(data, false);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static Image? GetSprite(string name)
    {
        if (Images.TryGetValue(name, out var image))
            return image;
        image = Resources.GetObject(name) as Image;
        Images.Add(name, image);
        return image;
    }
}
