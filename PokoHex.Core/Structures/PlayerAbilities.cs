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
    public Skill[] Skills { get; }
    public Reaction[] Reactions { get; }

    internal PlayerAbilities(Memory<byte> raw)
    {
        Skills = Enumerable.Range(0, SkillCount).Select(i => new Skill(i, raw.Slice(i * Skill.SIZE, Skill.SIZE))).ToArray();
        SkillWheel = raw.Slice(SkillCount * Skill.SIZE, SkillWheelCount);
        var reactions = SkillCount * Skill.SIZE + SkillWheelCount;
        Reactions = Enumerable.Range(0, ReactionCount).Select(i => new Reaction(i, raw.Slice(reactions + i, 1))).ToArray();
        ReactionWheel = raw.Slice(reactions + ReactionCount, ReactionWheelCount * 2);
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
