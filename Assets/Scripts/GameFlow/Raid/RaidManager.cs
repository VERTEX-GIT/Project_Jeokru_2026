using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RaidState
{
    Inactive,
    Active,
    Failed
}

[DisallowMultipleComponent]
public sealed class RaidManager : MonoBehaviour
{
    public static RaidManager Instance
    {
        get;
        private set;
    }

    [Header("References")]
    [SerializeField]
    private GameTimeManager gameTimeManager;

    [SerializeField]
    private EnemySpawnZone enemySpawnZone;

    [Header("Raid Times")]
    [SerializeField]
    private int[] raidTimes =
    {
        15,
        30,
        45,
        60
    };

    [Header("Enemy Types")]
    [SerializeField]
    private UnitData meleeEnemyData;

    [SerializeField]
    private UnitData rangedEnemyData;

    [Header("Spawn Count")]
    [SerializeField]
    [Min(0)]
    private int baseEnemiesPerSecond = 1;

    [SerializeField]
    [Min(0.01f)]
    private float spawnCountIncreaseCoefficient =
        5f;

    [Header("Spawn Duration")]
    [SerializeField]
    [Min(1)]
    private int baseSpawnDuration = 3;

    [SerializeField]
    [Min(0.01f)]
    private float spawnDurationIncreaseCoefficient =
        5f;

    [Header("Retreat")]
    [SerializeField]
    [Min(0f)]
    private float retreatDelay = 5f;

    private readonly List<UnitCore>
        spawnedEnemies = new();

    private int activeSpawnBatchCount;

    private bool hasEnemyExisted;

    private float retreatDelayRemaining;

    private bool retreatCountdownStarted;

    private bool isRetreating;

    public RaidState State
    {
        get;
        private set;
    } = RaidState.Inactive;

    public bool IsRaidActive =>
        State != RaidState.Inactive;

    public bool IsRaidFailed =>
        State == RaidState.Failed;

    public bool IsSpawning =>
        activeSpawnBatchCount > 0;

    public bool IsRetreating =>
        isRetreating;

    public int ActiveEnemyCount
    {
        get
        {
            CleanupEnemyList();

            return spawnedEnemies.Count;
        }
    }

    public event Action RaidStarted;
    public event Action RaidSucceeded;
    public event Action RaidFailed;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;

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

        if (enemySpawnZone == null)
        {
            enemySpawnZone =
                FindAnyObjectByType<
                    EnemySpawnZone>();
        }
    }

    private void OnEnable()
    {
        if (gameTimeManager != null)
        {
            gameTimeManager.TimeTicked +=
                HandleTimeTicked;
        }
    }

    private void OnDisable()
    {
        if (gameTimeManager != null)
        {
            gameTimeManager.TimeTicked -=
                HandleTimeTicked;
        }
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        if (State == RaidState.Inactive)
        {
            return;
        }

        CleanupEnemyList();

        EvaluateRaidState();

        if (State == RaidState.Failed)
        {
            UpdateFailedRaid();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleTimeTicked(
        int currentDay,
        int currentTime)
    {
        if (!IsRaidTime(
                currentTime))
        {
            return;
        }

        // 실패가 확정된 레이드는
        // 이후 예정된 새로운 증원을 받지 않는다.
        //
        // 실패 전에 이미 시작된 SpawnBatch는
        // 그대로 끝까지 진행한다.
        if (State == RaidState.Failed)
        {
            return;
        }

        BeginSpawnEvent(
            currentDay);
    }

    private bool IsRaidTime(
        int currentTime)
    {
        if (raidTimes == null)
        {
            return false;
        }

        foreach (int raidTime
                 in raidTimes)
        {
            if (raidTime ==
                currentTime)
            {
                return true;
            }
        }

        return false;
    }

    private void BeginSpawnEvent(
        int currentDay)
    {
        if (State == RaidState.Failed)
        {
            return;
        }

        if (enemySpawnZone == null)
        {
            Debug.LogError(
                "RaidManager: EnemySpawnZone이 없습니다.",
                this);

            return;
        }

        int enemiesPerSecond =
            CalculateEnemiesPerSecond(
                currentDay);

        int duration =
            CalculateSpawnDuration(
                currentDay);

        if (enemiesPerSecond <= 0 ||
            duration <= 0)
        {
            return;
        }

        if (State == RaidState.Inactive)
        {
            StartNewRaid();
        }

        StartCoroutine(
            SpawnBatchRoutine(
                enemiesPerSecond,
                duration));
    }

    private void StartNewRaid()
    {
        spawnedEnemies.Clear();

        activeSpawnBatchCount = 0;

        hasEnemyExisted = false;

        retreatDelayRemaining = 0f;

        retreatCountdownStarted = false;

        isRetreating = false;

        State =
            RaidState.Active;

        RaidStarted?.Invoke();

        Debug.Log(
            "RaidManager: 레이드 시작",
            this);
    }

    private IEnumerator SpawnBatchRoutine(
        int enemiesPerSecond,
        int duration)
    {
        activeSpawnBatchCount++;

        for (int second = 0;
             second < duration;
             second++)
        {
            while (gameTimeManager != null &&
                   !gameTimeManager.IsRunning)
            {
                yield return null;
            }

            int spawnedThisSecond = 0;

            while (spawnedThisSecond <
                   enemiesPerSecond)
            {
                while (gameTimeManager != null &&
                       !gameTimeManager.IsRunning)
                {
                    yield return null;
                }

                if (TrySpawnEnemy())
                {
                    spawnedThisSecond++;
                }
                else
                {
                    yield return null;
                }
            }

            if (second >=
                duration - 1)
            {
                continue;
            }

            float elapsedSecond = 0f;

            while (elapsedSecond < 1f)
            {
                if (gameTimeManager == null ||
                    gameTimeManager.IsRunning)
                {
                    elapsedSecond +=
                        Time.deltaTime;
                }

                yield return null;
            }
        }

        activeSpawnBatchCount--;

        if (activeSpawnBatchCount < 0)
        {
            activeSpawnBatchCount = 0;
        }
    }

    private bool TrySpawnEnemy()
    {
        UnitData selectedData =
            UnityEngine.Random.value < 0.5f
                ? meleeEnemyData
                : rangedEnemyData;

        if (selectedData == null)
        {
            selectedData =
                meleeEnemyData != null
                    ? meleeEnemyData
                    : rangedEnemyData;
        }

        if (selectedData == null)
        {
            Debug.LogError(
                "RaidManager: 적 UnitData가 연결되지 않았습니다.",
                this);

            return false;
        }

        if (!enemySpawnZone.TrySpawn(
                selectedData,
                out UnitCore enemy))
        {
            return false;
        }

        spawnedEnemies.Add(
            enemy);

        hasEnemyExisted = true;

        // 실패 이전에 이미 시작돼 있던
        // SpawnBatch에서 뒤늦게 생성된 적.
        //
        // 이 적도 실패 레이드에 합류해서
        // 공장만 공격한다.
        if (State == RaidState.Failed)
        {
            ForceEnemyToFactory(
                enemy);
        }

        return true;
    }

    private void EvaluateRaidState()
    {
        if (State == RaidState.Active)
        {
            if (!HasAvailableAlly())
            {
                FailRaid();

                return;
            }

            if (CanSucceedRaid())
            {
                SucceedRaid();
            }

            return;
        }

        if (State == RaidState.Failed &&
            isRetreating &&
            !IsSpawning &&
            spawnedEnemies.Count == 0)
        {
            CompleteFailedRaid();
        }
    }

    private bool CanSucceedRaid()
    {
        if (!hasEnemyExisted)
        {
            return false;
        }

        if (IsSpawning)
        {
            return false;
        }

        if (spawnedEnemies.Count > 0)
        {
            return false;
        }

        return State ==
            RaidState.Active;
    }

    private bool HasAvailableAlly()
    {
        UnitCore[] units =
            FindObjectsByType<UnitCore>(
                FindObjectsSortMode.None);

        foreach (UnitCore unit
                 in units)
        {
            if (unit == null ||
                unit.Data == null)
            {
                continue;
            }

            if (unit.Data.Team !=
                UnitTeam.Ally)
            {
                continue;
            }

            if (!unit.IsGameplayAvailable)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void SucceedRaid()
    {
        if (State != RaidState.Active)
        {
            return;
        }

        State =
            RaidState.Inactive;

        spawnedEnemies.Clear();

        activeSpawnBatchCount = 0;

        hasEnemyExisted = false;

        retreatDelayRemaining = 0f;

        retreatCountdownStarted = false;

        isRetreating = false;

        RaidSucceeded?.Invoke();

        Debug.Log(
            "RaidManager: 레이드 성공",
            this);
    }

    private void FailRaid()
    {
        if (State != RaidState.Active)
        {
            return;
        }

        State =
            RaidState.Failed;

        retreatDelayRemaining =
            retreatDelay;

        retreatCountdownStarted =
            false;

        isRetreating =
            false;

        ForceAllEnemiesToFactories();

        RaidFailed?.Invoke();

        Debug.Log(
            "RaidManager: 레이드 실패",
            this);
    }

    private void UpdateFailedRaid()
    {
        if (isRetreating)
        {
            return;
        }

        // 실패 전에 시작된 증원이
        // 아직 남아 있다면 전부 받을 때까지 기다린다.
        if (IsSpawning)
        {
            retreatCountdownStarted =
                false;

            retreatDelayRemaining =
                retreatDelay;

            return;
        }

        // 이미 진행 중이던 모든 증원이
        // 끝난 뒤부터 퇴각 대기시간 시작.
        if (!retreatCountdownStarted)
        {
            retreatCountdownStarted =
                true;

            retreatDelayRemaining =
                retreatDelay;

            Debug.Log(
                "RaidManager: 모든 기존 증원 종료, " +
                "퇴각 대기 시작",
                this);
        }

        if (gameTimeManager != null &&
            !gameTimeManager.IsRunning)
        {
            return;
        }

        retreatDelayRemaining -=
            Time.deltaTime;

        if (retreatDelayRemaining > 0f)
        {
            return;
        }

        BeginRetreat();
    }

    private void BeginRetreat()
    {
        if (isRetreating)
        {
            return;
        }

        isRetreating =
            true;

        CleanupEnemyList();

        foreach (UnitCore enemy
                 in spawnedEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            UnitRetreatMover retreatMover =
                enemy.GetComponent<
                    UnitRetreatMover>();

            if (retreatMover == null)
            {
                retreatMover =
                    enemy.gameObject
                        .AddComponent<
                            UnitRetreatMover>();
            }

            retreatMover.BeginRetreat(
                enemySpawnZone);
        }

        Debug.Log(
            "RaidManager: 적 퇴각 시작",
            this);
    }

    private void ForceAllEnemiesToFactories()
    {
        CleanupEnemyList();

        foreach (UnitCore enemy
                 in spawnedEnemies)
        {
            ForceEnemyToFactory(
                enemy);
        }
    }

    private void ForceEnemyToFactory(
        UnitCore enemy)
    {
        if (enemy == null ||
            enemy.Data == null ||
            enemy.Data.Team !=
                UnitTeam.Enemy)
        {
            return;
        }

        GameObject factory =
            FindNearestAliveFactory(
                enemy.transform.position);

        if (factory == null)
        {
            enemy.ClearTarget();

            return;
        }

        enemy.SetTarget(
            factory);

        UnitTargeting targeting =
            enemy.GetComponent<
                UnitTargeting>();

        if (targeting != null)
        {
            targeting.enabled =
                false;
        }

        enemy.SetAutoCombat(
            true);
    }

    private GameObject FindNearestAliveFactory(
        Vector3 position)
    {
        FactoryCore[] factories =
            FindObjectsByType<
                FactoryCore>(
                FindObjectsSortMode.None);

        FactoryCore nearestFactory =
            null;

        float nearestDistanceSqr =
            float.MaxValue;

        foreach (FactoryCore factory
                 in factories)
        {
            if (factory == null ||
                !factory.TryGetComponent(
                    out FactoryHealth health) ||
                !health.IsAlive)
            {
                continue;
            }

            float distanceSqr =
                (factory.transform.position -
                 position)
                .sqrMagnitude;

            if (distanceSqr >=
                nearestDistanceSqr)
            {
                continue;
            }

            nearestDistanceSqr =
                distanceSqr;

            nearestFactory =
                factory;
        }

        return nearestFactory != null
            ? nearestFactory.gameObject
            : null;
    }

    private void CompleteFailedRaid()
    {
        State =
            RaidState.Inactive;

        spawnedEnemies.Clear();

        activeSpawnBatchCount = 0;

        hasEnemyExisted = false;

        retreatDelayRemaining = 0f;

        retreatCountdownStarted = false;

        isRetreating = false;

        Debug.Log(
            "RaidManager: 실패 레이드 종료",
            this);
    }

    private int CalculateEnemiesPerSecond(
        int currentDay)
    {
        float value =
            baseEnemiesPerSecond +
            (float)currentDay /
            spawnCountIncreaseCoefficient;

        return Mathf.Max(
            0,
            Mathf.FloorToInt(
                value));
    }

    private int CalculateSpawnDuration(
        int currentDay)
    {
        float value =
            baseSpawnDuration +
            (float)currentDay /
            spawnDurationIncreaseCoefficient;

        return Mathf.Max(
            1,
            Mathf.FloorToInt(
                value));
    }

    private void CleanupEnemyList()
    {
        for (int i =
                spawnedEnemies.Count - 1;
             i >= 0;
             i--)
        {
            if (spawnedEnemies[i] != null)
            {
                continue;
            }

            spawnedEnemies.RemoveAt(i);
        }
    }

#if UNITY_EDITOR
    [ContextMenu(
        "Debug/Start Raid Spawn Event")]
    private void DebugStartRaid()
    {
        // Failed 상태에서는
        // 디버그 호출도 새 증원을 생성하지 않는다.
        if (State == RaidState.Failed)
        {
            return;
        }

        int day =
            gameTimeManager != null
                ? gameTimeManager.CurrentDay
                : 1;

        BeginSpawnEvent(
            day);
    }

    [ContextMenu(
        "Debug/Print Raid State")]
    private void DebugPrintRaidState()
    {
        Debug.Log(
            $"Raid State: {State}, " +
            $"Enemies: {ActiveEnemyCount}, " +
            $"Spawning: {IsSpawning}, " +
            $"Retreat Countdown: " +
            $"{retreatCountdownStarted}, " +
            $"Retreating: {isRetreating}, " +
            $"Retreat Delay: " +
            $"{retreatDelayRemaining:F2}",
            this);
    }
#endif
}