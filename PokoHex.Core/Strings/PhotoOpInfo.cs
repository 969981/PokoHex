namespace PokoHex.Core;

public static class PhotoOpInfo
{
    public static bool IsPublic(int index)
    {
        var names = GameInfo.GetStrings(GameLanguage.DefaultLanguage).photooplist;
        return index > 0 && index < names.Length && names[index].Length != 0;
    }

    public static string GetName(int index)
    {
        var names = GameInfo.Strings.photooplist;
        return (uint)index < names.Length && names[index].Length != 0 ? names[index] : $"Photo Op {index}";
    }
}
