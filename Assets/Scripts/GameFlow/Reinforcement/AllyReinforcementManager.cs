using UnityEngine;

[DisallowMultipleComponent]
public sealed class AllyReinforcementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameTimeManager gameTimeManager;

    [SerializeField]
    private AllySpawnZone spawnZone;

    [SerializeField]
    private UnitData allyUnitData;

    [Header("Daily Reinforcement")]
    [SerializeField]
    [Min(1)]
    private int targetActiveAllies = 3;

    [SerializeField]
    [Min(0)]
    private int dailyReinforcementLimit = 1;

    [SerializeField]
    [Min(1)]
    private int zeroAllyReinforcementLimit = 2;

    [SerializeField]
    [Min(1)]
    private int maxTotalAllies = 6;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (gameTimeManager != null)
        {
            gameTimeManager.DayChanged +=
                HandleDayChanged;
        }
    }

    private void OnDisable()
    {
        if (gameTimeManager != null)
        {
            gameTimeManager.DayChanged -=
                HandleDayChanged;
        }
    }

    private void ResolveReferences()
    {
        if (gameTimeManager == null)
        {
            gameTimeManager =
                GameTimeManager.Instance;
        }

        if (gameTimeManager == null)
        {
            gameTimeManager =
                FindAnyObjectByType<
                    GameTimeManager>();
        }

        if (spawnZone == null)
        {
            spawnZone =
                FindAnyObjectByType<
                    AllySpawnZone>();
        }
    }

    private void HandleDayChanged(
        int day)
    {
        TryProvideDailyReinforcements(
            day);
    }

    private void TryProvideDailyReinforcements(
        int day)
    {
        if (spawnZone == null ||
            allyUnitData == null)
        {
            Debug.LogError(
                "AllyReinforcementManager: " +
                "Spawn Zone 또는 Ally Unit Data가 연결되지 않았습니다.",
                this);

            return;
        }

        CountAllies(
            out int activeCount,
            out int totalCount);

        if (activeCount >=
            targetActiveAllies)
        {
            return;
        }

        int missingActive =
            targetActiveAllies -
            activeCount;

        int spawnCount;

        if (activeCount == 0)
        {
            // 전멸 상태에서는 전체 인원 상한보다 복구 가능성을 우선한다.
            // 다음 날 최소 한 명 이상이 합류해야 게임이 소프트락되지 않는다.
            spawnCount =
                Mathf.Min(
                    missingActive,
                    Mathf.Max(
                        1,
                        zeroAllyReinforcementLimit));
        }
        else
        {
            if (totalCount >=
                maxTotalAllies)
            {
                return;
            }

            int remainingCapacity =
                maxTotalAllies -
                totalCount;

            spawnCount =
                Mathf.Min(
                    missingActive,
                    dailyReinforcementLimit,
                    remainingCapacity);
        }

        int spawnedCount = 0;

        for (int i = 0;
             i < spawnCount;
             i++)
        {
            if (!spawnZone.TrySpawn(
                    allyUnitData,
                    out _))
            {
                break;
            }

            spawnedCount++;
        }

        if (spawnedCount > 0)
        {
            Debug.Log(
                $"AllyReinforcementManager: " +
                $"{day}일차 지원 인원 {spawnedCount}명 도착.",
                this);
        }
    }

    private void CountAllies(
        out int activeCount,
        out int totalCount)
    {
        activeCount = 0;
        totalCount = 0;

        UnitCore[] units =
            FindObjectsByType<UnitCore>(
                FindObjectsSortMode.None);

        foreach (UnitCore unit
                 in units)
        {
            if (unit == null ||
                unit.Data == null ||
                unit.Data.Team !=
                    UnitTeam.Ally ||
                !unit.Data.IsBasicUnit)
            {
                continue;
            }

            totalCount++;

            if (unit.IsActive)
            {
                activeCount++;
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Provide Reinforcements")]
    private void DebugProvideReinforcements()
    {
        ResolveReferences();

        int day =
            gameTimeManager != null
                ? gameTimeManager.CurrentDay
                : 1;

        TryProvideDailyReinforcements(
            day);
    }

    [ContextMenu("Debug/Print Ally Count")]
    private void DebugPrintAllyCount()
    {
        CountAllies(
            out int activeCount,
            out int totalCount);

        Debug.Log(
            $"AllyReinforcementManager: " +
            $"활동 {activeCount}명 / 전체 {totalCount}명",
            this);
    }
#endif
}
