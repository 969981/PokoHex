using System.Collections.Generic;

namespace PokoHex.Core;

public sealed class GameStrings
{
    public readonly string[] itemlist;
    public readonly string[] villagerlist;
    public readonly string[] arealist;
    public readonly string[] skilllist;
    public readonly string[] reactionlist;
    public readonly string[] photooplist;
    public readonly string[] photoframelist;
    public readonly string[] photofilterlist;
    //public readonly string[] uselist;
    public readonly ItemInfo[] ItemDataSource;
    public readonly AbilityInfo[] SkillDataSource;
    public readonly AbilityInfo[] ReactionDataSource;
    public readonly ItemInfo[] PhotoOpDataSource;
    public readonly ItemInfo[] PhotoFrameDataSource;
    public readonly ItemInfo[] PhotoFilterDataSource;

    public GameStrings(string language)
    {
        itemlist = GameLanguage.GetStrings("item", language);
        villagerlist = GameLanguage.GetStrings("villager", language);
        arealist = GameLanguage.GetStrings("area", language);
        skilllist = GameLanguage.GetStrings("skill", language);
        reactionlist = GameLanguage.GetStrings("reaction", language);
        photooplist = GameLanguage.GetStrings("photoop", language);
        photoframelist = GameLanguage.GetStrings("photoframe", language);
        photofilterlist = GameLanguage.GetStrings("photofilter", language);
        PhotoFrameDataSource = GetPhotoStyles(photoframelist);
        PhotoFilterDataSource = GetPhotoStyles(photofilterlist);
        var photos = new List<ItemInfo>();
        for (int i = 1; i < photooplist.Length; i++)
        {
            if (photooplist[i].Length != 0)
                photos.Add(new ItemInfo((ushort)i, photooplist[i]));
        }
        PhotoOpDataSource = photos.ToArray();
        var skills = new List<AbilityInfo>();
        var reactions = new List<AbilityInfo>();
        for (int i = 0; i < skilllist.Length; i++)
        {
            if (Skill.IsPublic(i) && skilllist[i].Length != 0)
                skills.Add(new AbilityInfo((byte)i, skilllist[i]));
        }
        for (int i = 0; i < reactionlist.Length; i++)
        {
            if (Reaction.IsPublic(i) && reactionlist[i].Length != 0)
                reactions.Add(new AbilityInfo((byte)i, reactionlist[i]));
        }
        SkillDataSource = skills.ToArray();
        ReactionDataSource = reactions.ToArray();
        var items = new List<ItemInfo>();
        for (int i = 0; i < itemlist.Length; i++)
        {
            if (itemlist[i].Length != 0)
                items.Add(new ItemInfo(checked((ushort)i), itemlist[i]));
        }
        ItemDataSource = items.ToArray();
    }

    private static ItemInfo[] GetPhotoStyles(string[] names)
    {
        var result = new List<ItemInfo> { new(0, "None") };
        for (int i = 1; i < names.Length; i++)
        {
            if (names[i].Length != 0)
                result.Add(new ItemInfo((ushort)i, names[i]));
        }
        return result.ToArray();
    }
}
