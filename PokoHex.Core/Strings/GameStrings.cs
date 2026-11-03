using System.Collections.Generic;

namespace PokoHex.Core;

public sealed class GameStrings
{
    public readonly string[] itemlist;
    public readonly string[] villagerlist;
    public readonly string[] arealist;
    public readonly string[] skilllist;
    public readonly string[] reactionlist;
    //public readonly string[] uselist;
    public readonly ItemInfo[] ItemDataSource;
    public readonly AbilityInfo[] SkillDataSource;
    public readonly AbilityInfo[] ReactionDataSource;

    public GameStrings(string language)
    {
        itemlist = GameLanguage.GetStrings("item", language);
        villagerlist = GameLanguage.GetStrings("villager", language);
        arealist = GameLanguage.GetStrings("area", language);
        skilllist = GameLanguage.GetStrings("skill", language);
        reactionlist = GameLanguage.GetStrings("reaction", language);
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
}
