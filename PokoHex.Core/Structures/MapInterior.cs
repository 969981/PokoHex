namespace PokoHex.Core;

public sealed record MapInterior(int Id, byte Type, int Chunk, int Floor, ushort ItemId, bool UpperFloor, bool IsGate)
{
    public int Ceiling => Floor + 10;
    public bool Contains(MapPart part) => part.Chunk == Chunk && part.Y >= Floor && part.Y < Floor + 12;

    public string Name
    {
        get
        {
            if (IsGate)
                return $"Area gate {Id}";
            var names = GameInfo.Strings.itemlist;
            var name = ItemId != 0 && ItemId < names.Length && names[ItemId].Length != 0 ? names[ItemId] : $"Interior {Id}";
            return UpperFloor ? $"{name} (2F)" : name;
        }
    }

    public override string ToString() => $"{Name} (Y {Floor + 1}–{Ceiling - 1})";
}
