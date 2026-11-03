using PokoHex.Core;

namespace PokoHex.Web;

public static class SpriteUtil
{
    public static string? GetSkill(int index) => Skill.IsPublic(index) ? $"images/skills/skill_{index:D4}.png" : null;
    public static string? GetReaction(int index) => Reaction.IsPublic(index) ? $"images/reactions/reaction_{index:D4}.png" : null;

    public static string? GetItem(ushort id)
    {
        if (id == 0)
            return null;
        var icon = ItemInfo.GetMenuIcon(id);
        return $"images/items/item_{(icon == 0 ? 565 : icon):D4}.png";
    }

    public static string? GetVillager(int index)
    {
        var portrait = VillagerInfo.GetPortrait(index);
        return portrait == 0 ? null : $"images/villagers/villager_{portrait:D4}.png";
    }

    public static string GetItemName(ushort id)
    {
        var names = GameInfo.Strings.itemlist;
        return id == 0 ? "(Empty)" : id < names.Length && names[id].Length != 0 ? names[id] : $"Item {id:D4}";
    }

    public static string GetVillagerName(int index)
    {
        var names = GameInfo.Strings.villagerlist;
        return (uint)index < names.Length && names[index].Length != 0 ? names[index] : $"Villager {index:D4}";
    }

    public static string GetAreaName(int area)
    {
        var names = GameInfo.Strings.arealist;
        return area == 0 ? "No home" : (uint)area < names.Length && names[area].Length != 0 ? $"{names[area]} ({area})" : $"Area {area}";
    }
}
