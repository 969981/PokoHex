using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using PokoHex.Core;

namespace PokoHex;

public static class AbilitySprite
{
    private static readonly ResourceManager Resources = new("PokoHex.Properties.Resources", typeof(AbilitySprite).Assembly);
    private static readonly Dictionary<string, Image?> Images = new();

    public static Image? GetSkillSprite(int index) => Skill.IsPublic(index) ? GetSprite($"skill_{index:0000}") : null;
    public static Image? GetReactionSprite(int index) => Reaction.IsPublic(index) ? GetSprite($"reaction_{index:0000}") : null;

    private static Image? GetSprite(string name)
    {
        if (Images.TryGetValue(name, out var image))
            return image;
        image = Resources.GetObject(name) as Image;
        Images.Add(name, image);
        return image;
    }
}
