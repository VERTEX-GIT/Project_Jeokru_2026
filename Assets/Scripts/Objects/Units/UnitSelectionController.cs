using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 포인터 입력으로 유닛 선택을 관리하고
// 선택된 유닛에 이동/전투/작업 명령을 전달
[DisallowMultipleComponent]
public sealed class UnitSelectionController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference pointerPositionAction;

    [SerializeField]
    private InputActionReference primaryClickAction;

    [SerializeField]
    private InputActionReference moveCommandAction;

    [Header("References")]
    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private TileOccupancyManager occupancyManager;

    [SerializeField]
    private ObjectPlacementController placementController;

    [SerializeField]
    private UnitDestinationAssigner destinationAssigner;

    [Header("Selection Drag")]
    [SerializeField]
    private RectTransform selectionBox;

    [SerializeField]
    private Canvas selectionCanvas;

    [SerializeField]
    [Min(0f)]
    private float dragThreshold = 8f;

    private readonly List<UnitSelectable> selectedUnits = new();

    private InputAction moveCommandInput;

    private Vector2 dragStartScreenPosition;
    private bool isPrimaryHeld;

    public IReadOnlyList<UnitSelectable> SelectedUnits =>
        selectedUnits;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (occupancyManager == null)
        {
            occupancyManager =
                TileOccupancyManager.Instance;
        }

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<
                    TileOccupancyManager>();
        }

        if (placementController == null)
        {
            placementController =
                FindAnyObjectByType<
                    ObjectPlacementController>();
        }

        if (destinationAssigner == null)
        {
            destinationAssigner =
                FindAnyObjectByType<
                    UnitDestinationAssigner>();
        }

        moveCommandInput =
            moveCommandAction != null
                ? moveCommandAction.action
                : primaryClickAction?.action
                    .actionMap?
                    .FindAction("MoveCommand");

        HideSelectionBox();
    }

    private void OnEnable()
    {
        if (primaryClickAction != null)
        {
            primaryClickAction.action.started +=
                OnPrimaryPress;

            primaryClickAction.action.canceled +=
                OnPrimaryRelease;

            primaryClickAction.action.Enable();
        }

        if (moveCommandInput != null)
        {
            moveCommandInput.performed +=
                OnMoveCommand;

            moveCommandInput.Enable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (primaryClickAction != null)
        {
            primaryClickAction.action.started -=
                OnPrimaryPress;

            primaryClickAction.action.canceled -=
                OnPrimaryRelease;

            primaryClickAction.action.Disable();
        }

        if (moveCommandInput != null)
        {
            moveCommandInput.performed -=
                OnMoveCommand;

            moveCommandInput.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Disable();
        }

        CancelPrimaryDrag();
        ClearSelection();
    }

    private void Update()
    {
        if (!isPrimaryHeld ||
            pointerPositionAction == null)
        {
            return;
        }

        Vector2 currentPosition =
            pointerPositionAction.action
                .ReadValue<Vector2>();

        float dragDistance =
            Vector2.Distance(
                dragStartScreenPosition,
                currentPosition);

        if (dragDistance < dragThreshold)
        {
            HideSelectionBox();
            return;
        }

        UpdateSelectionBox(
            dragStartScreenPosition,
            currentPosition);
    }

    private void OnPrimaryPress(
        InputAction.CallbackContext context)
    {
        if (placementController != null &&
            placementController.CurrentMode !=
                PlacementMode.None)
        {
            return;
        }

        if (pointerPositionAction == null)
        {
            return;
        }

        dragStartScreenPosition =
            pointerPositionAction.action
                .ReadValue<Vector2>();

        isPrimaryHeld = true;
    }

    private void OnPrimaryRelease(
        InputAction.CallbackContext context)
    {
        if (!isPrimaryHeld)
        {
            return;
        }

        if (pointerPositionAction == null)
        {
            CancelPrimaryDrag();
            return;
        }

        Vector2 endScreenPosition =
            pointerPositionAction.action
                .ReadValue<Vector2>();

        float dragDistance =
            Vector2.Distance(
                dragStartScreenPosition,
                endScreenPosition);

        bool isRangeSelection =
            dragDistance >= dragThreshold;

        isPrimaryHeld = false;

        HideSelectionBox();

        if (isRangeSelection)
        {
            ClearSelection();

            SelectUnitsInScreenRect(
                dragStartScreenPosition,
                endScreenPosition);

            return;
        }

        HandlePrimaryClick();
    }

    private void HandlePrimaryClick()
    {
        if (placementController != null &&
            placementController.CurrentMode !=
                PlacementMode.None)
        {
            return;
        }

        // 실제 월드 위치에 유닛이 있다면
        // 이동 중 여부와 관계없이 우선 선택
        if (TryGetUnitUnderPointer(
                out UnitSelectable selectable))
        {
            ToggleSelection(selectable);
            return;
        }

        if (!TryGetPointerCell(
                out Vector3Int pointerCell))
        {
            return;
        }

        // 정지 중인 타일 오브젝트 판정
        if (occupancyManager.TryGetOccupant(
                pointerCell,
                out TileObjectPlacement occupant))
        {
            TryToggleUnit(occupant);
            return;
        }

        ClearSelection();
    }

    private void OnMoveCommand(
        InputAction.CallbackContext context)
    {
        if (placementController != null &&
            placementController.CurrentMode !=
                PlacementMode.None)
        {
            return;
        }

        if (!TryGetPointerCell(
                out Vector3Int pointerCell))
        {
            return;
        }

        if (occupancyManager.TryGetOccupant(
                pointerCell,
                out TileObjectPlacement occupant))
        {
            if (occupant.ObjectType ==
                    TileObjectType.Unit &&
                occupant.TryGetComponent(
                    out UnitCore clickedUnit) &&
                clickedUnit.Data != null &&
                clickedUnit.Data.Team ==
                    UnitTeam.Enemy)
            {
                TryIssueCombatCommand();
                return;
            }

            if (occupant.ObjectType ==
                    TileObjectType.Facility &&
                occupant.TryGetComponent(
                    out FactoryCore factory))
            {
                TryIssueFactoryCommand(factory);
            }

            return;
        }

        TryIssueMoveCommand(pointerCell);
    }

    private void TryIssueCombatCommand()
    {
        if (selectedUnits.Count == 0 ||
            destinationAssigner == null)
        {
            return;
        }

        destinationAssigner
            .IssueCombatCommand(
                selectedUnits);
    }

    private void TryToggleUnit(
        TileObjectPlacement occupant)
    {
        if (occupant == null ||
            occupant.ObjectType !=
                TileObjectType.Unit)
        {
            return;
        }

        if (!occupant.TryGetComponent(
                out UnitSelectable selectable))
        {
            return;
        }

        ToggleSelection(selectable);
    }

    private void TryIssueMoveCommand(
        Vector3Int destinationCell)
    {
        if (selectedUnits.Count == 0 ||
            destinationAssigner == null)
        {
            return;
        }

        destinationAssigner.IssueMoveCommand(
            selectedUnits,
            destinationCell);
    }

    private void TryIssueFactoryCommand(
        FactoryCore factory)
    {
        if (selectedUnits.Count == 0 ||
            destinationAssigner == null ||
            factory == null)
        {
            return;
        }

        destinationAssigner.IssueFactoryCommand(
            selectedUnits,
            factory);
    }

    private bool TryGetPointerCell(
        out Vector3Int cell)
    {
        cell = default;

        if (worldCamera == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager ==
                null ||
            pointerPositionAction == null)
        {
            return false;
        }

        Vector2 screenPosition =
            pointerPositionAction.action
                .ReadValue<Vector2>();

        float cameraDistance =
            Mathf.Abs(
                worldCamera.transform.position.z);

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    cameraDistance));

        worldPosition.z = 0f;

        cell =
            occupancyManager.CoordinateManager
                .WorldToCell(worldPosition);

        return occupancyManager
            .CoordinateManager
            .HasTile(cell);
    }

    private bool TryGetUnitUnderPointer(
        out UnitSelectable selectable)
    {
        selectable = null;

        if (worldCamera == null ||
            pointerPositionAction == null)
        {
            return false;
        }

        Vector2 screenPosition =
            pointerPositionAction.action
                .ReadValue<Vector2>();

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                screenPosition);

        Collider2D hit =
            Physics2D.OverlapPoint(
                worldPosition);

        if (hit == null)
        {
            return false;
        }

        selectable =
            hit.GetComponentInParent<
                UnitSelectable>();

        return selectable != null;
    }

    private void ToggleSelection(
        UnitSelectable unit)
    {
        if (unit == null ||
            !unit.CanSelect())
        {
            return;
        }

        if (unit.IsSelected)
        {
            RemoveFromSelection(unit);
        }
        else
        {
            AddToSelection(unit);
        }
    }

    private void AddToSelection(
        UnitSelectable unit)
    {
        if (unit == null ||
            selectedUnits.Contains(unit) ||
            !unit.CanSelect())
        {
            return;
        }

        selectedUnits.Add(unit);
        unit.Select();
    }

    private void RemoveFromSelection(
        UnitSelectable unit)
    {
        if (unit == null ||
            !selectedUnits.Remove(unit))
        {
            return;
        }

        unit.Deselect();
    }

    private void SelectUnitsInScreenRect(
        Vector2 startScreen,
        Vector2 endScreen)
    {
        if (worldCamera == null)
        {
            return;
        }

        float minX =
            Mathf.Min(
                startScreen.x,
                endScreen.x);

        float maxX =
            Mathf.Max(
                startScreen.x,
                endScreen.x);

        float minY =
            Mathf.Min(
                startScreen.y,
                endScreen.y);

        float maxY =
            Mathf.Max(
                startScreen.y,
                endScreen.y);

        Rect selectionRect =
            Rect.MinMaxRect(
                minX,
                minY,
                maxX,
                maxY);

        UnitSelectable[] units =
            FindObjectsByType<UnitSelectable>(
                FindObjectsSortMode.None);

        foreach (UnitSelectable unit in units)
        {
            if (unit == null ||
                !unit.CanSelect())
            {
                continue;
            }

            Vector3 screenPosition =
                worldCamera.WorldToScreenPoint(
                    unit.transform.position);

            if (screenPosition.z < 0f)
            {
                continue;
            }

            if (!selectionRect.Contains(
                    screenPosition))
            {
                continue;
            }

            AddToSelection(unit);
        }
    }

    private void UpdateSelectionBox(
        Vector2 startScreen,
        Vector2 endScreen)
    {
        if (selectionBox == null ||
            selectionCanvas == null)
        {
            return;
        }

        RectTransform canvasRect =
            selectionCanvas.transform
                as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        Camera canvasCamera =
            selectionCanvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                ? null
                : selectionCanvas.worldCamera;

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    startScreen,
                    canvasCamera,
                    out Vector2 startLocal) ||
            !RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    endScreen,
                    canvasCamera,
                    out Vector2 endLocal))
        {
            return;
        }

        Vector2 min =
            Vector2.Min(
                startLocal,
                endLocal);

        Vector2 max =
            Vector2.Max(
                startLocal,
                endLocal);

        selectionBox.gameObject
            .SetActive(true);

        selectionBox.anchoredPosition =
            min;

        selectionBox.sizeDelta =
            max - min;
    }

    private void HideSelectionBox()
    {
        if (selectionBox != null)
        {
            selectionBox.gameObject
                .SetActive(false);
        }
    }

    private void CancelPrimaryDrag()
    {
        isPrimaryHeld = false;

        HideSelectionBox();
    }

    public void ClearSelection()
    {
        for (int i =
                selectedUnits.Count - 1;
            i >= 0;
            i--)
        {
            UnitSelectable unit =
                selectedUnits[i];

            if (unit != null)
            {
                unit.Deselect();
            }
        }

        selectedUnits.Clear();
    }
}