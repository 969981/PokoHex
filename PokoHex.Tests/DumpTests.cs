using System;
using System.IO;
using System.Linq;
using PokoHex.Core;
using PokoHex.Parsing;
using Xunit;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Tests;

public static class DumpTests
{
    [Fact]
    public static void ReadCatalogueTables()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        Assert.Equal(ResourceUtil.GetBinaryResource("catalogue_dex.bin"), GameCatalogueDumper.GetPokedexMap(path!));
        Assert.Equal(ResourceUtil.GetBinaryResource("catalogue_recipes.bin"), GameCatalogueDumper.GetRecipeMap(path!));
        Assert.Equal(ResourceUtil.GetBinaryResource("catalogue_cooking.bin"), GameCatalogueDumper.GetCookingMap(path!));
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var strings = GameInfo.GetStrings(language);
            Assert.Equal(GameCatalogueDumper.GetPokedexList(path!, language), strings.dexlist);
            Assert.Equal(GameCatalogueDumper.GetHabitatList(path!, language), strings.habitatlist);
        }
        Assert.Equal(374, CatalogueInfo.GetEntries(CatalogueType.Pokedex).Length);
        Assert.Equal(257, CatalogueInfo.GetEntries(CatalogueType.Habitats).Length);
        Assert.Equal(921, CatalogueInfo.GetEntries(CatalogueType.Crafting).Length);
        Assert.Equal(34, CatalogueInfo.GetEntries(CatalogueType.Cooking).Length);
    }

    [Fact]
    public static void DumpCatalogueResources()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        var destination = Environment.GetEnvironmentVariable("POKOHEX_RESOURCE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(destination), "Set POKOHEX_RESOURCE_PATH to the resource output folder.");
        GameCatalogueDumper.UpdateDumps(path!, destination!);
        Assert.Equal(GameCatalogueDumper.GetPokedexMap(path!), File.ReadAllBytes(Path.Combine(destination!, "byte", "catalogue_dex.bin")));
        Assert.Equal(GameCatalogueDumper.GetRecipeMap(path!), File.ReadAllBytes(Path.Combine(destination!, "byte", "catalogue_recipes.bin")));
        Assert.Equal(GameCatalogueDumper.GetCookingMap(path!), File.ReadAllBytes(Path.Combine(destination!, "byte", "catalogue_cooking.bin")));
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var folder = Path.Combine(destination!, "text", language);
            Assert.Equal(GameCatalogueDumper.GetPokedexList(path!, language), File.ReadAllLines(Path.Combine(folder, $"text_dex_{language}.txt")));
            Assert.Equal(GameCatalogueDumper.GetHabitatList(path!, language), File.ReadAllLines(Path.Combine(folder, $"text_habitat_{language}.txt")));
        }
    }

    [Fact]
    public static void ReadPhotoOpTables()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var strings = GameInfo.GetStrings(language);
            Assert.Equal(GamePhotoDumper.GetPhotoOpList(path!, language), strings.photooplist);
            Assert.Equal(GamePhotoDumper.GetFrameList(path!, language), strings.photoframelist);
            Assert.Equal(GamePhotoDumper.GetFilterList(path!, language), strings.photofilterlist);
            Assert.Equal(50, strings.PhotoOpDataSource.Length);
            Assert.Equal(31, strings.PhotoFrameDataSource.Length);
            Assert.Equal(9, strings.PhotoFilterDataSource.Length);
        }
    }

    [Fact]
    public static void ReadPhotoSave()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_SAVE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_SAVE_PATH to a save folder.");
        var file = Path.Combine(path!, "G00", "SCSHDAT.BIN");
        Assert.SkipUnless(File.Exists(file), "The save folder has no photo file.");
        var original = File.ReadAllBytes(file);
        var photos = ScreenshotSave.Read(original);
        Assert.Equal(ScreenshotSave.SlotCount, photos.Entries.Length);
        Assert.Equal(original, photos.Write());
        Assert.Equal(original, File.ReadAllBytes(file));
    }

    [Fact]
    public static void DumpPhotoOpResources()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        var destination = Environment.GetEnvironmentVariable("POKOHEX_RESOURCE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(destination), "Set POKOHEX_RESOURCE_PATH to the resource output folder.");
        GamePhotoDumper.UpdateDumps(path!, destination!);
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var file = Path.Combine(destination!, "text", language, $"text_photoop_{language}.txt");
            Assert.Equal(GamePhotoDumper.GetPhotoOpList(path!, language), File.ReadAllLines(file));
            Assert.Equal(GamePhotoDumper.GetFrameList(path!, language), File.ReadAllLines(Path.Combine(destination!, "text", language, $"text_photoframe_{language}.txt")));
            Assert.Equal(GamePhotoDumper.GetFilterList(path!, language), File.ReadAllLines(Path.Combine(destination!, "text", language, $"text_photofilter_{language}.txt")));
        }
    }

    [Fact]
    public static void ReadPaintAndBuildingTables()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        Assert.Equal(GamePaintDumper.GetPaintList(path!), ResourceUtil.GetBinaryResource("item_paint.bin"));
        Assert.Equal(GameBuildingDumper.GetBuildingList(path!), ResourceUtil.GetBinaryResource("map_buildings.bin"));
        Assert.Equal(3, ItemPaintInfo.GetColorCount(1510));
        Assert.True(ItemPaintInfo.AllowsPattern(1510));
        Assert.Equal(1870, MapBuildingInfo.Get(31).ItemId);
    }

    [Fact]
    public static void ReadArchiveEntries()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        using var archive = new GameArchive(GameItemDumper.GetArchiveFolder(path!));
        Assert.SkipUnless(archive.EntryCount == 12298, "These archive samples use the update game dump.");
        Assert.Empty(archive.Read(5595));
        Assert.Empty(archive.Read(10062));
        Assert.Empty(archive.Read(11596));
        Assert.Equal(568, archive.Read(5918).Length);
        Assert.Equal(1_863_044, archive.Read(5907).Length);
        Assert.Equal(34_480_304, archive.Read(5879).Length);
        Assert.Equal(Convert.FromHexString("0000000000000000010000000100DD010000"), archive.Read(10022));
        var mixed = archive.Read(10220);
        Assert.Equal(65550, mixed.Length);
        Assert.Equal(Convert.FromHexString("0000050085010000050085010000"), mixed.AsSpan(mixed.Length - 14).ToArray());
    }

    [Fact]
    public static void ReadAbilityTables()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var strings = GameInfo.GetStrings(language);
            Assert.Equal(GameAbilityDumper.GetSkillList(path!, language), strings.skilllist);
            Assert.Equal(GameAbilityDumper.GetReactionList(path!, language), strings.reactionlist);
            Assert.Equal(10, strings.SkillDataSource.Length);
            Assert.Equal(47, strings.ReactionDataSource.Length);
        }
    }

    [Fact]
    public static void DumpAbilityResources()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        var destination = Environment.GetEnvironmentVariable("POKOHEX_RESOURCE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(destination), "Set POKOHEX_RESOURCE_PATH to the resource output folder.");
        GameAbilityDumper.UpdateDumps(path!, destination!);
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var folder = Path.Combine(destination!, "text", language);
            Assert.Equal(GameAbilityDumper.GetSkillList(path!, language), File.ReadAllLines(Path.Combine(folder, $"text_skill_{language}.txt")));
            Assert.Equal(GameAbilityDumper.GetReactionList(path!, language), File.ReadAllLines(Path.Combine(folder, $"text_reaction_{language}.txt")));
        }
    }

    [Fact]
    public static void ReadMapParts()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        var parts = GameMapDumper.GetPartList(path!);
        Assert.Equal(ResourceUtil.GetBinaryResource("map_parts.bin"), parts);
        Assert.Equal(1357, MapPartInfo.Get(1498).ItemId);
        Assert.Equal(2, MapPartInfo.Get(1498).Width);
        Assert.Equal(1628, MapPartInfo.Get(3197).ItemId);
    }

    [Fact]
    public static void DumpMapResources()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        var destination = Environment.GetEnvironmentVariable("POKOHEX_RESOURCE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to the update game dump.");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(destination), "Set POKOHEX_RESOURCE_PATH to the resource output folder.");
        GameMapDumper.UpdateDumps(path!, destination!);
        Assert.Equal(GameMapDumper.GetPartList(path!), File.ReadAllBytes(Path.Combine(destination!, "byte", "map_parts.bin")));
    }

    [Fact]
    public static void ReadStageSave()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_SAVE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_SAVE_PATH to a save folder.");
        var save = SaveFolder.Read(path!);
        var file = Path.Combine(save.Folder, "G00", "STGDAT01.BIN");
        Assert.SkipUnless(File.Exists(file), "The save has no first area.");
        var data = File.ReadAllBytes(file);
        var stage = save.GetStage(1);
        Assert.NotEmpty(stage.Parts);
        Assert.NotEmpty(stage.Chunks);
        Assert.Equal(data, stage.Write());
        Assert.Equal(data, File.ReadAllBytes(file));
    }

    [Fact]
    public static void ReadItemTables()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to a game dump.");
        var items = GameItemDumper.ReadItems(path!);
        Assert.InRange(items.Length, 1000, 4600);
        Assert.Equal(items.Length, items.Select(z => z.Value).Distinct().Count());
        Assert.Contains(items, z => z.Value == 404 && z.Text.Length != 0);
        Assert.Contains(items, z => z.Value == 1419 && z.Text.Length != 0);
    }

    [Fact]
    public static void DumpItemResources()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        var destination = Environment.GetEnvironmentVariable("POKOHEX_RESOURCE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to a game dump.");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(destination), "Set POKOHEX_RESOURCE_PATH to the resource output folder.");
        GameItemDumper.UpdateDumps(path!, destination!);
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var items = File.ReadAllLines(Path.Combine(destination!, "text", language, $"text_item_{language}.txt"));
            Assert.Equal(GameItemDumper.GetItemList(path!, language), items);
        }
    }

    [Fact]
    public static void ReadEmbeddedItems()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_GAME_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_GAME_PATH to a game dump.");
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var strings = GameInfo.GetStrings(language);
            Assert.Equal(GameItemDumper.GetItemList(path!, language), strings.itemlist);
            Assert.Equal(GameItemDumper.ReadItems(path!, language), strings.ItemDataSource);
        }
    }

    [Fact]
    public static void RegisterInventoryItem()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_SAVE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_SAVE_PATH to a save folder.");
        var save = SaveFolder.Read(path!);
        var common = save.Common;
        var layout = common.Layout;
        Assert.SkipUnless(layout.ObtainedItemsOffset != 0, "This save has no supported item Collection history.");
        var file = Path.Combine(save.Folder, "G00", "CMNDAT.BIN");
        var original = File.ReadAllBytes(file);
        var expected = (byte[])common.Data.Clone();
        var entry = GameInfo.Strings.ItemDataSource.FirstOrDefault(z => z.Value != 0 && z.Value < SaveLayout.ObtainedItemsCount && expected[layout.ObtainedItemsOffset + z.Value] == 0);
        Assert.SkipUnless(entry != null, "This save has no unregistered catalog item.");
        var id = entry!.Value;
        var pending = new Item(new byte[Item.SIZE]);
        pending.Set(id, 1);
        Assert.Equal(0, common.Data[layout.ObtainedItemsOffset + id]);
        common.Inventory[0].RawValue = pending.RawValue;
        common.Inventory[0].Count = pending.Count;
        WriteUInt32LittleEndian(expected.AsSpan(layout.InventoryOffset), pending.RawValue);
        expected[layout.InventoryOffset + Item.SIZE - 1] = 1;
        expected[layout.ObtainedItemsOffset + id] = 0x43;
        Assert.Equal(expected, SaveFile.Read(common.Write()).Data);
        Assert.Equal(original, File.ReadAllBytes(file));
    }

    [Fact]
    public static void EditSaveCopy()
    {
        var path = Environment.GetEnvironmentVariable("POKOHEX_SAVE_PATH");
        Assert.SkipUnless(Directory.Exists(path), "Set POKOHEX_SAVE_PATH to a save folder.");
        var save = SaveFolder.Read(path!);
        var file = Path.Combine(save.Folder, "G00", "CMNDAT.BIN");
        var original = File.ReadAllBytes(file);
        Assert.Equal(original, save.Common.Write());
        var before = (byte[])save.Common.Data.Clone();
        var name = save.Common.PlayerName;
        Assert.NotEmpty(name);
        save.Common.PlayerName = "Editor Test";
        save.Common.Inventory[0].Set(404, 99);
        var written = save.Common.Write();
        var reopened = SaveFile.Read(written);
        Assert.Equal("Editor Test", reopened.PlayerName);
        Assert.Equal(404, reopened.Inventory[0].ItemId);
        Assert.Equal(99, reopened.Inventory[0].Count);
        Assert.Equal(save.Common.Data, reopened.Data);
        var layout = reopened.Layout;
        for (int i = 0; i < before.Length; i++)
        {
            if (i >= layout.PlayerNameOffset && i < layout.PlayerNameOffset + (layout.PlayerNameSize * layout.PlayerNameCount))
                continue;
            if (i >= layout.InventoryOffset && i < layout.InventoryOffset + Item.SIZE)
                continue;
            Assert.Equal(before[i], reopened.Data[i]);
        }
        Assert.Equal(original, File.ReadAllBytes(file));
    }
}
