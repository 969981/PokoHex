using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PokoHex;

partial class AbilityEditor
{
    private IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        var split = new SplitContainer();
        var buttons = new FlowLayoutPanel();
        DGV_Abilities = new DataGridView();
        B_Learn = new Button();
        B_LearnAll = new Button();
        B_UnlockFood = new Button();
        B_UnlockAbsorption = new Button();
        L_Count = new Label();
        GB_Wheel = new GroupBox();
        LB_Wheel = new ListBox();
        CB_WheelAbility = new ComboBox();
        B_Set = new Button();
        SuspendLayout();
        split.Dock = DockStyle.Fill;
        split.Size = new Size(920, 550);
        split.SplitterDistance = 550;
        split.Panel1MinSize = 260;
        split.Panel2MinSize = 320;
        split.FixedPanel = FixedPanel.Panel2;
        DGV_Abilities.Name = "DGV_Abilities";
        DGV_Abilities.Dock = DockStyle.Fill;
        DGV_Abilities.AllowUserToAddRows = false;
        DGV_Abilities.AllowUserToDeleteRows = false;
        DGV_Abilities.AllowUserToResizeRows = false;
        DGV_Abilities.ReadOnly = true;
        DGV_Abilities.MultiSelect = false;
        DGV_Abilities.RowHeadersVisible = false;
        DGV_Abilities.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        DGV_Abilities.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        DGV_Abilities.BackgroundColor = SystemColors.Window;
        DGV_Abilities.RowTemplate.Height = 40;
        DGV_Abilities.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 44, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ImageLayout = DataGridViewImageCellLayout.Zoom, DefaultCellStyle = new DataGridViewCellStyle { NullValue = null, Padding = new Padding(3) } });
        DGV_Abilities.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Move", FillWeight = 70, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Abilities.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Learned", FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Abilities.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Uses", FillWeight = 15, SortMode = DataGridViewColumnSortMode.NotSortable });
        DGV_Abilities.CurrentCellChanged += DGV_Abilities_CurrentCellChanged;
        buttons.Dock = DockStyle.Bottom;
        buttons.Height = 42;
        buttons.AutoSize = true;
        buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttons.Padding = new Padding(0, 6, 0, 0);
        B_Learn.Name = "B_Learn";
        B_Learn.Text = "Learn";
        B_Learn.Size = new Size(100, 30);
        B_Learn.Click += B_Learn_Click;
        B_LearnAll.Name = "B_LearnAll";
        B_LearnAll.Text = "Learn all";
        B_LearnAll.Size = new Size(100, 30);
        B_LearnAll.Click += B_LearnAll_Click;
        B_UnlockFood.Name = "B_UnlockFood";
        B_UnlockFood.Text = "Unlock food boost";
        B_UnlockFood.Size = new Size(160, 30);
        B_UnlockFood.Visible = false;
        B_UnlockFood.Click += B_UnlockFood_Click;
        B_UnlockAbsorption.Name = "B_UnlockAbsorption";
        B_UnlockAbsorption.Text = "Unlock absorption";
        B_UnlockAbsorption.Size = new Size(160, 30);
        B_UnlockAbsorption.Visible = false;
        B_UnlockAbsorption.Click += B_UnlockAbsorption_Click;
        buttons.Controls.AddRange([B_Learn, B_LearnAll, B_UnlockFood, B_UnlockAbsorption]);
        L_Count.Dock = DockStyle.Bottom;
        L_Count.Height = 28;
        L_Count.TextAlign = ContentAlignment.MiddleLeft;
        split.Panel1.Controls.Add(DGV_Abilities);
        split.Panel1.Controls.Add(buttons);
        split.Panel1.Controls.Add(L_Count);
        GB_Wheel.Name = "GB_Wheel";
        GB_Wheel.Dock = DockStyle.Fill;
        GB_Wheel.Size = new Size(366, 550);
        GB_Wheel.Text = "Skill wheel order";
        GB_Wheel.Padding = new Padding(12, 18, 12, 12);
        LB_Wheel.Name = "LB_Wheel";
        LB_Wheel.Location = new Point(16, 26);
        LB_Wheel.Size = new Size(334, 400);
        LB_Wheel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        LB_Wheel.IntegralHeight = false;
        LB_Wheel.SelectedIndexChanged += LB_Wheel_SelectedIndexChanged;
        CB_WheelAbility.Name = "CB_WheelAbility";
        CB_WheelAbility.Location = new Point(16, 440);
        CB_WheelAbility.Size = new Size(334, 23);
        CB_WheelAbility.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        CB_WheelAbility.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_WheelAbility.AutoCompleteSource = AutoCompleteSource.ListItems;
        CB_WheelAbility.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        CB_WheelAbility.DropDownWidth = 400;
        CB_WheelAbility.SelectedIndexChanged += CB_WheelAbility_SelectedIndexChanged;
        B_Set.Name = "B_Set";
        B_Set.Text = "Set";
        B_Set.Location = new Point(16, 480);
        B_Set.Size = new Size(100, 30);
        B_Set.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        B_Set.Click += B_Set_Click;
        GB_Wheel.Controls.AddRange([LB_Wheel, CB_WheelAbility, B_Set]);
        split.Panel2.Controls.Add(GB_Wheel);
        Controls.Add(split);
        Padding = new Padding(10);
        Size = new Size(920, 600);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ResumeLayout(false);
    }

    private DataGridView DGV_Abilities = null!;
    private Button B_Learn = null!;
    private Button B_LearnAll = null!;
    private Button B_UnlockFood = null!;
    private Button B_UnlockAbsorption = null!;
    private Label L_Count = null!;
    private GroupBox GB_Wheel = null!;
    private ListBox LB_Wheel = null!;
    private ComboBox CB_WheelAbility = null!;
    private Button B_Set = null!;
}
