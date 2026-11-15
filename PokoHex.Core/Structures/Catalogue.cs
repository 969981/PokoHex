using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed class Catalogue(Memory<byte> pokedex, Memory<byte> habitats, Memory<byte> recipes, Memory<byte> cooking)
{
    public const int PokedexCount = 1024;
    public const int HabitatCount = 512;
    public const int HabitatSize = 12;
    public const int RecipeCount = 1500;
    public const int RecipeSize = 6;
    public const int CookingCount = 101;
    private readonly Stack<Change[]> Undo = [];
    private readonly Stack<Change[]> Redo = [];
    private sealed record Change(Memory<byte> Target, byte[] Before, byte[] After);
    public bool CanUndo => Undo.Count != 0;
    public bool CanRedo => Redo.Count != 0;

    public int GetState(CatalogueType type, int index)
    {
        Validate(type, index);
        return type switch
        {
            CatalogueType.Pokedex => (pokedex.Span[index] & 3) == 3 ? 2 : (pokedex.Span[index] & 8) != 0 ? 1 : 0,
            CatalogueType.Habitats => (habitats.Span[index * HabitatSize] & 2) != 0 ? 2 : (habitats.Span[index * HabitatSize] & 1) != 0 ? 1 : 0,
            CatalogueType.Crafting => (recipes.Span[index * RecipeSize + 4] & 2) != 0 ? 2 : 0,
            CatalogueType.Cooking => CatalogueInfo.GetCookingVariants(index).Any(i => (cooking.Span[i] & 1) != 0) ? 2 : 0,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    public bool CanUnlock(CatalogueType type, int index, bool partial = false) =>
        GetState(type, index) < (partial ? 1 : 2) && (!partial || type is CatalogueType.Pokedex or CatalogueType.Habitats);

    public void Unlock(CatalogueType type, int index, bool partial = false)
    {
        Validate(type, index);
        if (CanUnlock(type, index, partial))
            Unlock(type, [index], partial);
    }

    public void UnlockAll(CatalogueType type)
    {
        var ids = CatalogueInfo.GetEntries(type).Where(z => CanUnlock(type, z.Value)).Select(z => (int)z.Value).ToArray();
        Unlock(type, ids, false);
    }

    private void Unlock(CatalogueType type, int[] ids, bool partial)
    {
        if (ids.Length == 0)
            return;
        var order = 0;
        if (type == CatalogueType.Crafting)
        {
            for (int i = 0; i < RecipeCount; i++)
                order = Math.Max(order, ReadUInt16LittleEndian(recipes.Span[(i * RecipeSize + 2)..]));
            if (order + ids.Length > ushort.MaxValue)
                throw new ArgumentException("There’s no room for more recipes in the learned order.");
        }
        var changes = new List<Change>();
        foreach (var id in ids)
        {
            var target = type switch
            {
                CatalogueType.Pokedex => pokedex.Slice(id, 1),
                CatalogueType.Habitats => habitats.Slice(id * HabitatSize, 1),
                CatalogueType.Crafting => recipes.Slice(id * RecipeSize, RecipeSize),
                CatalogueType.Cooking => cooking.Slice(CatalogueInfo.GetCookingVariants(id)[0], 1),
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
            var before = target.ToArray();
            var after = (byte[])before.Clone();
            if (type == CatalogueType.Crafting)
            {
                WriteUInt16LittleEndian(after.AsSpan(2), (ushort)++order);
                after[4] |= 2;
            }
            else
            {
                byte mask = type switch
                {
                    CatalogueType.Pokedex => partial ? (byte)8 : (byte)3,
                    CatalogueType.Habitats => partial ? (byte)1 : (byte)3,
                    _ => 1,
                };
                after[0] |= mask;
            }
            changes.Add(new Change(target, before, after));
        }
        // Validate and gather the whole action before changing the save.
        var edit = changes.ToArray();
        Apply(edit, false);
        Undo.Push(edit);
        Redo.Clear();
    }

    private static void Apply(Change[] changes, bool restore)
    {
        foreach (var change in changes)
            (restore ? change.Before : change.After).CopyTo(change.Target.Span);
    }

    public void UndoEdit()
    {
        if (!Undo.TryPop(out var edit))
            return;
        Apply(edit, true);
        Redo.Push(edit);
    }

    public void RedoEdit()
    {
        if (!Redo.TryPop(out var edit))
            return;
        Apply(edit, false);
        Undo.Push(edit);
    }

    private static void Validate(CatalogueType type, int index)
    {
        if (!CatalogueInfo.IsPublic(type, index))
            throw new ArgumentException("Select a catalogue entry.");
    }
}
