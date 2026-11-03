using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PokoHex.Core;

namespace PokoHex.Web.Controls;

public partial class AbilityEditor
{
    [Parameter, EditorRequired] public SaveFile SAV { get; set; } = null!;
    [Parameter] public bool Reactions { get; set; }
    [Parameter] public int Language { get; set; }
    [Parameter] public bool Active { get; set; } = true;
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter, EditorRequired] public Action<string> Error { get; set; } = null!;
    private SaveFile? Loaded;
    private int PreviousLanguage = -1;
    private AbilityInfo[] Entries = [];
    private AbilityInfo[] Choices = [];
    private byte SelectedAbility;
    private int SelectedSlot;
    private byte WheelAbility;
    private PlayerAbilities? Abilities => SAV.Abilities;
    private int WheelCount => Reactions ? PlayerAbilities.ReactionWheelCount : PlayerAbilities.SkillWheelCount;
    private string WheelId => Reactions ? "reaction-wheel" : "skill-wheel";
    private int LearnedCount => Entries.Count(z => IsLearned(z.Value));
    public bool HasChanges => Abilities != null && WheelAbility != GetSlot(SelectedSlot);

    protected override bool ShouldRender() => Active;
    private Task NotifyChanged() => Changed.InvokeAsync();
    private byte GetSlot(int slot) => Reactions ? Abilities!.GetReactionSlot(slot) : Abilities!.GetSkillSlot(slot);
    private bool IsLearned(int index) => Reactions ? Abilities!.Reactions[index].Learned : Abilities!.Skills[index].Learned;
    private string GetName(byte index) => index == 0 ? "(Empty)" : Entries.FirstOrDefault(z => z.Value == index)?.Text ?? $"Saved {(Reactions ? "reaction" : "move")} {index}";

    protected override void OnParametersSet()
    {
        if (Loaded != SAV)
        {
            Loaded = SAV;
            SelectedAbility = 0;
            SelectedSlot = 0;
            WheelAbility = Abilities == null ? (byte)0 : GetSlot(0);
        }
        if (PreviousLanguage != Language || Entries.Length == 0)
        {
            Entries = (Reactions ? GameInfo.Strings.ReactionDataSource : GameInfo.Strings.SkillDataSource).OrderBy(z => z.Text).ToArray();
            PreviousLanguage = Language;
            if (SelectedAbility == 0)
                SelectedAbility = Entries.FirstOrDefault()?.Value ?? 0;
        }
        LoadChoices();
    }

    private void LoadChoices()
    {
        if (Abilities == null)
        {
            Choices = [];
            return;
        }
        var choices = Entries.Where(z => IsLearned(z.Value) && (Reactions || Skill.CanUseWheel(z.Value))).ToList();
        choices.Insert(0, new AbilityInfo(0, "(Empty)"));
        var current = GetSlot(SelectedSlot);
        if (choices.All(z => z.Value != current))
            choices.Add(new AbilityInfo(current, GetName(current)));
        Choices = choices.ToArray();
    }

    public void SaveChanges()
    {
        if (!HasChanges)
            return;
        if (Reactions)
            Abilities!.SetReactionSlot(SelectedSlot, WheelAbility);
        else
            Abilities!.SetSkillSlot(SelectedSlot, WheelAbility);
    }

    private void Learn(int index)
    {
        if (Reactions)
            Abilities!.LearnReaction(index);
        else
            Abilities!.LearnSkill(index);
    }

    private async Task LearnAbilities(bool all)
    {
        if (Abilities == null)
            return;
        try
        {
            SaveChanges();
            if (all)
            {
                foreach (var entry in Entries)
                    Learn(entry.Value);
            }
            else
                Learn(SelectedAbility);
            WheelAbility = GetSlot(SelectedSlot);
            LoadChoices();
            await NotifyChanged();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private void LB_Wheel_Changed(int slot)
    {
        try
        {
            SaveChanges();
            SelectedSlot = slot;
            WheelAbility = GetSlot(slot);
            LoadChoices();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }

    private async Task B_Set_Click()
    {
        try
        {
            SaveChanges();
            WheelAbility = GetSlot(SelectedSlot);
            LoadChoices();
            await NotifyChanged();
        }
        catch (ArgumentException ex) { Error(ex.Message); }
    }
}
