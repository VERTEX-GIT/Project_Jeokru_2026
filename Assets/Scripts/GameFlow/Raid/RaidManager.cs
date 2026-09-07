using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public bool IsRaidActive
    {
        get;
        private set;
    }

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
        CleanupEnemyList();

        if (!IsRaidActive)
        {
            return;
        }

        // 성공/실패 판정은 다음 구현에서 추가.
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

        IsRaidActive = true;

        StartCoroutine(
            SpawnBatchRoutine(
                enemiesPerSecond,
                duration));
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
                if (TrySpawnEnemy())
                {
                    spawnedThisSecond++;
                }
                else
                {
                    // 입구가 모두 예약/점유되어 있다면
                    // 손실시키지 않고 다음 프레임에 다시 시도.
                    yield return null;
                }
            }

            if (second <
                duration - 1)
            {
                yield return
                    new WaitForSeconds(1f);
            }
        }

        activeSpawnBatchCount--;
    }

    private bool TrySpawnEnemy()
    {
        UnitData selectedData =
            Random.value < 0.5f
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

        return true;
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
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Start Raid Spawn Event")]
    private void DebugStartRaid()
    {
        int day =
            gameTimeManager != null
                ? gameTimeManager.CurrentDay
                : 1;

        BeginSpawnEvent(day);
    }
#endif
}