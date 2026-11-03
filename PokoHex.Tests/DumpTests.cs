using System;
using System.IO;
using System.Linq;
using PokoHex.Core;
using PokoHex.Parsing;
using Xunit;

namespace PokoHex.Tests;

public static class DumpTests
{
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
