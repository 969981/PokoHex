using System;
using System.Linq;

namespace PokoHex.Core;

public sealed class PlayerAbilities
{
    public const int SkillCount = 64;
    public const int ReactionCount = 128;
    public const int SkillWheelCount = 8;
    public const int ReactionWheelCount = 24;
    public const int SIZE = SkillCount * Skill.SIZE + SkillWheelCount + ReactionCount + ReactionWheelCount * 2;
    private readonly Memory<byte> SkillWheel;
    private readonly Memory<byte> ReactionWheel;
    private readonly Memory<byte> FunctionFlags;
    public Skill[] Skills { get; }
    public Reaction[] Reactions { get; }

    internal PlayerAbilities(Memory<byte> raw, Memory<byte> functionFlags)
    {
        FunctionFlags = functionFlags;
        Skills = Enumerable.Range(0, SkillCount).Select(i => new Skill(i, raw.Slice(i * Skill.SIZE, Skill.SIZE))).ToArray();
        SkillWheel = raw.Slice(SkillCount * Skill.SIZE, SkillWheelCount);
        var reactions = SkillCount * Skill.SIZE + SkillWheelCount;
        Reactions = Enumerable.Range(0, ReactionCount).Select(i => new Reaction(i, raw.Slice(reactions + i, 1))).ToArray();
        ReactionWheel = raw.Slice(reactions + ReactionCount, ReactionWheelCount * 2);
    }

    public bool RockSmashFoodUnlocked => (FunctionFlags.Span[9] & 4) != 0 && (FunctionFlags.Span[15] & 32) != 0;

    public void UnlockRockSmashFood()
    {
        if (!Skills[4].Learned)
            throw new ArgumentException("Learn Rock Smash first.");
        // Food availability (74) and the extra effect (125) are separate flags.
        FunctionFlags.Span[9] |= 4;
        FunctionFlags.Span[15] |= 32;
    }

    public bool IsFoodBoostUnlocked(int index) => index switch
    {
        1 => (FunctionFlags.Span[8] & 128) != 0,
        3 => (FunctionFlags.Span[9] & 2) != 0,
        4 => RockSmashFoodUnlocked,
        5 => (FunctionFlags.Span[9] & 1) != 0,
        _ => false,
    };

    public void UnlockFoodBoost(int index)
    {
        if (index is not (1 or 3 or 4 or 5))
            throw new ArgumentOutOfRangeException(nameof(index), "Choose a move with a food boost.");
        if (!Skills[index].Learned)
            throw new ArgumentException("Learn the move first.");
        switch (index)
        {
            case 1: FunctionFlags.Span[8] |= 128; break;
            case 3: FunctionFlags.Span[9] |= 2; break;
            case 4: UnlockRockSmashFood(); break;
            case 5: FunctionFlags.Span[9] |= 1; break;
        }
    }

    public bool LiquidAbsorptionUnlocked => (FunctionFlags.Span[6] & 64) != 0;

    public void UnlockLiquidAbsorption()
    {
        if (!Skills[3].Learned)
            throw new ArgumentException("Learn Water Gun first.");
        FunctionFlags.Span[6] |= 64;
    }

    public void LearnSkill(int index)
    {
        if (!Skill.IsPublic(index))
            throw new ArgumentOutOfRangeException(nameof(index), "Choose a move from the list.");
        if (Skills[index].Learned)
            return;
        Skills[index].Learn();
        if (Skill.CanUseWheel(index) && !SkillWheel.Span.Contains((byte)index))
        {
            var empty = SkillWheel.Span.IndexOf((byte)0);
            if (empty >= 0)
                SetSkillSlot(empty, (byte)index);
        }
    }

    public void LearnReaction(int index)
    {
        if (!Reaction.IsPublic(index))
            throw new ArgumentOutOfRangeException(nameof(index), "Choose a reaction from the list.");
        if (Reactions[index].Learned)
            return;
        Reactions[index].Learn();
        var empty = -1;
        for (int i = 0; i < ReactionWheelCount; i++)
        {
            var id = GetReactionSlot(i);
            if (id == index)
                return;
            if (id == 0 && empty < 0)
                empty = i;
        }
        if (empty >= 0)
            SetReactionSlot(empty, (byte)index);
    }

    public byte GetSkillSlot(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, SkillWheelCount);
        return SkillWheel.Span[slot];
    }

    public byte GetReactionSlot(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, ReactionWheelCount);
        return ReactionWheel.Span[slot * 2];
    }

    public void SetSkillSlot(int slot, byte index)
    {
        var previous = GetSkillSlot(slot);
        if (previous == index)
            return;
        if (index != 0 && (!Skill.CanUseWheel(index) || !Skills[index].Learned))
            throw new ArgumentException("Choose a learned wheel move or leave the slot empty.");
        var other = index == 0 ? -1 : SkillWheel.Span.IndexOf(index);
        if (other >= 0)
            SkillWheel.Span[other] = previous;
        SkillWheel.Span[slot] = index;
    }

    public void SetReactionSlot(int slot, byte index)
    {
        var previous = GetReactionSlot(slot);
        if (previous == index)
            return;
        if (index != 0 && (!Reaction.IsPublic(index) || !Reactions[index].Learned))
            throw new ArgumentException("Choose a learned reaction or leave the slot empty.");
        var data = ReactionWheel.Span;
        if (index != 0)
        {
            for (int i = 0; i < ReactionWheelCount; i++)
            {
                if (i == slot || GetReactionSlot(i) != index)
                    continue;
                // Move both saved bytes with the reaction when swapping slots.
                (data[slot * 2], data[i * 2]) = (data[i * 2], data[slot * 2]);
                (data[slot * 2 + 1], data[i * 2 + 1]) = (data[i * 2 + 1], data[slot * 2 + 1]);
                return;
            }
        }
        data[slot * 2] = index;
        data[slot * 2 + 1] = 0;
    }
}
