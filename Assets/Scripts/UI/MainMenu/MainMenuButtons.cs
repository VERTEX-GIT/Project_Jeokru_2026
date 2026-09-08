using UnityEngine;

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

    private void Start()
    {
        SetGroupVisible(
            settingsPanel,
            false);
    }

    public void StartGame()
    {
        if (SceneChanger.Instance == null)
        {
            Debug.LogError(
                "MainMenuButtons: SceneChanger가 없습니다.",
                this);

            return;
        }

        SceneChanger.Instance
            .ChangeScene(
                sceneToLoad);
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