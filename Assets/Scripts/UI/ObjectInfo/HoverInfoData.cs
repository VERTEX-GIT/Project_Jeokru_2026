public readonly struct HoverInfoData
{
    public string Name { get; }
    public string State { get; }

    public string HpLabel { get; }
    public string Hp { get; }

    public string StressLabel { get; }
    public string Stress { get; }

    public string AttackPowerLabel { get; }
    public string AttackPower { get; }

    public string DefenseLabel { get; }
    public string Defense { get; }

    public string AttackSpeedLabel { get; }
    public string AttackSpeed { get; }

    public string Description { get; }

    public HoverInfoData(
        string name,
        string state,
        string hpLabel,
        string hp,
        string stressLabel,
        string stress,
        string attackPowerLabel,
        string attackPower,
        string defenseLabel,
        string defense,
        string attackSpeedLabel,
        string attackSpeed,
        string description)
    {
        Name = name;
        State = state;

        HpLabel = hpLabel;
        Hp = hp;

        StressLabel = stressLabel;
        Stress = stress;

        AttackPowerLabel = attackPowerLabel;
        AttackPower = attackPower;

        DefenseLabel = defenseLabel;
        Defense = defense;

        AttackSpeedLabel = attackSpeedLabel;
        AttackSpeed = attackSpeed;

        Description = description;
    }
}