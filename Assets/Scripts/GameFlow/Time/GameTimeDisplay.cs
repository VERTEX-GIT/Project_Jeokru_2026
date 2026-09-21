using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameTimeDisplay : MonoBehaviour
{
    [SerializeField]
    private TMP_Text dayText;

    [SerializeField]
    private Slider timeSlider;

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
        if (gameTimeManager == null)
        {
            return;
        }

        if (dayText != null)
        {
            dayText.text =
                $"{gameTimeManager.CurrentDay}일차";
        }

        if (timeSlider != null)
        {
            timeSlider.normalizedValue =
                (gameTimeManager.CurrentTime - 1f) / 59f;
        }
    }
}
