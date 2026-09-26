using System.Collections;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CutsceneFadeController :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    [SerializeField]
    [Min(0f)]
    private float fadeDuration = 0.8f;

    private void Awake()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError(
                "CutsceneFadeController: Fade CanvasGroup이 지정되지 않았습니다.",
                this);

            return;
        }

        fadeCanvasGroup.DOKill();
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private void OnDisable()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.DOKill();
        }
    }

    public void SetBlackImmediate()
    {
        SetImmediate(
            1f,
            true);
    }

    public void SetClearImmediate()
    {
        SetImmediate(
            0f,
            false);
    }

    public IEnumerator FadeOut()
    {
        yield return FadeTo(
            1f,
            Ease.OutSine,
            true);
    }

    public IEnumerator FadeIn()
    {
        yield return FadeTo(
            0f,
            Ease.InSine,
            false);
    }

    private void SetImmediate(
        float alpha,
        bool blockInput)
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.DOKill();

        fadeCanvasGroup.alpha =
            alpha;

        fadeCanvasGroup.interactable =
            blockInput;

        fadeCanvasGroup.blocksRaycasts =
            blockInput;
    }

    private IEnumerator FadeTo(
        float targetAlpha,
        Ease ease,
        bool keepInputBlocked)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.DOKill();

        fadeCanvasGroup.interactable =
            true;

        fadeCanvasGroup.blocksRaycasts =
            true;

        yield return fadeCanvasGroup
            .DOFade(
                targetAlpha,
                fadeDuration)
            .SetEase(
                ease)
            .SetUpdate(true)
            .WaitForCompletion();

        if (keepInputBlocked)
        {
            yield break;
        }

        fadeCanvasGroup.interactable =
            false;

        fadeCanvasGroup.blocksRaycasts =
            false;
    }
}
