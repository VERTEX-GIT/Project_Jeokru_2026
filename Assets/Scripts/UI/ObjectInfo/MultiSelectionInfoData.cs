public readonly struct MultiSelectionInfoData
{
    public string Title { get; }

    public string AverageHp { get; }
    public string AverageStress { get; }

    public int MovingCount { get; }
    public int CombatCount { get; }
    public int WorkingCount { get; }
    public int IdleCount { get; }

    public int RangedCount { get; }
    public int MeleeCount { get; }

    public MultiSelectionInfoData(
        string title,
        string averageHp,
        string averageStress,
        int movingCount,
        int combatCount,
        int workingCount,
        int idleCount,
        int rangedCount,
        int meleeCount)
    {
        Title = title;

        AverageHp = averageHp;
        AverageStress = averageStress;

        MovingCount = movingCount;
        CombatCount = combatCount;
        WorkingCount = workingCount;
        IdleCount = idleCount;

        RangedCount = rangedCount;
        MeleeCount = meleeCount;
    }
}