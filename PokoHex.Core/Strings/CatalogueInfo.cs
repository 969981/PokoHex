using System;
using System.Linq;

namespace PokoHex.Core;

public static class CatalogueInfo
{
    private static readonly ushort[] DexRows = ResourceUtil.GetBinaryResourceAsUshort("catalogue_dex.bin");
    private static readonly ushort[] Recipes = ResourceUtil.GetBinaryResourceAsUshort("catalogue_recipes.bin");
    private static readonly ushort[] Cooking = ResourceUtil.GetBinaryResourceAsUshort("catalogue_cooking.bin");
    private static readonly ushort[] Residents = ResourceUtil.GetBinaryResourceAsUshort("catalogue_residents.bin");

    public static bool IsPublic(CatalogueType type, int index)
    {
        if (index <= 0)
            return false;
        var strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
        return type switch
        {
            CatalogueType.Pokedex => index < DexRows.Length && DexRows[index] != ushort.MaxValue && strings.dexlist[index].Length != 0,
            CatalogueType.Habitats => index < strings.habitatlist.Length && strings.habitatlist[index].Length != 0,
            CatalogueType.Crafting => index < Recipes.Length && Recipes[index] != 0 && Recipes[index] < strings.itemlist.Length && strings.itemlist[Recipes[index]].Length != 0,
            CatalogueType.Cooking => Cooking.Contains((ushort)index) && index < strings.itemlist.Length && strings.itemlist[index].Length != 0,
            _ => false,
        };
    }

    public static ItemInfo[] GetEntries(CatalogueType type)
    {
        var count = type switch
        {
            CatalogueType.Pokedex => Catalogue.PokedexCount,
            CatalogueType.Habitats => GameInfo.Strings.habitatlist.Length,
            CatalogueType.Crafting => Catalogue.RecipeCount,
            CatalogueType.Cooking => GameInfo.Strings.itemlist.Length,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        return Enumerable.Range(1, count - 1).Where(i => IsPublic(type, i)).Select(i => new ItemInfo((ushort)i, GetName(type, i))).ToArray();
    }

    public static string GetName(CatalogueType type, int index)
    {
        var strings = GameInfo.Strings;
        return type switch
        {
            CatalogueType.Pokedex => GetPokedexName(index),
            CatalogueType.Habitats => strings.habitatlist[index],
            CatalogueType.Crafting => strings.itemlist[GetItem(type, index)],
            CatalogueType.Cooking => strings.itemlist[index],
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static string GetPokedexName(int index)
    {
        var names = GameInfo.Strings.villagerlist;
        var resident = GetResident(index);
        return resident > 0 && names[resident].Length != 0 ? names[resident] : GameInfo.Strings.dexlist[index];
    }

    public static ushort GetItem(CatalogueType type, int index) => type == CatalogueType.Cooking ? (ushort)index : type == CatalogueType.Crafting && (uint)index < Recipes.Length ? Recipes[index] : (ushort)0;
    public static int GetResident(int index) => (uint)index < Residents.Length ? Residents[index] : 0;
    public static int[] GetCookingVariants(int item) => Enumerable.Range(1, Cooking.Length - 1).Where(i => Cooking[i] == item).ToArray();
    public static string GetTitle(CatalogueType type) => type switch
    {
        CatalogueType.Pokedex => "Pokédex",
        CatalogueType.Habitats => "Habitats",
        CatalogueType.Crafting => "Crafting",
        CatalogueType.Cooking => "Cooking",
        _ => "Catalogue",
    };

    public static string GetStatus(CatalogueType type, int state) => type switch
    {
        CatalogueType.Pokedex => state == 2 ? "Registered" : state == 1 ? "Seen" : "Not seen",
        CatalogueType.Habitats => state == 2 ? "Built" : state == 1 ? "Discovered" : "Undiscovered",
        CatalogueType.Crafting => state == 2 ? "Learned" : "Not learned",
        CatalogueType.Cooking => state == 2 ? "Known" : "Unknown",
        _ => string.Empty,
    };
    public static string GetAction(CatalogueType type, bool all = false) => type switch
    {
        CatalogueType.Pokedex => all ? "Register all" : "Register",
        CatalogueType.Habitats => all ? "Record all built" : "Record built",
        CatalogueType.Crafting => all ? "Learn all" : "Learn",
        CatalogueType.Cooking => all ? "Mark all known" : "Mark known",
        _ => string.Empty,
    };
}
