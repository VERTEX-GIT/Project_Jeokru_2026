using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameTimeDisplay : MonoBehaviour
{
    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private GameTimeManager gameTimeManager;

    private void Awake()
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
    }

    private void OnEnable()
    {
        if (gameTimeManager == null)
        {
            return;
        }

        gameTimeManager.TimeChanged +=
            HandleTimeChanged;

        gameTimeManager.DayChanged +=
            HandleDayChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (gameTimeManager == null)
        {
            return;
        }

        gameTimeManager.TimeChanged -=
            HandleTimeChanged;

        gameTimeManager.DayChanged -=
            HandleDayChanged;
    }

    private void HandleTimeChanged(
        int currentTime)
    {
        Refresh();
    }

    private void HandleDayChanged(
        int currentDay)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (timeText == null ||
            gameTimeManager == null)
        {
            return;
        }

        timeText.text =
            $"{gameTimeManager.CurrentDay}일차  " +
            $"{gameTimeManager.CurrentTime:00}";
    }
}