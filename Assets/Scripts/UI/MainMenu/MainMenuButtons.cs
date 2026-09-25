using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuButtons : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField]
    private string sceneToLoad =
        "InGame";

    [Header("UI")]
    [SerializeField]
    private CanvasGroup settingsPanel;

    [SerializeField]
    private Button continueButton;

    private void Start()
    {
        SetGroupVisible(
            settingsPanel,
            false);

        RefreshContinueButton();
    }

    public void NewGame()
    {
        if (!CanChangeToGameScene())
        {
            return;
        }

        if (!SaveManager.DeleteSave())
        {
            Debug.LogError(
                "MainMenuButtons: 기존 세이브 파일을 삭제하지 못했습니다.",
                this);

            return;
        }

        GameSession.StartNewGame();

        SceneChanger.Instance
            .ChangeScene(
                sceneToLoad);
    }

    public void ContinueGame()
    {
        if (!CanChangeToGameScene())
        {
            return;
        }

        if (!SaveManager.HasSave)
        {
            RefreshContinueButton();
            return;
        }

        GameSession.ContinueGame();

        SceneChanger.Instance
            .ChangeScene(
                sceneToLoad);
    }

    // 기존 씬 버튼 연결이 남아 있어도 새 게임으로 동작하도록 유지한다.
    public void StartGame()
    {
        NewGame();
    }

    public void OpenSettings()
    {
        SetGroupVisible(
            settingsPanel,
            true);
    }

    public void CloseSettings()
    {
        SetGroupVisible(
            settingsPanel,
            false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication
            .isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool CanChangeToGameScene()
    {
        if (SceneChanger.Instance == null)
        {
            Debug.LogError(
                "MainMenuButtons: SceneChanger가 없습니다.",
                this);

            return false;
        }

        if (SceneChanger.Instance.IsChanging)
        {
            return false;
        }

        return true;
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.interactable =
            SaveManager.HasSave;
    }

    private static void SetGroupVisible(
        CanvasGroup group,
        bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha =
            visible
                ? 1f
                : 0f;

        group.interactable =
            visible;

        group.blocksRaycasts =
            visible;
    }
}
