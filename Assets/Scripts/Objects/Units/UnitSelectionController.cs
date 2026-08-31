using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 포인터 입력으로 유닛 선택을 관리하고
// 선택된 유닛에 이동/전투/작업/전위 배치 명령을 전달
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

    [SerializeField]
    private UnitFrontlinePlanner frontlinePlanner;

    [Header("Selection Drag")]
    [SerializeField]
    private RectTransform selectionBox;

    [SerializeField]
    private Canvas selectionCanvas;

    [SerializeField]
    [Min(0f)]
    private float dragThreshold = 8f;

    private readonly List<UnitSelectable>
        selectedUnits = new();

    private InputAction moveCommandInput;

    // 좌클릭 드래그 선택
    private Vector2 dragStartScreenPosition;
    private bool isPrimaryHeld;

    // 우클릭 전위 배치
    private Vector2 moveDragStartScreenPosition;
    private Vector3Int moveDragStartCell;

    private bool isMoveHeld;
    private bool canStartFrontline;
    private bool isFrontlineDrag;

    public IReadOnlyList<UnitSelectable>
        SelectedUnits =>
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

        if (frontlinePlanner == null)
        {
            frontlinePlanner =
                FindAnyObjectByType<
                    UnitFrontlinePlanner>();
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
            moveCommandInput.started +=
                OnMovePress;

            moveCommandInput.canceled +=
                OnMoveRelease;

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
            moveCommandInput.started -=
                OnMovePress;

            moveCommandInput.canceled -=
                OnMoveRelease;

            moveCommandInput.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Disable();
        }

        CancelPrimaryDrag();
        CancelMoveDrag();

        ClearSelection();
    }

    private void Update()
    {
        UpdatePrimaryDrag();
        UpdateMoveDrag();
    }

    // =========================
    // 좌클릭 선택
    // =========================

    private void OnPrimaryPress(
        InputAction.CallbackContext context)
    {
        if (IsPlacementModeActive())
        {
            return;
        }

        if (pointerPositionAction == null)
        {
            return;
        }

        dragStartScreenPosition =
            GetPointerScreenPosition();

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
            GetPointerScreenPosition();

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

    private void UpdatePrimaryDrag()
    {
        if (!isPrimaryHeld ||
            pointerPositionAction == null)
        {
            return;
        }

        Vector2 currentPosition =
            GetPointerScreenPosition();

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

    private void HandlePrimaryClick()
    {
        if (IsPlacementModeActive())
        {
            return;
        }

        // 이동 중인 유닛도 실제 Collider로 우선 선택
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

        if (occupancyManager.TryGetOccupant(
                pointerCell,
                out TileObjectPlacement occupant))
        {
            TryToggleUnit(occupant);
            return;
        }

        ClearSelection();
    }

    // =========================
    // 우클릭 명령 / 전위 배치
    // =========================

    private void OnMovePress(
        InputAction.CallbackContext context)
    {
        if (IsPlacementModeActive() ||
            pointerPositionAction == null)
        {
            return;
        }

        moveDragStartScreenPosition =
            GetPointerScreenPosition();

        isMoveHeld = true;
        isFrontlineDrag = false;
        canStartFrontline = false;

        if (selectedUnits.Count == 0 ||
            frontlinePlanner == null)
        {
            return;
        }

        if (!TryGetPointerCell(
                out moveDragStartCell))
        {
            return;
        }

        // 전위 배치는 빈 타일에서 시작할 때만 가능.
        // 적/공장을 우클릭한 경우 기존 명령을 유지한다.
        if (occupancyManager.TryGetOccupant(
                moveDragStartCell,
                out _))
        {
            return;
        }

        canStartFrontline = true;
    }

    private void UpdateMoveDrag()
    {
        if (!isMoveHeld ||
            !canStartFrontline ||
            IsPlacementModeActive() ||
            pointerPositionAction == null ||
            frontlinePlanner == null)
        {
            return;
        }

        Vector2 currentScreenPosition =
            GetPointerScreenPosition();

        float dragDistance =
            Vector2.Distance(
                moveDragStartScreenPosition,
                currentScreenPosition);

        if (dragDistance < dragThreshold)
        {
            return;
        }

        if (!isFrontlineDrag)
        {
            if (!frontlinePlanner.BeginFrontline(
                    moveDragStartCell))
            {
                canStartFrontline = false;
                return;
            }

            isFrontlineDrag = true;
        }

        if (TryGetPointerCell(
                out Vector3Int currentCell))
        {
            frontlinePlanner.UpdateFrontline(
                currentCell);
        }
    }

    private void OnMoveRelease(
        InputAction.CallbackContext context)
    {
        if (!isMoveHeld)
        {
            return;
        }

        isMoveHeld = false;

        if (isFrontlineDrag)
        {
            // 마지막 프레임에서 포인터가 이동했을 수도 있으므로
            // release 위치까지 한 번 더 반영
            if (TryGetPointerCell(
                    out Vector3Int endCell))
            {
                frontlinePlanner.UpdateFrontline(
                    endCell);
            }

            frontlinePlanner.CompleteFrontline(
                selectedUnits);

            isFrontlineDrag = false;
            canStartFrontline = false;

            return;
        }

        canStartFrontline = false;

        // 드래그가 아니라면 기존 우클릭 명령
        HandleMoveCommand();
    }

    private void HandleMoveCommand()
    {
        if (IsPlacementModeActive())
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
            // 적군 우클릭
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

            // 공장 우클릭
            if (occupant.ObjectType ==
                    TileObjectType.Facility &&
                occupant.TryGetComponent(
                    out FactoryCore factory))
            {
                TryIssueFactoryCommand(
                    factory);

                return;
            }

            return;
        }

        // 빈 타일 우클릭
        TryIssueMoveCommand(
            pointerCell);
    }

    // =========================
    // 명령 전달
    // =========================

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

    // =========================
    // 포인터 / 타일
    // =========================

    private Vector2 GetPointerScreenPosition()
    {
        return pointerPositionAction.action
            .ReadValue<Vector2>();
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
            GetPointerScreenPosition();

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
            occupancyManager
                .CoordinateManager
                .WorldToCell(
                    worldPosition);

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
            GetPointerScreenPosition();

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

    // =========================
    // 선택 관리
    // =========================

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

        ToggleSelection(
            selectable);
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
            FindObjectsByType<
                UnitSelectable>(
                FindObjectsSortMode.None);

        foreach (UnitSelectable unit
                in units)
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

    // =========================
    // 선택 박스 UI
    // =========================

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

    // =========================
    // 입력 취소
    // =========================

    private void CancelPrimaryDrag()
    {
        isPrimaryHeld = false;

        HideSelectionBox();
    }

    private void CancelMoveDrag()
    {
        isMoveHeld = false;
        canStartFrontline = false;
        isFrontlineDrag = false;

        if (frontlinePlanner != null)
        {
            frontlinePlanner
                .CancelFrontline();
        }
    }

    private bool IsPlacementModeActive()
    {
        return placementController != null &&
            placementController.CurrentMode !=
                PlacementMode.None;
    }
}