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

public enum RaidEventType
{
    Small,
    Medium
}

[DisallowMultipleComponent]
public sealed class RaidManager : MonoBehaviour
{
    public static RaidManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private GameTimeManager gameTimeManager;

    [SerializeField]
    private EnemySpawnZone enemySpawnZone;

    [Header("Raid Times")]
    [SerializeField]
    [Range(1, 59)]
    private int smallRaidTime = 30;

    [SerializeField]
    [Range(2, 60)]
    private int mediumRaidTime = 60;

    [Header("Enemy Types")]
    [SerializeField]
    private UnitData meleeEnemyData;

    [SerializeField]
    private UnitData rangedEnemyData;

    [Header("Enemy Count")]
    [SerializeField]
    [Min(1)]
    private int baseSmallRaidEnemyCount = 2;

    [SerializeField]
    [Min(1)]
    private int baseMediumRaidEnemyCount = 3;

    [SerializeField]
    [Min(1)]
    private int smallRaidIncreaseIntervalDays = 5;

    [SerializeField]
    [Min(1)]
    private int mediumRaidIncreaseIntervalDays = 3;

    [SerializeField]
    [Min(1)]
    private int enemyCountIncreaseAmount = 1;

    [Header("Spawn")]
    [SerializeField]
    [Min(0.05f)]
    private float spawnInterval = 0.75f;

    [Header("Retreat")]
    [SerializeField]
    [Min(0f)]
    private float retreatDelay = 5f;

    private readonly List<UnitCore> spawnedEnemies = new();
    private int activeSpawnBatchCount;
    private bool hasEnemyExisted;
    private float retreatDelayRemaining;
    private bool retreatCountdownStarted;
    private bool isRetreating;

    public RaidState State { get; private set; } = RaidState.Inactive;
    public bool IsRaidActive => State != RaidState.Inactive;
    public bool IsRaidFailed => State == RaidState.Failed;
    public bool IsSpawning => activeSpawnBatchCount > 0;
    public bool IsRetreating => isRetreating;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameTimeManager == null)
            gameTimeManager = GameTimeManager.Instance;

        if (gameTimeManager == null)
            gameTimeManager = FindAnyObjectByType<GameTimeManager>();

        if (enemySpawnZone == null)
            enemySpawnZone = FindAnyObjectByType<EnemySpawnZone>();
    }

    private void OnEnable()
    {
        if (gameTimeManager != null)
            gameTimeManager.TimeTicked += HandleTimeTicked;
    }

    private void OnDisable()
    {
        if (gameTimeManager != null)
            gameTimeManager.TimeTicked -= HandleTimeTicked;
    }

    private void Update()
    {
        if (PauseMenu.IsPaused || State == RaidState.Inactive)
            return;

        CleanupEnemyList();
        EvaluateRaidState();

        if (State == RaidState.Failed)
            UpdateFailedRaid();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandleTimeTicked(int currentDay, int currentTime)
    {
        if (State == RaidState.Failed)
            return;

        if (currentTime == smallRaidTime)
        {
            BeginSpawnEvent(currentDay, RaidEventType.Small);
            return;
        }

        if (currentTime == mediumRaidTime)
            BeginSpawnEvent(currentDay, RaidEventType.Medium);
    }

    private void BeginSpawnEvent(int currentDay, RaidEventType raidEventType)
    {
        if (State == RaidState.Failed)
            return;

        if (enemySpawnZone == null)
        {
            Debug.LogError("RaidManager: EnemySpawnZone이 없습니다.", this);
            return;
        }

        int enemyCount = CalculateEnemyCount(currentDay, raidEventType);
        if (enemyCount <= 0)
            return;

        if (State == RaidState.Inactive)
            StartNewRaid();

        StartCoroutine(SpawnBatchRoutine(enemyCount));

        Debug.Log(
            $"RaidManager: {raidEventType} 레이드 증원 시작, 적 {enemyCount}명",
            this);
    }

    private void StartNewRaid()
    {
        spawnedEnemies.Clear();
        activeSpawnBatchCount = 0;
        hasEnemyExisted = false;
        retreatDelayRemaining = 0f;
        retreatCountdownStarted = false;
        isRetreating = false;

        State = RaidState.Active;
        RaidStarted?.Invoke();

        Debug.Log("RaidManager: 레이드 시작", this);
    }

    private IEnumerator SpawnBatchRoutine(int enemyCount)
    {
        activeSpawnBatchCount++;

        for (int spawnedCount = 0; spawnedCount < enemyCount;)
        {
            while (gameTimeManager != null && !gameTimeManager.IsRunning)
                yield return null;

            if (!TrySpawnEnemy())
            {
                yield return null;
                continue;
            }

            spawnedCount++;

            if (spawnedCount >= enemyCount)
                continue;

            float elapsedInterval = 0f;

            while (elapsedInterval < spawnInterval)
            {
                if (gameTimeManager == null || gameTimeManager.IsRunning)
                    elapsedInterval += Time.deltaTime;

                yield return null;
            }
        }

        activeSpawnBatchCount = Mathf.Max(0, activeSpawnBatchCount - 1);
    }

    private bool TrySpawnEnemy()
    {
        UnitData selectedData =
            UnityEngine.Random.value < 0.5f
                ? meleeEnemyData
                : rangedEnemyData;

        if (selectedData == null)
            selectedData = meleeEnemyData != null ? meleeEnemyData : rangedEnemyData;

        if (selectedData == null)
        {
            Debug.LogError("RaidManager: 적 UnitData가 연결되지 않았습니다.", this);
            return false;
        }

        if (!enemySpawnZone.TrySpawn(selectedData, out UnitCore enemy))
            return false;

        spawnedEnemies.Add(enemy);
        hasEnemyExisted = true;

        if (State == RaidState.Failed)
            ForceEnemyToFactory(enemy);

        return true;
    }

    private void EvaluateRaidState()
    {
        if (State == RaidState.Active)
        {
            if (!HasLivingActiveAlly())
            {
                FailRaid();
                return;
            }

            if (CanSucceedRaid())
                SucceedRaid();

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
        return hasEnemyExisted &&
               !IsSpawning &&
               spawnedEnemies.Count == 0 &&
               State == RaidState.Active;
    }

    private bool HasLivingActiveAlly()
    {
        UnitCore[] units =
            FindObjectsByType<UnitCore>(FindObjectsSortMode.None);

        foreach (UnitCore unit in units)
        {
            if (unit == null ||
                unit.Data == null ||
                unit.Data.Team != UnitTeam.Ally ||
                !unit.IsActive)
            {
                continue;
            }

            if (!unit.TryGetComponent(out UnitHealth health) ||
                !health.IsAlive)
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
            return;

        ResetRaidState();

        gameTimeManager?.NotifyRaidSucceeded();

        if (gameTimeManager != null &&
            !gameTimeManager.IsGameOver)
        {
            gameTimeManager.NotifyRaidResolved();
        }

        RaidSucceeded?.Invoke();

        Debug.Log("RaidManager: 레이드 성공", this);
    }

    private void FailRaid()
    {
        if (State != RaidState.Active)
            return;

        State = RaidState.Failed;
        retreatDelayRemaining = retreatDelay;
        retreatCountdownStarted = false;
        isRetreating = false;

        ForceAllEnemiesToFactories();

        RaidFailed?.Invoke();

        Debug.Log("RaidManager: 레이드 실패", this);
    }

    private void UpdateFailedRaid()
    {
        if (isRetreating)
            return;

        if (IsSpawning)
        {
            retreatCountdownStarted = false;
            retreatDelayRemaining = retreatDelay;
            return;
        }

        if (!retreatCountdownStarted)
        {
            retreatCountdownStarted = true;
            retreatDelayRemaining = retreatDelay;

            Debug.Log(
                "RaidManager: 모든 기존 증원 종료, 퇴각 대기 시작",
                this);
        }

        if (gameTimeManager != null && !gameTimeManager.IsRunning)
            return;

        retreatDelayRemaining -= Time.deltaTime;

        if (retreatDelayRemaining <= 0f)
            BeginRetreat();
    }

    private void BeginRetreat()
    {
        if (isRetreating)
            return;

        isRetreating = true;
        CleanupEnemyList();

        foreach (UnitCore enemy in spawnedEnemies)
        {
            if (enemy == null)
                continue;

            UnitRetreatMover retreatMover =
                enemy.GetComponent<UnitRetreatMover>();

            if (retreatMover == null)
                retreatMover = enemy.gameObject.AddComponent<UnitRetreatMover>();

            retreatMover.BeginRetreat(enemySpawnZone);
        }

        Debug.Log("RaidManager: 적 퇴각 시작", this);
    }

    private void ForceAllEnemiesToFactories()
    {
        CleanupEnemyList();

        foreach (UnitCore enemy in spawnedEnemies)
            ForceEnemyToFactory(enemy);
    }

    private void ForceEnemyToFactory(UnitCore enemy)
    {
        if (enemy == null ||
            enemy.Data == null ||
            enemy.Data.Team != UnitTeam.Enemy)
        {
            return;
        }

        enemy.SetAutoCombat(true);

        UnitTargeting targeting =
            enemy.GetComponent<UnitTargeting>();

        if (targeting != null)
        {
            targeting.enabled = true;
            targeting.SetTargetingMode(UnitTargetingMode.FactoryOnly);
            targeting.TryAcquireTarget();
            return;
        }

        GameObject factory =
            FindNearestAliveFactory(enemy.transform.position);

        if (factory == null)
        {
            enemy.ClearTarget();
            return;
        }

        enemy.SetTarget(factory);
    }

    private GameObject FindNearestAliveFactory(Vector3 position)
    {
        FactoryCore[] factories =
            FindObjectsByType<FactoryCore>(FindObjectsSortMode.None);

        FactoryCore nearestFactory = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (FactoryCore factory in factories)
        {
            if (factory == null ||
                !factory.TryGetComponent(out FactoryHealth health) ||
                !health.IsAlive)
            {
                continue;
            }

            float distanceSqr =
                (factory.transform.position - position).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
                continue;

            nearestDistanceSqr = distanceSqr;
            nearestFactory = factory;
        }

        return nearestFactory != null
            ? nearestFactory.gameObject
            : null;
    }

    private void CompleteFailedRaid()
    {
        ResetRaidState();

        gameTimeManager?.NotifyRaidFailed();

        if (gameTimeManager != null &&
            !gameTimeManager.IsGameOver)
        {
            gameTimeManager.NotifyRaidResolved();
        }

        Debug.Log("RaidManager: 실패 레이드 종료", this);
    }

    private void ResetRaidState()
    {
        State = RaidState.Inactive;
        spawnedEnemies.Clear();
        activeSpawnBatchCount = 0;
        hasEnemyExisted = false;
        retreatDelayRemaining = 0f;
        retreatCountdownStarted = false;
        isRetreating = false;
    }

    private int CalculateEnemyCount(
        int currentDay,
        RaidEventType raidEventType)
    {
        int safeDay = Mathf.Max(1, currentDay);

        int baseCount =
            raidEventType == RaidEventType.Small
                ? baseSmallRaidEnemyCount
                : baseMediumRaidEnemyCount;

        int increaseInterval =
            raidEventType == RaidEventType.Small
                ? smallRaidIncreaseIntervalDays
                : mediumRaidIncreaseIntervalDays;

        int increaseSteps =
            (safeDay - 1) /
            Mathf.Max(1, increaseInterval);

        return Mathf.Max(
            1,
            baseCount +
            increaseSteps * enemyCountIncreaseAmount);
    }

    private void CleanupEnemyList()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
                spawnedEnemies.RemoveAt(i);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Start Small Raid")]
    private void DebugStartSmallRaid()
    {
        int day =
            gameTimeManager != null
                ? gameTimeManager.CurrentDay
                : 1;

        BeginSpawnEvent(day, RaidEventType.Small);
    }

    [ContextMenu("Debug/Start Medium Raid")]
    private void DebugStartMediumRaid()
    {
        int day =
            gameTimeManager != null
                ? gameTimeManager.CurrentDay
                : 1;

        BeginSpawnEvent(day, RaidEventType.Medium);
    }

    [ContextMenu("Debug/Print Raid State")]
    private void DebugPrintRaidState()
    {
        int day =
            gameTimeManager != null
                ? gameTimeManager.CurrentDay
                : 1;

        Debug.Log(
            $"Raid State: {State}, " +
            $"Enemies: {ActiveEnemyCount}, " +
            $"Spawning: {IsSpawning}, " +
            $"Small Count: {CalculateEnemyCount(day, RaidEventType.Small)}, " +
            $"Medium Count: {CalculateEnemyCount(day, RaidEventType.Medium)}, " +
            $"Retreating: {isRetreating}",
            this);
    }
#endif
}
