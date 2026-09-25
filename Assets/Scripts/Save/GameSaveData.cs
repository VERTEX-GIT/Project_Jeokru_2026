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

    public float productionProgress;
    public bool isProductionCycleActive;
}

[Serializable]
public sealed class UnitSaveData
{
    // UnitData의 표시 이름과 분리된 저장 전용 식별자.
    // 실제 UnitData 쪽 SaveId는 유닛 복원 단계에서 추가한다.
    public string unitId;

    // 체크포인트에서 유닛을 복원할 논리 타일.
    // 이동 경로 자체는 저장하지 않는다.
    public CellSaveData cell = new();

    public float currentHp;
    public float currentStress;

    // ScriptableObject 기본값이 아닌 런타임 능력치도 보존한다.
    public float attackPower;
    public float defense;
    public float attackCooldown;

    // 일차가 넘어가도 유지할 수 있는 작업 상태.
    public bool hasFactoryAssignment;
    public CellSaveData assignedFactoryCell = new();

    // 상담 중인 상태도 일차 체크포인트에서 보존할 수 있게 한다.
    public bool isCounseling;
    public bool hasCounselingReturnCell;
    public CellSaveData counselingReturnCell = new();
    public float counselingRecoveryTimer;
}
