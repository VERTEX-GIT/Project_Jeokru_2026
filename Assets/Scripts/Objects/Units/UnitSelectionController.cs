using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 포인터 입력으로 유닛 선택을 관리하고 선택된 유닛에 이동 명령을 전달
[DisallowMultipleComponent]
public sealed class UnitSelectionController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference pointerPositionAction; // 마우스 위치
    [SerializeField]
    private InputActionReference primaryClickAction; // 선택 입력
    [SerializeField]
    private InputActionReference moveCommandAction; // 이동 명령 입력

    [Header("References")]
    [SerializeField]
    private Camera worldCamera;
    [SerializeField]
    private TileOccupancyManager occupancyManager;
    [SerializeField]
    private ObjectPlacementController placementController;
    [SerializeField]
    private UnitDestinationAssigner destinationAssigner;

    private readonly List<UnitSelectable> selectedUnits = new();

    // Inspector 참조 또는 동일 액션 맵에서 찾은 실제 이동 명령 액션
    private InputAction moveCommandInput;
    // 동일 액션 맵에서 찾은 공장 수리 액션
    private InputAction factoryRepairInput;
    private bool isFactoryRepairMode;

    // 외부에서는 선택 목록을 읽기 전용으로 제공
    public IReadOnlyList<UnitSelectable> SelectedUnits => selectedUnits;

    // 누락된 씬 참조를 탐색하고 사용할 이동 명령 액션 결정
    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (occupancyManager == null)
        {
            occupancyManager = TileOccupancyManager.Instance;
        }

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<TileOccupancyManager>();
        }

        if (placementController == null)
        {
            placementController =
                FindAnyObjectByType<ObjectPlacementController>();
        }
        if (destinationAssigner == null)
        {
            destinationAssigner =
                FindAnyObjectByType<UnitDestinationAssigner>();
        }

        moveCommandInput = moveCommandAction != null
            ? moveCommandAction.action
            : primaryClickAction?.action.actionMap?.FindAction("MoveCommand");

        factoryRepairInput =
            primaryClickAction?.action.actionMap?.FindAction("FactoryRepair");
    }

    // 선택 및 이동 관련 입력 액션을 활성화하고 콜백 등록
    private void OnEnable()
    {
        if (primaryClickAction != null)
        {
            primaryClickAction.action.performed += OnPrimaryClick;
            primaryClickAction.action.Enable();
        }

        if (moveCommandInput != null)
        {
            moveCommandInput.performed += OnMoveCommand;
            moveCommandInput.Enable();
        }

        if (factoryRepairInput != null)
        {
            factoryRepairInput.performed += OnFactoryRepair;
            factoryRepairInput.Enable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Enable();
        }
    }

    // 선택 및 이동 입력 콜백을 해제하고 모든 유닛 선택 해제
    private void OnDisable()
    {
        if (primaryClickAction != null)
        {
            primaryClickAction.action.performed -= OnPrimaryClick;
            primaryClickAction.action.Disable();
        }

        if (moveCommandInput != null)
        {
            moveCommandInput.performed -= OnMoveCommand;
            moveCommandInput.Disable();
        }

        if (factoryRepairInput != null)
        {
            factoryRepairInput.performed -= OnFactoryRepair;
            factoryRepairInput.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Disable();
        }

        isFactoryRepairMode = false;
        ClearSelection();
    }

    // 배치 모드가 아닐 때 클릭한 유닛의 선택을 전환하고 빈 타일 클릭 시 전체 선택 해제
    private void OnPrimaryClick(
        InputAction.CallbackContext context)
    {
        if (placementController != null &&
            placementController.CurrentMode !=
                PlacementMode.None)
        {
            return;
        }

        if (isFactoryRepairMode)
        {
            if (TryRepairFactoryUnderPointer())
            {
                isFactoryRepairMode = false;
            }

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

    // 배치 모드가 아닐 때 우클릭 위치에 따라
    // 일반 이동 또는 공장 작업 명령 전달
    private void OnMoveCommand(
        InputAction.CallbackContext context)
    {
        if (isFactoryRepairMode ||
            placementController != null &&
            placementController.CurrentMode != PlacementMode.None)
        {
            return;
        }

        if (!TryGetPointerCell(
                out Vector3Int pointerCell))
        {
            return;
        }

        // 오브젝트를 우클릭한 경우
        if (occupancyManager.TryGetOccupant(
                pointerCell,
                out TileObjectPlacement occupant))
        {
            // 적군을 클릭했다면 전투 명령
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

            // 공장이면 공장 작업 명령
            if (occupant.ObjectType ==
                    TileObjectType.Facility &&
                occupant.TryGetComponent(
                    out FactoryCore factory))
            {
                TryIssueFactoryCommand(factory);
            }

            return;
        }

        // 빈 타일이면 일반 이동
        TryIssueMoveCommand(pointerCell);
    }

    // 공장 수리 모드 시작 또는 취소
    private void OnFactoryRepair(
        InputAction.CallbackContext context)
    {
        if (placementController != null &&
            placementController.CurrentMode != PlacementMode.None)
        {
            return;
        }

        isFactoryRepairMode = !isFactoryRepairMode;

        Debug.Log(
            isFactoryRepairMode
                ? "공장 수리 모드: 수리할 공장을 클릭하세요."
                : "공장 수리 모드를 취소했습니다.",
            this);
    }

    // 포인터가 가리키는 공장 하나를 수리
    private bool TryRepairFactoryUnderPointer()
    {
        if (!TryGetPointerCell(out Vector3Int pointerCell) ||
            !occupancyManager.TryGetOccupant(
                pointerCell,
                out TileObjectPlacement occupant) ||
            occupant.ObjectType != TileObjectType.Facility ||
            !occupant.TryGetComponent(out FactoryRepair factoryRepair))
        {
            return false;
        }

        FactoryRepairResult result = factoryRepair.TryRepair();

        Debug.Log(
            $"{factoryRepair.name}: 공장 수리 결과 - {result}",
            factoryRepair);

        return true;
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

    // 포인터가 가리키는 유닛의 선택 상태 전환
    private void TryToggleUnit(
        TileObjectPlacement occupant)
    {
        if (occupant == null ||
            occupant.ObjectType != TileObjectType.Unit)
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

    // 선택된 유닛들에게 이동 명령을 발행
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

    // 선택된 유닛들에게 공장 작업 이동 명령 전달
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

    // 화면 포인터 좌표를 맵 안의 타일 셀 좌표로 변환
    private bool TryGetPointerCell(out Vector3Int cell)
    {
        cell = default;

        if (worldCamera == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager == null ||
            pointerPositionAction == null)
        {
            return false;
        }

        Vector2 screenPosition =
            pointerPositionAction.action.ReadValue<Vector2>();

        float cameraDistance =
            Mathf.Abs(worldCamera.transform.position.z);

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    cameraDistance));

        worldPosition.z = 0f;

        cell = occupancyManager.CoordinateManager
            .WorldToCell(worldPosition);

        return occupancyManager.CoordinateManager
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
            hit.GetComponentInParent<UnitSelectable>();

        return selectable != null;
    }

    // 유닛의 현재 상태에 따라 선택 또는 선택 해제
    private void ToggleSelection(UnitSelectable unit)
    {
        if (unit == null || !unit.CanSelect())
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

    // 중복되지 않은 유닛을 선택 목록에 추가
    private void AddToSelection(UnitSelectable unit)
    {
        if (unit == null ||
            selectedUnits.Contains(unit))
        {
            return;
        }

        selectedUnits.Add(unit);
        unit.Select();
    }

    // 유닛을 선택 목록에서 제거하고 선택 상태 해제
    private void RemoveFromSelection(UnitSelectable unit)
    {
        if (unit == null ||
            !selectedUnits.Remove(unit))
        {
            return;
        }

        unit.Deselect();
    }

    // 현재 선택된 모든 유닛의 상태와 목록 초기화
    public void ClearSelection()
    {
        for (int i = selectedUnits.Count - 1; i >= 0; i--)
        {
            UnitSelectable unit = selectedUnits[i];

            if (unit != null)
            {
                unit.Deselect();
            }
        }

        selectedUnits.Clear();
    }
}
