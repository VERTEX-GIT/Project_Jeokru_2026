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

    private readonly List<UnitCore>
        spawnedEnemies = new();

    private int activeSpawnBatchCount;

    private bool hasEnemyExisted;

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

        // 레이드가 없는 상태에서만
        // 새로운 레이드로 시작한다.
        //
        // 이미 Active거나 Failed 상태라면
        // 기존 레이드에 증원만 추가한다.
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
                    // 모든 입구가 사용 중이면
                    // 적을 버리지 않고 다음 프레임에 재시도한다.
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

        if (State == RaidState.Failed)
        {
            // 실패는 이번 레이드 동안 확정 상태.
            // 이후 아군이 증원되더라도
            // 여기서 성공 상태로 되돌리지 않는다.
            return;
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

        RaidFailed?.Invoke();

        Debug.Log(
            "RaidManager: 레이드 실패",
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
            $"Had Enemy: {hasEnemyExisted}",
            this);
    }
#endif
}