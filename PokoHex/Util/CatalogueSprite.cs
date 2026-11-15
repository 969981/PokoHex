using System.Drawing;
using System.Resources;
using PokoHex.Core;

namespace PokoHex;

public static class CatalogueSprite
{
    private static readonly ResourceManager Resources = new("PokoHex.Properties.Resources", typeof(CatalogueSprite).Assembly);
    private static readonly Image? Habitat = Resources.GetObject("catalogue_habitat") as Image;

    public static Image? GetSprite(CatalogueType type, int index) => type switch
    {
        CatalogueType.Pokedex => VillagerSprite.GetVillagerSprite(CatalogueInfo.GetResident(index)),
        CatalogueType.Habitats => Habitat,
        CatalogueType.Crafting or CatalogueType.Cooking => ItemSprite.GetItemSprite(CatalogueInfo.GetItem(type, index)),
        _ => null,
    };
}
