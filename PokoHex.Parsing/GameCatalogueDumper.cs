using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PokoHex.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Parsing;

public static class GameCatalogueDumper
{
    public static byte[] GetPokedexMap(string path)
    {
        var table = ReadTable(path, 1066, 1024, 3, 104);
        var result = new byte[Catalogue.PokedexCount * 2];
        result.AsSpan().Fill(0xFF);
        for (int i = 0; i < 1024; i++)
        {
            var row = table.AsSpan(64 + i * 104, 104);
            var slot = ReadUInt16LittleEndian(row[40..]);
            if (slot == 0)
                continue;
            if (ReadUInt16LittleEndian(row[24..]) != i || slot >= 1000 || ReadUInt16LittleEndian(result.AsSpan(slot * 2)) != ushort.MaxValue)
                throw new InvalidDataException("The Pokédex slot is invalid or duplicated.");
            WriteUInt16LittleEndian(result.AsSpan(slot * 2), (ushort)i);
        }
        return result;
    }

    public static byte[] GetRecipeMap(string path) => GetItemMap(path, 493, Catalogue.RecipeCount, 44, 18, 0);
    public static byte[] GetCookingMap(string path) => GetItemMap(path, 490, Catalogue.CookingCount, 20, 0, 4);

    private static byte[] GetItemMap(string path, int index, int count, int stride, int offset, int padding)
    {
        var table = ReadTable(path, index, count, 0, stride, padding);
        var result = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            var item = ReadUInt16LittleEndian(table.AsSpan(64 + i * stride + offset));
            if (item >= 4600)
                throw new InvalidDataException("The recipe item is outside the supported item table.");
            WriteUInt16LittleEndian(result.AsSpan(i * 2), item);
        }
        return result;
    }

    public static string[] GetPokedexList(string path, string language = GameLanguage.DefaultLanguage)
    {
        var names = ReadNames(path, 4933 + GetLanguage(language), 1024, 8);
        var map = GetPokedexMap(path);
        var result = new string[Catalogue.PokedexCount];
        for (int i = 0; i < result.Length; i++)
        {
            var row = ReadUInt16LittleEndian(map.AsSpan(i * 2));
            result[i] = row == ushort.MaxValue ? string.Empty : names[row];
        }
        return result;
    }

    public static string[] GetHabitatList(string path, string language = GameLanguage.DefaultLanguage) => ReadNames(path, 4537 + GetLanguage(language), 501, 12);

    private static int GetLanguage(string language) => language switch
    {
        "jp" => 0, "en" => 1, "fr" => 2, "it" => 3, "de" => 4,
        "es" => 5, "zht" => 6, "ko" => 7, "zhs" => 8,
        _ => throw new ArgumentException("This language is not supported.", nameof(language)),
    };

    private static string[] ReadNames(string path, int index, int count, int stride)
    {
        using var archive = OpenArchive(path);
        var data = archive.Read(index);
        if (data.Length < 64 + count * stride || ReadInt32LittleEndian(data) != count || ReadInt32LittleEndian(data.AsSpan(4)) != 2)
            throw new InvalidDataException("The catalogue text table is not supported.");
        var result = new string[count];
        for (int i = 0; i < count; i++)
        {
            var field = 64 + i * stride;
            var offset = (long)field + ReadUInt32LittleEndian(data.AsSpan(field));
            if (offset < 64 + count * stride || offset >= data.Length)
                throw new InvalidDataException("The catalogue name offset is invalid.");
            var text = data.AsSpan((int)offset);
            var end = text.IndexOf((byte)0);
            if (end < 0)
                throw new InvalidDataException("The catalogue name is not terminated.");
            var name = new UTF8Encoding(false, true).GetString(text[..end]);
            result[i] = Regex.Replace(name, "<([^<>:]+):[^<>]+>", "$1");
        }
        return result;
    }

    private static byte[] ReadTable(string path, int index, int count, int version, int stride, int padding = 0)
    {
        using var archive = OpenArchive(path);
        var data = archive.Read(index);
        if (data.Length != 64 + count * stride + padding || ReadInt32LittleEndian(data) != count || ReadInt32LittleEndian(data.AsSpan(4)) != version)
            throw new InvalidDataException("The catalogue parameter table is not supported.");
        return data;
    }

    private static GameArchive OpenArchive(string path)
    {
        var archive = new GameArchive(GameItemDumper.GetArchiveFolder(path));
        if (archive.EntryCount == 12298)
            return archive;
        archive.Dispose();
        throw new InvalidDataException("The catalogue data version is not supported.");
    }

    public static void UpdateDumps(string path, string destination)
    {
        Directory.CreateDirectory(Path.Combine(destination, "byte"));
        File.WriteAllBytes(Path.Combine(destination, "byte", "catalogue_dex.bin"), GetPokedexMap(path));
        File.WriteAllBytes(Path.Combine(destination, "byte", "catalogue_recipes.bin"), GetRecipeMap(path));
        File.WriteAllBytes(Path.Combine(destination, "byte", "catalogue_cooking.bin"), GetCookingMap(path));
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var folder = Path.Combine(destination, "text", language);
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, $"text_dex_{language}.txt"), GetPokedexList(path, language), new UTF8Encoding(false));
            File.WriteAllLines(Path.Combine(folder, $"text_habitat_{language}.txt"), GetHabitatList(path, language), new UTF8Encoding(false));
        }
    }
}
