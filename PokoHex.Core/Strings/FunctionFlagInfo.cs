using System.Collections.Generic;

namespace PokoHex.Core;

public static class FunctionFlagInfo
{
    // These labels describe known behavior; the game stores numeric flag IDs.
    private static readonly IReadOnlyDictionary<int, string> Names = new Dictionary<int, string>
    {
        [3] = "Workbench unlocked",
        [7] = "Poké Center PC unlocked",
        [18] = "Wake at home on load",
        [27] = "PC challenges unlocked",
        [28] = "PC Link Play unlocked",
        [30] = "PC shop unlocked",
        [63] = "Inventory upgrade 1",
        [64] = "Inventory upgrade 2",
        [65] = "Inventory upgrade 3",
        [66] = "Inventory upgrade 4",
        [76] = "Pause menu unlocked",
        [77] = "Patterns unlocked (requires 79)",
        [78] = "Multiple paint areas unlocked (requires 79)",
        [79] = "Painting unlocked",
        [82] = "Jump unlocked",
        [102] = "PP upgrade 1",
        [103] = "PP upgrade 2",
        [104] = "PP upgrade 3",
        [105] = "PP upgrade 4",
        [107] = "Secondary bar unlocked",
        [108] = "PC stamps unlocked",
        [114] = "PC appraisal unlocked",
        [133] = "PC environment level unlocked",
        [135] = "Workbench Craft option",
    };

    public static bool IsKnown(int index) => index is 54 or 56 or 57 or 71 or 72 or 73 or 74 or 125 or 148 or 153 || Names.ContainsKey(index);

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
        _ => Names.TryGetValue(index, out var name) ? name : $"function_{index}",
    };
}
