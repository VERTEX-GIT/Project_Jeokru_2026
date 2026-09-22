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

    [Header("Result UI")]
    [SerializeField]
    private GameObject resultScreen;

    [SerializeField]
    private TMP_Text resultText;

    private void Update()
    {
        if (gameTimeManager == null || resultScreen == null) return;

        bool show = gameTimeManager.IsGameOver;
        if (show && resultText != null)
        {
            resultText.text = gameTimeManager.IsVictory
                ? "승리\n30일차 레이드 성공"
                : "패배\n모든 공장이 파괴되었습니다";
        }

        if (resultScreen.activeSelf != show)
        {
            resultScreen.SetActive(show);
        }
    }

    public void ReturnToTitle()
    {
        if (SceneChanger.Instance != null)
            SceneChanger.Instance.ChangeScene("MainTitle");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainTitle");
    }

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
