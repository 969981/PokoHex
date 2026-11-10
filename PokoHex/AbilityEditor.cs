using System;
using System.Linq;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

public partial class AbilityEditor : UserControl
{
    private PlayerAbilities? SAV;
    private bool Loading;
    private int SelectedSlot = -1;
    public bool Reactions { get; set; }
    private AbilityInfo[] Entries => Reactions ? GameInfo.Strings.ReactionDataSource : GameInfo.Strings.SkillDataSource;
    private int WheelCount => Reactions ? PlayerAbilities.ReactionWheelCount : PlayerAbilities.SkillWheelCount;
    private byte GetSlot(int slot) => Reactions ? SAV!.GetReactionSlot(slot) : SAV!.GetSkillSlot(slot);
    private bool IsLearned(int index) => Reactions ? SAV!.Reactions[index].Learned : SAV!.IsSkillLearned(index);
    public bool HasChanges => SAV != null && SelectedSlot >= 0 && CB_WheelAbility.SelectedItem is AbilityInfo entry && entry.Value != GetSlot(SelectedSlot);

    public AbilityEditor() => InitializeComponent();

    public void LoadSave(SaveFile save)
    {
        SAV = save.Abilities;
        SelectedSlot = -1;
        GB_Wheel.Text = Reactions ? "Reaction wheel order" : "Skill wheel order";
        DGV_Abilities.Columns[1].HeaderText = Reactions ? "Reaction" : "Move";
        DGV_Abilities.Columns[3].Visible = !Reactions;
        DGV_Abilities.Enabled = GB_Wheel.Enabled = SAV != null;
        LoadAbilities();
        LoadWheel();
    }

    public void RefreshLanguage()
    {
        SaveChanges();
        LoadAbilities();
        LoadWheel();
    }

    private void LoadAbilities()
    {
        Loading = true;
        var selected = DGV_Abilities.CurrentRow?.Tag as AbilityInfo;
        DGV_Abilities.Rows.Clear();
        if (SAV != null)
        {
            foreach (var entry in Entries.OrderBy(z => z.Text))
            {
                var image = Reactions ? AbilitySprite.GetReactionSprite(entry.Value) : AbilitySprite.GetSkillSprite(entry.Value);
                var row = DGV_Abilities.Rows[DGV_Abilities.Rows.Add(image!, entry.Text, IsLearned(entry.Value), Reactions ? 0U : SAV.Skills[entry.Value].Uses)];
                row.Tag = entry;
                if (selected?.Value == entry.Value)
                    DGV_Abilities.CurrentCell = row.Cells[1];
            }
        }
        Loading = false;
        UpdateSelection();
    }

    public void RefreshValues()
    {
        if (SAV != null)
        {
            foreach (DataGridViewRow row in DGV_Abilities.Rows)
                row.Cells[2].Value = IsLearned(((AbilityInfo)row.Tag!).Value);
        }
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (Loading)
            return;
        B_Learn.Enabled = SAV != null && DGV_Abilities.CurrentRow?.Tag is AbilityInfo entry && !IsLearned(entry.Value);
        B_LearnAll.Enabled = SAV != null && Entries.Any(z => !IsLearned(z.Value));
        var selected = (DGV_Abilities.CurrentRow?.Tag as AbilityInfo)?.Value ?? 0;
        B_UnlockDive.Visible = !Reactions && SAV != null && selected == 10;
        B_UnlockDive.Enabled = SAV != null && SAV.IsSkillLearned(10) && !SAV.DiveUnlocked;
        B_UnlockDive.Text = SAV?.DiveUnlocked == true ? "Dive unlocked" : "Unlock Dive";
        var hasFoodBoost = !Reactions && SAV != null && PlayerAbilities.HasFoodBoost(selected);
        var foodUnlocked = SAV?.IsFoodBoostUnlocked(selected) == true;
        B_UnlockFood.Visible = hasFoodBoost;
        B_UnlockFood.Enabled = hasFoodBoost && SAV!.CanUnlockFoodBoost(selected) && !foodUnlocked;
        B_UnlockFood.Text = foodUnlocked ? "Food boost unlocked" : "Unlock food boost";
        B_UnlockAbsorption.Visible = !Reactions && SAV != null && DGV_Abilities.CurrentRow?.Tag is AbilityInfo { Value: 3 };
        B_UnlockAbsorption.Enabled = SAV != null && SAV.Skills[3].Learned && !SAV.LiquidAbsorptionUnlocked;
        B_UnlockAbsorption.Text = SAV?.LiquidAbsorptionUnlocked == true ? "Absorption unlocked" : "Unlock absorption";
        var noun = Reactions ? "reactions" : "moves";
        L_Count.Text = SAV == null ? "Skills and reactions aren’t available for this save version."
            : $"{Entries.Count(z => IsLearned(z.Value))} of {Entries.Length} {noun} learned";
    }

    private string GetName(byte index) => index == 0 ? "(Empty)" : Entries.FirstOrDefault(z => z.Value == index)?.Text ?? $"Saved {(Reactions ? "reaction" : "move")} {index}";

    private void LoadWheel()
    {
        Loading = true;
        var selected = Math.Max(0, SelectedSlot);
        var slots = SAV == null ? [] : Enumerable.Range(0, WheelCount)
            .Select(i => new AbilityInfo((byte)i, $"{i + 1}: {GetName(GetSlot(i))}")).ToArray();
        LB_Wheel.DataSource = slots;
        LB_Wheel.DisplayMember = nameof(AbilityInfo.Text);
        LB_Wheel.ValueMember = nameof(AbilityInfo.Value);
        LB_Wheel.SelectedIndex = slots.Length == 0 ? -1 : Math.Min(selected, slots.Length - 1);
        SelectedSlot = LB_Wheel.SelectedIndex;
        Loading = false;
        LoadSlot();
    }

    private void LoadSlot()
    {
        Loading = true;
        if (SAV == null || SelectedSlot < 0)
            CB_WheelAbility.DataSource = null;
        else
        {
            var id = GetSlot(SelectedSlot);
            var choices = Entries.Where(z => IsLearned(z.Value) && (Reactions || Skill.CanUseWheel(z.Value))).OrderBy(z => z.Text).ToList();
            choices.Insert(0, new AbilityInfo(0, "(Empty)"));
            if (choices.All(z => z.Value != id))
                choices.Add(new AbilityInfo(id, GetName(id)));
            CB_WheelAbility.DataSource = choices;
            CB_WheelAbility.DisplayMember = nameof(AbilityInfo.Text);
            CB_WheelAbility.ValueMember = nameof(AbilityInfo.Value);
            CB_WheelAbility.SelectedValue = id;
        }
        Loading = false;
        B_Set.Enabled = HasChanges;
    }

    public void SaveChanges()
    {
        if (Loading || !HasChanges || CB_WheelAbility.SelectedItem is not AbilityInfo entry)
            return;
        if (Reactions)
            SAV!.SetReactionSlot(SelectedSlot, entry.Value);
        else
            SAV!.SetSkillSlot(SelectedSlot, entry.Value);
    }

    private void Learn(int index)
    {
        if (Reactions)
            SAV!.LearnReaction(index);
        else
            SAV!.LearnSkill(index);
    }

    private void B_Learn_Click(object sender, EventArgs e)
    {
        if (SAV == null || DGV_Abilities.CurrentRow?.Tag is not AbilityInfo entry)
            return;
        SaveChanges();
        Learn(entry.Value);
        LoadAbilities();
        LoadWheel();
    }

    private void B_LearnAll_Click(object sender, EventArgs e)
    {
        if (SAV == null)
            return;
        SaveChanges();
        foreach (var entry in Entries)
            Learn(entry.Value);
        LoadAbilities();
        LoadWheel();
    }

    private void B_UnlockFood_Click(object sender, EventArgs e)
    {
        if (Reactions || SAV == null || DGV_Abilities.CurrentRow?.Tag is not AbilityInfo entry || !SAV.CanUnlockFoodBoost(entry.Value))
            return;
        SaveChanges();
        SAV.UnlockFoodBoost(entry.Value);
        LoadWheel();
        UpdateSelection();
    }

    private void B_UnlockDive_Click(object sender, EventArgs e)
    {
        if (Reactions || SAV == null || !SAV.IsSkillLearned(10) || DGV_Abilities.CurrentRow?.Tag is not AbilityInfo { Value: 10 })
            return;
        SaveChanges();
        SAV.UnlockDive();
        LoadWheel();
        UpdateSelection();
    }

    private void B_UnlockAbsorption_Click(object sender, EventArgs e)
    {
        if (Reactions || SAV == null || !SAV.Skills[3].Learned || DGV_Abilities.CurrentRow?.Tag is not AbilityInfo { Value: 3 })
            return;
        SaveChanges();
        SAV.UnlockLiquidAbsorption();
        LoadWheel();
        UpdateSelection();
    }

    private void LB_Wheel_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (Loading)
            return;
        SaveChanges();
        SelectedSlot = LB_Wheel.SelectedIndex;
        // Refresh both labels after a swap, keeping the newly selected slot.
        LoadWheel();
    }

    private void B_Set_Click(object sender, EventArgs e)
    {
        SaveChanges();
        LoadWheel();
    }

    private void CB_WheelAbility_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!Loading)
            B_Set.Enabled = HasChanges;
    }

    private void DGV_Abilities_CurrentCellChanged(object sender, EventArgs e) => UpdateSelection();
}
