public readonly struct HoverInfoData
{
    public string Name { get; }
    public string State { get; }

    public string Hp { get; }
    public string Stress { get; }

    public string AttackPower { get; }
    public string Defense { get; }
    public string AttackSpeed { get; }

    public string Description { get; }

    public HoverInfoData(
        string name,
        string state,
        string hp,
        string stress,
        string attackPower,
        string defense,
        string attackSpeed,
        string description)
    {
        Name = name;
        State = state;

        Hp = hp;
        Stress = stress;

        AttackPower = attackPower;
        Defense = defense;
        AttackSpeed = attackSpeed;

        Description = description;
    }
}