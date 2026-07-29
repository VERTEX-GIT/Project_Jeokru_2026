using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 포인터 입력으로 유닛 선택을 전환하고 현재 선택 목록을 관리
[DisallowMultipleComponent]
public sealed class UnitSelectionController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference pointerPositionAction; // 마우스 위치
    [SerializeField]
    private InputActionReference primaryClickAction; // 선택 입력
    [SerializeField]
    private InputActionReference cancelSelectionAction; // 선택 해제 입력

    [Header("References")]
    [SerializeField]
    private Camera worldCamera;
    [SerializeField]
    private TileOccupancyManager occupancyManager;
    [SerializeField]
    private ObjectPlacementController placementController;

    private readonly List<UnitSelectable> selectedUnits = new();

    // 외부에서는 선택 목록을 읽기 전용으로 제공
    public IReadOnlyList<UnitSelectable> SelectedUnits => selectedUnits;

    // Inspector에서 연결되지 않은 카메라와 점유 관리자 탐색
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

        if (placementController != null &&
            placementController.CurrentMode != PlacementMode.None)
        {
            return;
        }
    }

    // 선택 관련 입력 액션을 활성화하고 콜백 등록
    private void OnEnable()
    {
        if (primaryClickAction != null)
        {
            primaryClickAction.action.performed += OnPrimaryClick;
            primaryClickAction.action.Enable();
        }

        if (cancelSelectionAction != null)
        {
            cancelSelectionAction.action.performed += OnCancelSelection;
            cancelSelectionAction.action.Enable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Enable();
        }
    }

    // 입력 콜백을 해제하고 모든 유닛 선택 해제
    private void OnDisable()
    {
        if (primaryClickAction != null)
        {
            primaryClickAction.action.performed -= OnPrimaryClick;
            primaryClickAction.action.Disable();
        }

        if (cancelSelectionAction != null)
        {
            cancelSelectionAction.action.performed -= OnCancelSelection;
            cancelSelectionAction.action.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Disable();
        }

        ClearSelection();
    }

    private void OnPrimaryClick(
        InputAction.CallbackContext context)
    {
        // 유닛 또는 공장 배치 모드에서는
        // 같은 좌클릭 입력으로 유닛을 선택하지 않는다.
        if (placementController != null &&
            placementController.CurrentMode != PlacementMode.None)
        {
            return;
        }

        TryToggleUnitAtPointer();
    }

    private void OnCancelSelection(
        InputAction.CallbackContext context)
    {
        // 배치 모드에서의 우클릭 처리는 현재 없지만,
        // 선택 해제를 막고 싶다면 여기에도 같은 검사를 넣으면 된다.
        ClearSelection();
    }

    // 포인터가 가리키는 유닛의 선택 상태 전환
    private void TryToggleUnitAtPointer()
    {
        if (!TryGetPointerCell(out Vector3Int pointerCell))
        {
            return;
        }

        if (!occupancyManager.TryGetOccupant(
                pointerCell,
                out TileObjectPlacement occupant))
        {
            return;
        }

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
