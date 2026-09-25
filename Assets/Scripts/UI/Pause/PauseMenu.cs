using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private CanvasGroup pauseCanvasGroup;

    [SerializeField]
    private SettingsMenuController settingsMenu;

    [Header("Scene")]
    [SerializeField]
    private string mainMenuSceneName =
        "MainTitle";

    private ObjectPlacementController
        placementController;

    public static bool IsPaused =>
        GameplayPauseController.IsPaused;

    public static bool IsManualPaused =>
        GameplayPauseController.HasReason(
            GameplayPauseController
                .PauseReason.ManualPause);

    private void Start()
    {
        SetPausePanelVisible(
            false);

        if (settingsMenu != null &&
            settingsMenu.IsOpen)
        {
            settingsMenu.Close();
        }

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.ManualPause,
            false);

        if (!GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.GameOver))
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.GameOver))
        {
            return;
        }

        if (IsMainMenuScene())
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.escapeKey
                .wasPressedThisFrame)
        {
            return;
        }

        HandleEscapePressed();
    }

    private void OnDestroy()
    {
        if (!IsManualPaused)
        {
            return;
        }

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.ManualPause,
            false);

        if (!GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.GameOver))
        {
            Time.timeScale = 1f;
        }
    }

    private bool IsMainMenuScene()
    {
        return SceneManager
            .GetActiveScene()
            .name == mainMenuSceneName;
    }

    private void HandleEscapePressed()
    {
        if (GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.Dialogue))
        {
            return;
        }

        var medicine =
            MedicineUseController.Instance;

        if (!IsManualPaused &&
            medicine != null &&
            (medicine.CancelMedicineSelection() ||
             medicine.InputConsumedThisFrame))
        {
            return;
        }

        if (placementController == null)
        {
            placementController =
                FindAnyObjectByType<
                    ObjectPlacementController>();
        }

        // 실행 순서와 관계없이 첫 Esc는 배치 취소에만 사용한다.
        if (!IsManualPaused &&
            placementController != null &&
            (placementController.CancelPlacement() ||
             placementController.InputConsumedThisFrame))
        {
            return;
        }

        if (SceneChanger.Instance != null &&
            SceneChanger.Instance.IsChanging)
        {
            return;
        }

        if (!IsManualPaused)
        {
            PauseGame();
            return;
        }

        if (settingsMenu != null &&
            settingsMenu.IsOpen)
        {
            CloseSettings();
            return;
        }

        ResumeGame();
    }

    public void PauseGame()
    {
        if (IsMainMenuScene())
        {
            return;
        }

        if (IsManualPaused ||
            GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.Dialogue) ||
            GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.GameOver))
        {
            return;
        }

        SetPausePanelVisible(
            true);

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.ManualPause,
            true);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!IsManualPaused)
        {
            return;
        }

        if (GameplayPauseController.HasReason(
                GameplayPauseController
                    .PauseReason.GameOver))
        {
            return;
        }

        if (settingsMenu != null &&
            settingsMenu.IsOpen)
        {
            settingsMenu.Close();
        }

        SetPausePanelVisible(
            false);

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.ManualPause,
            false);

        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        if (IsMainMenuScene())
        {
            return;
        }

        if (!IsManualPaused)
        {
            PauseGame();
        }

        if (!IsManualPaused)
        {
            return;
        }

        if (settingsMenu == null)
        {
            Debug.LogError(
                "PauseMenu: SettingsMenuController가 연결되지 않았습니다.",
                this);

            return;
        }

        settingsMenu.Open();
    }

    public void CloseSettings()
    {
        if (settingsMenu == null)
        {
            return;
        }

        settingsMenu.Close();
    }

    public void GoToMainMenu()
    {
        if (SceneChanger.Instance == null)
        {
            Debug.LogError(
                "PauseMenu: SceneChanger가 없습니다.",
                this);

            return;
        }

        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.ManualPause,
            false);

        Time.timeScale = 1f;

        SetPausePanelVisible(
            false);

        if (settingsMenu != null &&
            settingsMenu.IsOpen)
        {
            settingsMenu.Close();
        }

        SceneChanger.Instance.ChangeScene(
            mainMenuSceneName);
    }

    public void QuitGame()
    {
        GameplayPauseController.SetPaused(
            GameplayPauseController
                .PauseReason.ManualPause,
            false);

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication
            .isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPausePanelVisible(
        bool visible)
    {
        if (pauseCanvasGroup == null)
        {
            return;
        }

        pauseCanvasGroup.alpha =
            visible ? 1f : 0f;

        pauseCanvasGroup.interactable =
            visible;

        pauseCanvasGroup.blocksRaycasts =
            visible;
    }
}
