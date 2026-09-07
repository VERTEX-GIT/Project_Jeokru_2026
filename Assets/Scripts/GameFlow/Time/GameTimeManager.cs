using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance
    {
        get;
        private set;
    }

    [Header("Start Time")]
    [SerializeField]
    [Min(1)]
    private int startDay = 1;

    [SerializeField]
    [Range(1, 60)]
    private int startTime = 1;

    [Header("Time Progress")]
    [SerializeField]
    [Min(0.01f)]
    private float timeIncreaseInterval = 1f;

    public int CurrentDay
    {
        get;
        private set;
    }

    public int CurrentTime
    {
        get;
        private set;
    }

    public bool IsGameOver
    {
        get;
        private set;
    }

    public bool IsRunning =>
        !PauseMenu.IsPaused &&
        !IsGameOver;

    public float TimeIncreaseInterval =>
        timeIncreaseInterval;

    public event Action<int> TimeChanged;

    public event Action<int> DayChanged;

    public event Action<int, int> TimeTicked;

    private float elapsedIntervalTime;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeTime();
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        elapsedIntervalTime +=
            Time.deltaTime;

        while (elapsedIntervalTime >=
               timeIncreaseInterval)
        {
            elapsedIntervalTime -=
                timeIncreaseInterval;

            AdvanceTime();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeTime()
    {
        CurrentDay =
            Mathf.Max(
                1,
                startDay);

        CurrentTime =
            Mathf.Clamp(
                startTime,
                1,
                60);

        elapsedIntervalTime = 0f;
        IsGameOver = false;
    }

    private void AdvanceTime()
    {
        CurrentTime++;

        if (CurrentTime > 60)
        {
            CurrentTime = 1;
            CurrentDay++;

            DayChanged?.Invoke(
                CurrentDay);
        }

        TimeChanged?.Invoke(
            CurrentTime);

        TimeTicked?.Invoke(
            CurrentDay,
            CurrentTime);
    }

    public void SetGameOver(
        bool gameOver)
    {
        IsGameOver = gameOver;
    }

    public void ResetTime()
    {
        InitializeTime();

        DayChanged?.Invoke(
            CurrentDay);

        TimeChanged?.Invoke(
            CurrentTime);

        TimeTicked?.Invoke(
            CurrentDay,
            CurrentTime);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Advance Time")]
    private void DebugAdvanceTime()
    {
        AdvanceTime();
    }
#endif
}