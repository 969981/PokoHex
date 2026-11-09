namespace PokoHex.Core;

public static class FunctionFlagInfo
{
    // These labels describe known behavior; the game stores numeric flag IDs.
    public static bool IsKnown(int index) => index is 54 or 56 or 57 or 71 or 72 or 73 or 74 or 125 or 148 or 153;

    public static string GetName(int index) => index switch
    {
        54 => "Liquid absorption",
        56 => $"{GameInfo.Strings.skilllist[10]} unlocked",
        57 => $"{GameInfo.Strings.skilllist[11]} unlocked",
        71 => $"{GameInfo.Strings.skilllist[1]} food boost",
        72 => $"{GameInfo.Strings.skilllist[5]} food boost",
        73 => $"{GameInfo.Strings.skilllist[3]} food boost",
        74 => $"{GameInfo.Strings.skilllist[4]} food boost",
        125 => $"{GameInfo.Strings.skilllist[4]} extra food effect",
        148 => "Dive unlocked",
        153 => $"{GameInfo.Strings.skilllist[10]} food boost",
        _ => $"function_{index}",
    };
}
