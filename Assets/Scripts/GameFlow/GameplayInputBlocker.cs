using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplayInputBlocker : MonoBehaviour
{
    private readonly struct RaycasterState
    {
        public readonly GraphicRaycaster Raycaster;
        public readonly bool Enabled;

        public RaycasterState(
            GraphicRaycaster raycaster,
            bool enabled)
        {
            Raycaster = raycaster;
            Enabled = enabled;
        }
    }

    public static GameplayInputBlocker Instance
    {
        get;
        private set;
    }

    public static bool IsBlocked =>
        PauseMenu.IsPaused ||
        IsDialogueBlocked;

    public static bool IsDialogueBlocked =>
        Instance != null &&
        Instance.isDialogueBlocked;

    private ObjectPlacementController
        placementController;

    private UnitSelectionController
        unitSelectionController;

    private MedicineUseController
        medicineUseController;

    private Transform dialogueRoot;

    private readonly List<RaycasterState>
        raycasterStates = new();

    private bool isDialogueBlocked;

    private bool placementWasEnabled;
    private bool selectionWasEnabled;
    private bool medicineWasEnabled;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        dialogueRoot = transform;

        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SetDialogueBlocked(
            false);

        Instance = null;
    }

    public void SetDialogueBlocked(
        bool blocked)
    {
        if (isDialogueBlocked ==
            blocked)
        {
            return;
        }

        ResolveReferences();

        isDialogueBlocked =
            blocked;

        if (blocked)
        {
            BlockGameplayInput();
            BlockGameplayUi();
            return;
        }

        RestoreGameplayInput();
        RestoreGameplayUi();
    }

    private void ResolveReferences()
    {
        if (placementController == null)
        {
            placementController =
                FindAnyObjectByType<
                    ObjectPlacementController>();
        }

        if (unitSelectionController == null)
        {
            unitSelectionController =
                FindAnyObjectByType<
                    UnitSelectionController>();
        }

        if (medicineUseController == null)
        {
            medicineUseController =
                MedicineUseController.Instance;
        }

        if (medicineUseController == null)
        {
            medicineUseController =
                FindAnyObjectByType<
                    MedicineUseController>();
        }
    }

    private void BlockGameplayInput()
    {
        if (placementController != null)
        {
            placementWasEnabled =
                placementController.enabled;

            placementController
                .CancelPlacement();

            placementController.enabled =
                false;
        }

        if (unitSelectionController != null)
        {
            selectionWasEnabled =
                unitSelectionController.enabled;

            unitSelectionController
                .ClearSelection();

            unitSelectionController.enabled =
                false;
        }

        if (medicineUseController != null)
        {
            medicineWasEnabled =
                medicineUseController.enabled;

            medicineUseController
                .CancelMedicineSelection();

            medicineUseController.enabled =
                false;
        }
    }

    private void RestoreGameplayInput()
    {
        if (placementController != null)
        {
            placementController.enabled =
                placementWasEnabled;
        }

        if (unitSelectionController != null)
        {
            unitSelectionController.enabled =
                selectionWasEnabled;
        }

        if (medicineUseController != null)
        {
            medicineUseController.enabled =
                medicineWasEnabled;
        }
    }

    private void BlockGameplayUi()
    {
        raycasterStates.Clear();

        GraphicRaycaster[] raycasters =
            FindObjectsByType<GraphicRaycaster>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (GraphicRaycaster raycaster
                 in raycasters)
        {
            if (raycaster == null ||
                IsDialogueUi(
                    raycaster.transform))
            {
                continue;
            }

            raycasterStates.Add(
                new RaycasterState(
                    raycaster,
                    raycaster.enabled));

            raycaster.enabled =
                false;
        }

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null &&
            !IsDialogueUi(
                EventSystem.current
                    .currentSelectedGameObject
                    .transform))
        {
            EventSystem.current
                .SetSelectedGameObject(
                    null);
        }
    }

    private void RestoreGameplayUi()
    {
        foreach (RaycasterState state
                 in raycasterStates)
        {
            if (state.Raycaster != null)
            {
                state.Raycaster.enabled =
                    state.Enabled;
            }
        }

        raycasterStates.Clear();
    }

    private bool IsDialogueUi(
        Transform target)
    {
        if (target == null ||
            dialogueRoot == null)
        {
            return false;
        }

        if (target == dialogueRoot ||
            target.IsChildOf(dialogueRoot))
        {
            return true;
        }

        // DialogueSystem이 Canvas 아래에 있는 구조도 허용한다.
        return dialogueRoot.IsChildOf(
            target);
    }
}
