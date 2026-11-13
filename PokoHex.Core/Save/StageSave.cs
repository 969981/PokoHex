using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed class StageSave
{
    public const uint Version = 0x0300018B;
    public const int HeaderSize = 0x124;
    public const int DataSize = 0x83FFDB8;
    private const int ChunkMap = 0x58571B;
    private const int ChunkCount = 0x58771B;
    private const int PartCount = 0x58771F;
    private const int PartPool = 0x587723;
    private const int PartCapacity = 0xC8000;
    private const int Points = 0x1207723;
    private const int Grid = 0x23FFEDC;
    private const int ChunkCells = 0x20000;
    private readonly byte[] Original;
    private readonly byte[] Raw;
    private readonly Dictionary<int, byte> Changed = [];
    private readonly Stack<Change[]> Undo = [];
    private readonly Stack<Change[]> Redo = [];
    private readonly record struct Change(int Offset, byte Before, byte After);

    public ReadOnlyMemory<byte> Data => Raw;
    public MapPart[] Parts { get; }
    public int[] Chunks { get; }
    public bool IsEdited => Changed.Count != 0;
    public bool CanUndo => Undo.Count != 0;
    public bool CanRedo => Redo.Count != 0;

    private StageSave(byte[] original, byte[] raw)
    {
        Original = original;
        Raw = raw;
        var count = ReadInt32LittleEndian(raw.AsSpan(PartCount));
        var chunks = ReadInt32LittleEndian(raw.AsSpan(ChunkCount));
        if (count is < 1 or > PartCapacity || chunks is < 1 or > 256 || Grid + (long)chunks * ChunkCells * 3 > raw.Length)
            throw new InvalidDataException("The map allocation is invalid.");
        Chunks = Enumerable.Range(0, 4096).Where(z => GetSlot(z) != 0xFFFF).ToArray();
        if (Chunks.Length != chunks || !Chunks.Select(GetSlot).Order().SequenceEqual(Enumerable.Range(0, chunks)))
            throw new InvalidDataException("The map chunks are invalid.");

        var parts = new List<MapPart>();
        for (int i = 0; i < count; i++)
        {
            var offset = PartPool + i * MapPart.SIZE;
            var state = ReadUInt32LittleEndian(raw.AsSpan(offset + 8));
            var handle = (int)((state >> 4) & 0xFFFFF);
            if (handle >= PartCapacity)
                throw new InvalidDataException("The map point reference is invalid.");
            var point = ReadUInt32LittleEndian(raw.AsSpan(Points + handle * 4));
            var part = new MapPart(i, raw.AsMemory(offset, MapPart.SIZE), raw.AsMemory(Points + handle * 4, 4));
            if ((point >> 12) != i)
                throw new InvalidDataException("The map part reference is invalid.");
            if (part.Variation == 0)
                continue;
            if (GetSlot(part.Chunk) == 0xFFFF)
                throw new InvalidDataException("A map part is outside the allocated map.");
            parts.Add(part);
        }
        Parts = parts.ToArray();
        // Saved links can be stale after pool compaction. The native loader rebuilds
        // them from point references; preserve them without following them here.
    }

    private int GetSlot(int chunk) => ReadUInt16LittleEndian(Raw.AsSpan(ChunkMap + chunk * 2));

    private int GetCellOffset(int x, int y, int z)
    {
        if ((uint)x >= 2048 || (uint)y >= 128 || (uint)z >= 2048)
            throw new ArgumentOutOfRangeException(nameof(x), "The position is outside the map.");
        var slot = GetSlot((z >> 5) * 64 + (x >> 5));
        if (slot == 0xFFFF)
            throw new ArgumentException("The position is in an unallocated map chunk.");
        var local = (y << 10) | ((z & 31) << 5) | (x & 31);
        return Grid + (slot * ChunkCells + local) * 3;
    }

    public uint GetCell(int x, int y, int z)
    {
        var offset = GetCellOffset(x, y, z);
        return (uint)(Raw[offset] | Raw[offset + 1] << 8 | Raw[offset + 2] << 16);
    }

    private Dictionary<int, uint> GetCells(MapBounds bounds)
    {
        if (bounds.Width is < 1 or > 32 || bounds.Depth is < 1 or > 32 || bounds.Height is < 1 or > 32)
            throw new ArgumentException("This object's footprint is not supported.");
        var result = new Dictionary<int, uint>();
        for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
        for (int z = bounds.Z; z < bounds.Z + bounds.Depth; z++)
        for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            result.Add(GetCellOffset(x, y, z), GetCell(x, y, z));
        return result;
    }

    public string? GetEditError(MapPart part, ushort variation, int x, int y, int z, int direction)
    {
        try { _ = PrepareEdit(part, variation, x, y, z, direction); return null; }
        catch (ArgumentException ex) { return ex.Message; }
    }

    private Dictionary<int, byte> PrepareEdit(MapPart part, ushort variation, int x, int y, int z, int direction)
    {
        if (!Parts.Contains(part))
            throw new ArgumentException("Select an object from this map.");
        var info = MapPartInfo.Get(variation);
        if (!part.Info.CanEdit || !info.CanEdit)
            throw new ArgumentException("This pass edits beds, sofas, cushions and toys. Other objects are view-only.");
        Span<byte> position = stackalloc byte[6];
        WriteUInt16LittleEndian(position, (ushort)part.X);
        WriteUInt16LittleEndian(position[2..], (ushort)part.Y);
        WriteUInt16LittleEndian(position[4..], (ushort)part.Z);
        // Keep objects with position-based state out of a placement-only edit.
        if (Raw.AsSpan(0, ChunkMap).IndexOf(position) >= 0)
            throw new ArgumentException("This object has attached saved state and is view-only in this pass.");
        var source = GetCells(part.Bounds);
        var bounds = info.GetBounds(x, y, z, direction);
        var target = GetCells(bounds);
        var cell = source.Values.First();
        if ((cell & 0x7FF) != 0x7FF || (cell & ~0x1F07FFu) != 0 ||
            ((cell >> 16) & 15) != (uint)(part.Handle >> 16) || source.Values.Any(z => z != cell))
            throw new ArgumentException("This object's occupied cells do not match its supported footprint.");
        foreach (var other in Parts)
        {
            if (other == part)
                continue;
            var otherBounds = other.Bounds;
            if (otherBounds.Width == 0 || otherBounds.Height == 0 || otherBounds.Depth == 0)
                otherBounds = new MapBounds(other.X, other.Y, other.Z, 1, 1, 1);
            if (otherBounds.Intersects(part.Bounds) || otherBounds.Intersects(bounds))
                throw new ArgumentException("The footprint overlaps another object. Move the other object first.");
        }
        if (target.Any(z => !source.ContainsKey(z.Key) && z.Value != 0))
            throw new ArgumentException("The destination is occupied. Choose empty cells for the whole footprint.");

        var writes = new Dictionary<int, byte>();
        foreach (var offset in source.Keys)
            AddValue(writes, offset, 0, 3);
        foreach (var offset in target.Keys)
            AddValue(writes, offset, cell, 3);
        var location = ReadUInt32LittleEndian(Raw.AsSpan(PartPool + part.Index * 16 + 4));
        location = (location & 0x1000) | variation | (uint)(x & 31) << 13 | (uint)y << 18 | (uint)(z & 31) << 25 | (uint)direction << 30;
        AddValue(writes, PartPool + part.Index * 16 + 4, location, 4);
        var chunk = (z >> 5) * 64 + (x >> 5);
        if (chunk != part.Chunk)
        {
            AddValue(writes, Points + part.Handle * 4, (uint)(part.Index << 12) | (uint)chunk, 4);
            // The game rebuilds these chains on load. Keep them valid in the export too.
            foreach (var group in Parts.Where(p => p.Chunk == part.Chunk || p.Chunk == chunk)
                         .GroupBy(p => p == part ? chunk : p.Chunk))
            {
                uint previous = uint.MaxValue;
                foreach (var entry in group.OrderBy(p => p.Index))
                {
                    AddValue(writes, PartPool + entry.Index * 16, previous, 4);
                    previous = (uint)entry.Index;
                }
            }
        }
        return writes;
    }

    private static void AddValue(Dictionary<int, byte> writes, int offset, uint value, int size)
    {
        for (int i = 0; i < size; i++)
            writes[offset + i] = (byte)(value >> (i * 8));
    }

    public void SetPart(MapPart part, ushort variation, int x, int y, int z, int direction)
    {
        if (part.Variation == variation && part.X == x && part.Y == y && part.Z == z && part.Direction == direction)
            return;
        var writes = PrepareEdit(part, variation, x, y, z, direction);
        var changes = writes.Where(z => Raw[z.Key] != z.Value).Select(z => new Change(z.Key, Raw[z.Key], z.Value)).ToArray();
        Apply(changes, true);
        Undo.Push(changes);
        Redo.Clear();
    }

    private void Apply(Change[] changes, bool forward)
    {
        foreach (var change in changes)
        {
            if (!Changed.ContainsKey(change.Offset))
                Changed.Add(change.Offset, Raw[change.Offset]);
            Raw[change.Offset] = forward ? change.After : change.Before;
            if (Raw[change.Offset] == Changed[change.Offset])
                Changed.Remove(change.Offset);
        }
    }

    public void UndoEdit()
    {
        if (!Undo.TryPop(out var changes))
            return;
        Apply(changes, false);
        Redo.Push(changes);
    }

    public void RedoEdit()
    {
        if (!Redo.TryPop(out var changes))
            return;
        Apply(changes, true);
        Undo.Push(changes);
    }

    public static StageSave Read(byte[] data)
    {
        if (data.Length < HeaderSize || ReadUInt32LittleEndian(data) != 0x31227687 || ReadUInt32LittleEndian(data.AsSpan(4)) != Version)
            throw new InvalidDataException("Map editing is not available for this stage version.");
        var length = ReadInt32LittleEndian(data.AsSpan(0x10));
        if (length <= HeaderSize || length > data.Length)
            throw new InvalidDataException("The compressed stage length is invalid.");
        using var input = new MemoryStream(data, HeaderSize, length - HeaderSize, false);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        var raw = new byte[DataSize];
        zlib.ReadExactly(raw);
        if (zlib.ReadByte() != -1)
            throw new InvalidDataException("The stage layout is not supported.");
        return new StageSave((byte[])data.Clone(), raw);
    }

    public byte[] Write()
    {
        if (!IsEdited)
            return (byte[])Original.Clone();
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, true))
            zlib.Write(Raw);
        var length = checked(HeaderSize + (int)compressed.Length);
        var result = new byte[Math.Max(length, Original.Length)];
        Original.AsSpan(0, HeaderSize).CopyTo(result);
        compressed.GetBuffer().AsSpan(0, (int)compressed.Length).CopyTo(result.AsSpan(HeaderSize));
        WriteInt32LittleEndian(result.AsSpan(0x10), length);
        return result;
    }
}
