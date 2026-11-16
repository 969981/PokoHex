namespace PokoHex.Core;

public static class PhotoStyleInfo
{
    public static bool IsPublic(int index, bool filters = false)
    {
        var strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
        var names = filters ? strings.photofilterlist : strings.photoframelist;
        return (uint)index < names.Length && (index == 0 || names[index].Length != 0);
    }

    public static string GetName(int index, bool filters = false)
    {
        if (index == 0)
            return "None";
        var names = filters ? GameInfo.Strings.photofilterlist : GameInfo.Strings.photoframelist;
        return (uint)index < names.Length && names[index].Length != 0 ? names[index] : $"{(filters ? "Filter" : "Frame")} {index}";
    }
}
