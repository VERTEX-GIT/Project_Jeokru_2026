using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance
    {
        get;
        private set;
    }

    [field: SerializeField]
    public bool IsChanging
    {
        get;
        private set;
    }

    [Header("UI")]
    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    [SerializeField]
    [Min(0f)]
    private float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeFade();
    }

    private void InitializeFade()
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    public void ChangeScene(
        string sceneName)
    {
        if (IsChanging)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "SceneChanger: 씬 이름이 비어 있습니다.",
                this);

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            Debug.LogError(
                $"SceneChanger: Build Settings에서 " +
                $"씬 '{sceneName}'을 찾을 수 없습니다.",
                this);

            return;
        }

        StartCoroutine(
            ChangeSceneRoutine(
                sceneName));
    }

    private IEnumerator ChangeSceneRoutine(
        string sceneName)
    {
        IsChanging = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts =
                true;

            fadeCanvasGroup.interactable =
                true;

            yield return fadeCanvasGroup
                .DOFade(
                    1f,
                    fadeDuration)
                .SetEase(
                    Ease.OutSine)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName);

        if (operation == null)
        {
            Debug.LogError(
                $"SceneChanger: '{sceneName}' " +
                "씬 로드를 시작하지 못했습니다.",
                this);

            ResetTransitionState();
            yield break;
        }

        operation.allowSceneActivation =
            false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        operation.allowSceneActivation =
            true;

        while (!operation.isDone)
        {
            yield return null;
        }

        Time.timeScale = 1f;

        if (fadeCanvasGroup != null)
        {
            yield return fadeCanvasGroup
                .DOFade(
                    0f,
                    fadeDuration)
                .SetEase(
                    Ease.InSine)
                .SetUpdate(true)
                .WaitForCompletion();

            fadeCanvasGroup.blocksRaycasts =
                false;

            fadeCanvasGroup.interactable =
                false;
        }

        IsChanging = false;
    }

    private void ResetTransitionState()
    {
        IsChanging = false;

        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }
}