using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    private Coroutine dialogueRoutine;
    private bool isHandlingDayEnd;

    private bool ShouldPlayOpeningDialogue =>
        playOpeningDialogue &&
        GameSession.StartMode ==
            GameStartMode.NewGame;

    private void Awake()
    {
        ResolveReferences();

        visualController?
            .HideAllVisuals();

        SetDialogueInputEnabled(
            false);

        if (ShouldPlayOpeningDialogue &&
            gameTimeManager != null)
        {
            gameTimeManager.SetDialogueBlocking(
                true);
        }
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

    private void OnDisable()
    {
        if (gameTimeManager != null)
        {
            gameTimeManager.DayEndRequested -=
                HandleDayEndRequested;
        }

        SetDialogueInputEnabled(
            false);
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner =
                FindAnyObjectByType<
                    DialogueRunner>();
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

        while (dialogueRunner
               .IsDialogueRunning)
        {
            yield return null;
        }

        FinishDialogueFlow(
            completeDayEndAfterDialogue);
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
