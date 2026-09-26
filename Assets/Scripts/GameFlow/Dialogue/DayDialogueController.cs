using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

[DisallowMultipleComponent]
public sealed class DayDialogueController : MonoBehaviour
{
    [Serializable]
    private sealed class DayDialogueEntry
    {
        [Min(1)]
        public int day = 1;

        public string nodeName;
    }

    [Header("References")]
    [SerializeField]
    private DialogueRunner dialogueRunner;

    [SerializeField]
    private LineAdvancer lineAdvancer;

    [SerializeField]
    private DialogueVisualController visualController;

    [SerializeField]
    private CanvasGroup dialogueInputGroup;

    [SerializeField]
    private GameTimeManager gameTimeManager;

    [Header("Opening Dialogue")]
    [SerializeField]
    private bool playOpeningDialogue = true;

    [SerializeField]
    private string openingNodeName = "Day00_Nightmare";

    [Header("Day End Dialogues")]
    [SerializeField]
    private List<DayDialogueEntry> dayEndDialogues = new()
    {
        new DayDialogueEntry
        {
            day = 1,
            nodeName = "Day01_Lobby"
        }
    };

    [Header("Skip")]
    [SerializeField, Min(0.1f)]
    private float skipHoldDuration = 1f;

    [SerializeField, Min(0f)]
    private float initialSkipGuideDuration = 2f;

    [SerializeField, Min(0f)]
    private float releaseSkipGuideDuration = 1f;

    [SerializeField, Min(0f)]
    private float skipFillRevealDelay = 0.2f;

    [SerializeField, Min(0f)]
    private float skipGuideRevealLeadTime = 0.05f;

    [SerializeField, Min(0.01f)]
    private float skipGuideFadeDuration = 0.15f;

    [SerializeField]
    private CanvasGroup skipGuideGroup;

    [SerializeField]
    private TMP_Text skipBaseText;

    [SerializeField]
    private TMP_Text skipFillText;

    [SerializeField]
    private RectTransform skipFillMask;

    private Coroutine dialogueRoutine;
    private bool isHandlingDayEnd;

    private float skipHoldTimer;
    private float skipGuideTimer;
    private float skipGuideTargetAlpha;
    private bool skipRequested;
    private bool isTrackingSpacePress;

    private bool ShouldPlayOpeningDialogue =>
        playOpeningDialogue &&
        GameSession.StartMode ==
            GameStartMode.NewGame;

    private bool IsDialogueFlowActive =>
        dialogueRoutine != null;

    private void Awake()
    {
        ResolveReferences();

        visualController?
            .HideAllVisuals();

        SetDialogueInputEnabled(
            false);

        ResetSkipState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (gameTimeManager != null)
        {
            gameTimeManager.DayEndRequested +=
                HandleDayEndRequested;
        }
    }

    private void Start()
    {
        if (!ShouldPlayOpeningDialogue)
        {
            return;
        }

        if (gameTimeManager != null)
        {
            gameTimeManager.SetDialogueBlocking(
                true);
        }

        if (string.IsNullOrWhiteSpace(
                openingNodeName))
        {
            gameTimeManager?
                .SetDialogueBlocking(false);

            return;
        }

        StartDialogueRoutine(
            openingNodeName,
            false);
    }

    private void Update()
    {
        UpdateSkipInput();
        UpdateSkipGuideVisibility();
        UpdateSkipGuideFade();
    }

    private void OnDisable()
    {
        if (gameTimeManager != null)
        {
            gameTimeManager.DayEndRequested -=
                HandleDayEndRequested;
        }

        SetDialogueInputEnabled(
            false);

        ResetSkipState();
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner =
                FindAnyObjectByType<
                    DialogueRunner>();
        }

        if (lineAdvancer == null &&
            dialogueRunner != null)
        {
            lineAdvancer =
                dialogueRunner
                    .GetComponentInChildren<
                        LineAdvancer>(
                        true);
        }

        if (lineAdvancer == null)
        {
            lineAdvancer =
                FindAnyObjectByType<
                    LineAdvancer>();
        }

        if (visualController == null)
        {
            visualController =
                FindAnyObjectByType<
                    DialogueVisualController>();
        }

        if (dialogueInputGroup == null &&
            dialogueRunner != null)
        {
            dialogueInputGroup =
                FindDialogueInputGroup(
                    dialogueRunner.transform);
        }

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

    private CanvasGroup FindDialogueInputGroup(
        Transform root)
    {
        if (root == null)
        {
            return null;
        }

        CanvasGroup[] groups =
            root.GetComponentsInChildren<
                CanvasGroup>(
                true);

        foreach (CanvasGroup group
                 in groups)
        {
            if (group == null)
            {
                continue;
            }

            if (group.gameObject.name ==
                "Line Presenter")
            {
                return group;
            }
        }

        return null;
    }

    private void HandleDayEndRequested(
        int day)
    {
        if (isHandlingDayEnd)
        {
            return;
        }

        string nodeName =
            FindDayEndNode(
                day);

        if (string.IsNullOrWhiteSpace(
                nodeName))
        {
            gameTimeManager?
                .CompleteDayEnd();

            return;
        }

        isHandlingDayEnd =
            true;

        StartDialogueRoutine(
            nodeName,
            true);
    }

    private string FindDayEndNode(
        int day)
    {
        if (dayEndDialogues == null)
        {
            return null;
        }

        foreach (DayDialogueEntry entry
                 in dayEndDialogues)
        {
            if (entry == null ||
                entry.day != day)
            {
                continue;
            }

            return entry.nodeName;
        }

        return null;
    }

    private void StartDialogueRoutine(
        string nodeName,
        bool completeDayEndAfterDialogue)
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(
                dialogueRoutine);
        }

        ResetSkipState();

        dialogueRoutine =
            StartCoroutine(
                RunDialogue(
                    nodeName,
                    completeDayEndAfterDialogue));
    }

    private IEnumerator RunDialogue(
        string nodeName,
        bool completeDayEndAfterDialogue)
    {
        ResolveReferences();

        if (dialogueRunner == null)
        {
            Debug.LogError(
                "DayDialogueController: DialogueRunner를 찾을 수 없습니다.",
                this);

            FinishDialogueFlow(
                completeDayEndAfterDialogue);

            yield break;
        }

        if (gameTimeManager != null)
        {
            gameTimeManager.SetDialogueBlocking(
                true);
        }

        visualController?
            .HideAllVisuals();

        SetDialogueInputEnabled(
            true);

        while (dialogueRunner
               .IsDialogueRunning)
        {
            yield return null;
        }

        dialogueRunner.StartDialogue(
            nodeName);

        yield return null;

        ShowSkipGuide(
            initialSkipGuideDuration);

        while (dialogueRunner
               .IsDialogueRunning)
        {
            yield return null;
        }

        FinishDialogueFlow(
            completeDayEndAfterDialogue);
    }

    private void UpdateSkipInput()
    {
        if (!IsDialogueFlowActive ||
            skipRequested ||
            Keyboard.current == null)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current;

        var spaceKey =
            keyboard.spaceKey;

        if (spaceKey.wasPressedThisFrame)
        {
            isTrackingSpacePress =
                true;

            skipHoldTimer =
                0f;

            HideSkipGuide();

            RefreshSkipFill();

            return;
        }

        if (isTrackingSpacePress)
        {
            if (spaceKey.isPressed)
            {
                skipHoldTimer +=
                    Time.unscaledDeltaTime;

                float guideRevealTime =
                    Mathf.Max(
                        0f,
                        skipFillRevealDelay -
                        skipGuideRevealLeadTime);

                if (skipHoldTimer >=
                    guideRevealTime)
                {
                    ShowSkipGuide(
                        float.PositiveInfinity);
                }

                RefreshSkipFill();

                if (skipHoldTimer >=
                    skipHoldDuration)
                {
                    RequestSkipCurrentDialogue();
                }

                return;
            }

            if (spaceKey.wasReleasedThisFrame)
            {
                float heldDuration =
                    skipHoldTimer;

                isTrackingSpacePress =
                    false;

                skipHoldTimer =
                    0f;

                RefreshSkipFill();

                HideSkipGuide();

                if (heldDuration <
                    skipHoldDuration)
                {
                    AdvanceDialogue();
                }

                return;
            }

            return;
        }

        if (keyboard.anyKey.wasPressedThisFrame)
        {
            AdvanceDialogue();
        }
    }

    private void UpdateSkipGuideVisibility()
    {
        if (skipGuideGroup == null ||
            !IsDialogueFlowActive ||
            isTrackingSpacePress)
        {
            return;
        }

        if (float.IsPositiveInfinity(
                skipGuideTimer))
        {
            return;
        }

        if (skipGuideTimer <= 0f)
        {
            HideSkipGuide();
            return;
        }

        skipGuideTimer -=
            Time.unscaledDeltaTime;

        if (skipGuideTimer <= 0f)
        {
            HideSkipGuide();
        }
    }

    private void UpdateSkipGuideFade()
    {
        if (skipGuideGroup == null)
        {
            return;
        }

        float fadeDuration =
            Mathf.Max(
                0.01f,
                skipGuideFadeDuration);

        skipGuideGroup.alpha =
            Mathf.MoveTowards(
                skipGuideGroup.alpha,
                skipGuideTargetAlpha,
                Time.unscaledDeltaTime /
                fadeDuration);
    }

    private void AdvanceDialogue()
    {
        if (dialogueRunner == null ||
            !dialogueRunner.IsDialogueRunning)
        {
            return;
        }

        if (lineAdvancer != null)
        {
            lineAdvancer.OnInputHurryUpLines();
            return;
        }

        dialogueRunner.RequestHurryUpLine();
    }

    private void RequestSkipCurrentDialogue()
    {
        if (skipRequested ||
            dialogueRunner == null ||
            !dialogueRunner.IsDialogueRunning)
        {
            return;
        }

        skipRequested =
            true;

        isTrackingSpacePress =
            false;

        skipHoldTimer =
            skipHoldDuration;

        RefreshSkipFill();

        dialogueRunner.Stop();
    }

    private void FinishDialogueFlow(
        bool completeDayEndAfterDialogue)
    {
        dialogueRoutine =
            null;

        visualController?
            .HideAllVisuals();

        SetDialogueInputEnabled(
            false);

        ResetSkipState();

        if (gameTimeManager != null)
        {
            gameTimeManager.SetDialogueBlocking(
                false);
        }

        if (!completeDayEndAfterDialogue)
        {
            return;
        }

        isHandlingDayEnd =
            false;

        gameTimeManager?
            .CompleteDayEnd();
    }

    private void ResetSkipState()
    {
        skipHoldTimer =
            0f;

        skipGuideTimer =
            0f;

        skipGuideTargetAlpha =
            0f;

        skipRequested =
            false;

        isTrackingSpacePress =
            false;

        RefreshSkipFill();

        if (skipGuideGroup != null)
        {
            skipGuideGroup.alpha =
                0f;

            skipGuideGroup.interactable =
                false;

            skipGuideGroup.blocksRaycasts =
                false;
        }
    }

    private void ShowSkipGuide(
        float duration)
    {
        skipGuideTimer =
            duration;

        skipGuideTargetAlpha =
            1f;
    }

    private void HideSkipGuide()
    {
        skipGuideTimer =
            0f;

        skipGuideTargetAlpha =
            0f;
    }

    private void RefreshSkipFill()
    {
        if (skipFillMask == null ||
            skipBaseText == null)
        {
            return;
        }

        float holdDuration =
            Mathf.Max(
                0.1f,
                skipHoldDuration);

        float revealDelay =
            Mathf.Clamp(
                skipFillRevealDelay,
                0f,
                holdDuration - 0.01f);

        float visibleDuration =
            Mathf.Max(
                0.01f,
                holdDuration -
                revealDelay);

        float progress =
            Mathf.Clamp01(
                (skipHoldTimer -
                 revealDelay) /
                visibleDuration);

        float fullWidth =
            skipBaseText
                .rectTransform
                .rect
                .width;

        Vector2 size =
            skipFillMask.sizeDelta;

        size.x =
            fullWidth *
            progress;

        skipFillMask.sizeDelta =
            size;
    }

    private void SetDialogueInputEnabled(
        bool enabled)
    {
        if (dialogueInputGroup == null)
        {
            return;
        }

        dialogueInputGroup.interactable =
            enabled;

        dialogueInputGroup.blocksRaycasts =
            enabled;
    }
}
