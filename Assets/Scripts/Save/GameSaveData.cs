using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public int version = 1;
    public string savedAtUtc;
    public GameTimeSaveData gameTime = new();
    public List<ResourceAmountSaveData> resources = new();
    public List<FactorySaveData> factories = new();
    public List<UnitSaveData> units = new();
}

[Serializable]
public sealed class GameTimeSaveData
{
    public int day = 1;
    public int time = 1;
}

[Serializable]
public sealed class ResourceAmountSaveData
{
    public ResourceType resourceType;
    public int amount;
}

[Serializable]
public sealed class CellSaveData
{
    public int x;
    public int y;
    public int z;

    public CellSaveData()
    {
    }

    public CellSaveData(
        int x,
        int y,
        int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[Serializable]
public sealed class FactorySaveData
{
    public FactoryType factoryType;
    public CellSaveData anchorCell = new();

    public float currentHp;
    public bool isDestroyed;

    public float productionProgressRate;
    public bool isProductionCycleActive;
}

[Serializable]
public sealed class UnitSaveData
{
    public string unitId;
    public CellSaveData cell = new();

    public bool isActive;
    public float currentHp;
    public float currentStress;

    public float attackPower;
    public float defense;
    public float attackCooldown;

    public bool hasFactoryAssignment;
    public CellSaveData assignedFactoryCell = new();

    public bool isCounseling;
    public bool hasCounselingReturnCell;
    public CellSaveData counselingReturnCell = new();
    public float counselingRecoveryTimer;
}
