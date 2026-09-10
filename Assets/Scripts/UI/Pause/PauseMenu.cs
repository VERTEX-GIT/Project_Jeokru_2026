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

    private ObjectPlacementController placementController;

    public static bool IsPaused
    {
        get;
        private set;
    }

    private void Start()
    {
        SetPausePanelVisible(false);

        if (settingsMenu != null &&
            settingsMenu.IsOpen)
        {
            settingsMenu.Close();
        }

        Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
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
        if (IsPaused)
        {
            Time.timeScale = 1f;
            IsPaused = false;
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
        var medicine = MedicineUseController.Instance;
        if (!IsPaused && medicine != null &&
            (medicine.CancelMedicineSelection() || medicine.InputConsumedThisFrame))
        {
            return;
        }

        if (placementController == null)
            placementController = FindAnyObjectByType<ObjectPlacementController>();

        // 실행 순서와 관계없이 첫 Esc는 배치 취소에만 사용한다.
        if (!IsPaused && placementController != null &&
            (placementController.CancelPlacement() || placementController.InputConsumedThisFrame))
            return;

        if (IsMainMenuScene())
        {
            return;
        }

        if (SceneChanger.Instance != null &&
            SceneChanger.Instance.IsChanging)
        {
            return;
        }

        if (!IsPaused)
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

        if (IsPaused)
        {
            return;
        }

        SetPausePanelVisible(true);

        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void ResumeGame()
    {
        if (settingsMenu != null &&
            settingsMenu.IsOpen)
        {
            settingsMenu.Close();
        }

        SetPausePanelVisible(false);

        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void OpenSettings()
    {
        if (IsMainMenuScene())
        {
            return;
        }

        if (!IsPaused)
        {
            PauseGame();
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

        IsPaused = false;
        Time.timeScale = 1f;

        SetPausePanelVisible(false);

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
        IsPaused = false;
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
