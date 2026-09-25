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

    [Header("Game End")]
    [SerializeField]
    [Min(1)]
    private int finalDay = 30;

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

    public bool IsVictory
    {
        get;
        private set;
    }

    public bool IsDialogueBlocking
    {
        get;
        private set;
    }

    public bool IsWaitingForDayEnd
    {
        get;
        private set;
    }

    public bool IsRunning =>
        !PauseMenu.IsPaused &&
        !IsGameOver &&
        !IsDialogueBlocking &&
        !IsWaitingForDayEnd;

    public float TimeIncreaseInterval =>
        timeIncreaseInterval;

    public int FinalDay =>
        finalDay;

    public bool IsWaitingForRaid =>
        CurrentTime == 60 &&
        RaidManager.Instance != null &&
        RaidManager.Instance.IsRaidActive;

    public event Action<int> TimeChanged;
    public event Action<int> DayChanged;
    public event Action<int, int> TimeTicked;
    public event Action<int> DayEndRequested;

    // 다음 날 번호와 시작 시간으로 전환된 직후,
    // 새 날의 DayChanged 처리 전에 체크포인트 저장을 요청한다.
    public event Action<int> DayCheckpointReached;
    public event Action<bool> GameFinished;

    private float elapsedIntervalTime;

    public void CheckFactoryDefeat()
    {
        if (IsGameOver)
        {
            return;
        }

        FactoryHealth[] factories =
            FindObjectsByType<FactoryHealth>(
                FindObjectsSortMode.None);

        if (factories.Length == 0)
        {
            return;
        }

        foreach (FactoryHealth factory
                 in factories)
        {
            if (factory.IsAlive)
            {
                return;
            }
        }

        FinishGame(false);
    }

    public void NotifyRaidSucceeded()
    {
        if (!IsFinalRaidResolutionTime() ||
            IsGameOver)
        {
            return;
        }

        CheckFactoryDefeat();

        if (!IsGameOver)
        {
            FinishGame(true);
        }
    }

    public void NotifyRaidFailed()
    {
        if (!IsFinalRaidResolutionTime() ||
            IsGameOver)
        {
            return;
        }

        FinishGame(false);
    }

    // 60에서 진행 중이던 레이드가 완전히 종료되면
    // 다음 날로 넘기기 전에 해당 일차의 종료 이벤트를 요청한다.
    public void NotifyRaidResolved()
    {
        if (IsGameOver ||
            CurrentTime != 60 ||
            IsWaitingForRaid ||
            IsWaitingForDayEnd)
        {
            return;
        }

        RequestDayEnd();
    }

    public void CompleteDayEnd()
    {
        if (IsGameOver ||
            !IsWaitingForDayEnd)
        {
            return;
        }

        IsWaitingForDayEnd = false;
        elapsedIntervalTime = 0f;

        AdvanceToNextDay();
    }

    public void SetDialogueBlocking(
        bool blocked)
    {
        IsDialogueBlocking =
            blocked;

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.Dialogue,
            blocked);

        if (blocked)
        {
            elapsedIntervalTime = 0f;
        }
    }

    public void RestoreTime(
        int day,
        int time)
    {
        if (IsGameOver)
        {
            Time.timeScale = 1f;
        }

        CurrentDay =
            Mathf.Max(
                1,
                day);

        CurrentTime =
            Mathf.Clamp(
                time,
                1,
                60);

        elapsedIntervalTime = 0f;
        IsGameOver = false;
        IsVictory = false;
        IsDialogueBlocking = false;
        IsWaitingForDayEnd = false;

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.Dialogue,
            false);

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.GameOver,
            false);

        DayChanged?.Invoke(
            CurrentDay);

        TimeChanged?.Invoke(
            CurrentTime);

        TimeTicked?.Invoke(
            CurrentDay,
            CurrentTime);
    }

    private void RequestDayEnd()
    {
        IsWaitingForDayEnd = true;
        elapsedIntervalTime = 0f;

        if (DayEndRequested == null)
        {
            CompleteDayEnd();
            return;
        }

        DayEndRequested.Invoke(
            CurrentDay);
    }

    private void AdvanceToNextDay()
    {
        CurrentTime = 1;
        CurrentDay++;

        DayCheckpointReached?.Invoke(
            CurrentDay);

        DayChanged?.Invoke(
            CurrentDay);

        TimeChanged?.Invoke(
            CurrentTime);

        TimeTicked?.Invoke(
            CurrentDay,
            CurrentTime);
    }

    private bool IsFinalRaidResolutionTime()
    {
        return
            CurrentDay >= finalDay &&
            CurrentTime == 60;
    }

    private void FinishGame(bool victory)
    {
        if (IsGameOver)
        {
            return;
        }

        IsVictory = victory;
        IsGameOver = true;

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.GameOver,
            true);

        GameFinished?.Invoke(
            victory);

        Time.timeScale = 0f;
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
        InitializeTime();
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (IsWaitingForRaid)
        {
            elapsedIntervalTime = 0f;
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

            if (!IsRunning ||
                IsWaitingForRaid)
            {
                elapsedIntervalTime = 0f;
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (IsGameOver)
            {
                Time.timeScale = 1f;
            }

            GameplayPauseController.SetPaused(
                GameplayPauseController
                    .PauseReason.Dialogue,
                false);

            GameplayPauseController.SetPaused(
                GameplayPauseController
                    .PauseReason.GameOver,
                false);

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
        IsVictory = false;
        IsDialogueBlocking = false;
        IsWaitingForDayEnd = false;

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.Dialogue,
            false);

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.GameOver,
            false);
    }

    private void AdvanceTime()
    {
        if (!IsRunning ||
            IsWaitingForRaid)
        {
            return;
        }

        CurrentTime++;

        if (CurrentTime > 60)
        {
            RequestDayEnd();
            return;
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
        IsGameOver =
            gameOver;

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.GameOver,
            gameOver);
    }

    public void ResetTime()
    {
        if (IsGameOver)
        {
            Time.timeScale = 1f;
        }

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

    [ContextMenu("Debug/Save Game Time")]
    private void DebugSaveGameTime()
    {
        SaveManager.Save(
            this);
    }

    [ContextMenu("Debug/Load Game Time")]
    private void DebugLoadGameTime()
    {
        SaveManager.TryLoad(
            this);
    }
#endif
}
