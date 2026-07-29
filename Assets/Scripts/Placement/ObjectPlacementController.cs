using UnityEngine;
using UnityEngine.InputSystem;

// 입력에 따라 배치 모드를 전환하고 미리보기와 실제 오브젝트 배치를 제어
[DisallowMultipleComponent]
public sealed class ObjectPlacementController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference pointerPositionAction; // 마우스 화면 좌표
    [SerializeField]
    private InputActionReference primaryClickAction; // 좌클릭 배치
    [SerializeField]
    private InputActionReference unitPlacementModeAction; // 유닛 배치 모드
    [SerializeField]
    private InputActionReference factoryPlacementModeAction; // 공장 배치 모드

    [Header("References")]
    [SerializeField]
    private Camera worldCamera; // 화면 좌표를 월드 좌표로 변환
    [SerializeField]
    private TileCoordinateManager coordinateManager; // 월드 좌표를 타일 셀 좌표로 변환
    [SerializeField]
    private PlacementPreview placementPreview; // 타일 위에 배치 미리보기 표시
    [SerializeField]
    private PlacementValidator placementValidator; // 해당 위치에 배치 가능한지 검사
    [SerializeField]
    private PlacementObjectProvider objectProvider; // 실제 유닛·공장 프리팹 생성

    // 현재 배치 상태와 미리보기에서 사용하는 기준 좌표
    public PlacementMode CurrentMode { get; private set; }
    public Vector3Int CurrentAnchorCell { get; private set; }
    public bool CurrentPlacementValid { get; private set; }

    // Inspector에서 연결되지 않은 필수 참조를 자동 탐색
    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (placementPreview == null)
        {
            placementPreview =
                GetComponent<PlacementPreview>();
        }

        if (placementValidator == null)
        {
            placementValidator =
                GetComponent<PlacementValidator>();
        }

        if (objectProvider == null)
        {
            objectProvider =
                GetComponent<PlacementObjectProvider>();
        }
    }

    // 입력 액션을 활성화하고 콜백 등록
    private void OnEnable()
    {
        EnableAction(pointerPositionAction);

        if (primaryClickAction != null)
        {
            primaryClickAction.action.performed +=
                OnPrimaryClick;

            primaryClickAction.action.Enable();
        }

        if (unitPlacementModeAction != null)
        {
            unitPlacementModeAction.action.started +=
                OnUnitPlacementStarted;

            unitPlacementModeAction.action.canceled +=
                OnUnitPlacementCanceled;

            unitPlacementModeAction.action.Enable();
        }

        if (factoryPlacementModeAction != null)
        {
            factoryPlacementModeAction.action.started +=
                OnFactoryPlacementStarted;

            factoryPlacementModeAction.action.canceled +=
                OnFactoryPlacementCanceled;

            factoryPlacementModeAction.action.Enable();
        }
    }

    // 입력 콜백을 해제하고 진행 중인 배치 모드 종료
    private void OnDisable()
    {
        DisableAction(pointerPositionAction);

        if (primaryClickAction != null)
        {
            primaryClickAction.action.performed -=
                OnPrimaryClick;

            primaryClickAction.action.Disable();
        }

        if (unitPlacementModeAction != null)
        {
            unitPlacementModeAction.action.started -=
                OnUnitPlacementStarted;

            unitPlacementModeAction.action.canceled -=
                OnUnitPlacementCanceled;

            unitPlacementModeAction.action.Disable();
        }

        if (factoryPlacementModeAction != null)
        {
            factoryPlacementModeAction.action.started -=
                OnFactoryPlacementStarted;

            factoryPlacementModeAction.action.canceled -=
                OnFactoryPlacementCanceled;

            factoryPlacementModeAction.action.Disable();
        }

        EndPlacementMode();
    }

    // 배치 모드 동안 포인터 위치에 맞춰 미리보기 갱신
    private void Update()
    {
        if (CurrentMode == PlacementMode.None)
        {
            return;
        }

        RefreshPlacementPreview();
    }

    private void OnUnitPlacementStarted(
        InputAction.CallbackContext context)
    {
        CurrentMode = PlacementMode.Unit;
        RefreshPlacementPreview();
    }

    private void OnUnitPlacementCanceled(
        InputAction.CallbackContext context)
    {
        if (CurrentMode == PlacementMode.Unit)
        {
            EndPlacementMode();
        }
    }

    private void OnFactoryPlacementStarted(
        InputAction.CallbackContext context)
    {
        CurrentMode = PlacementMode.Factory;
        RefreshPlacementPreview();
    }

    private void OnFactoryPlacementCanceled(
        InputAction.CallbackContext context)
    {
        if (CurrentMode == PlacementMode.Factory)
        {
            EndPlacementMode();
        }
    }

    private void OnPrimaryClick(
        InputAction.CallbackContext context)
    {
        TryPlaceCurrentObject();
    }

    // 현재 포인터 위치의 배치 가능 여부와 미리보기 상태 갱신
    private void RefreshPlacementPreview()
    {
        if (!TryGetPointerCell(out Vector3Int pointerCell))
        {
            HidePreview();
            return;
        }

        Vector3Int resolvedAnchor = pointerCell;

        // 공장 배치 모드에서는 5×5 영역 전체가 맵 안인 앵커를 찾음
        if (CurrentMode == PlacementMode.Factory &&
            !TryGetNearestFactoryAnchor(
                pointerCell,
                out resolvedAnchor))
        {
            HidePreview();
            return;
        }

        CurrentAnchorCell = resolvedAnchor;

        switch (CurrentMode)
        {
            case PlacementMode.Unit:
                CurrentPlacementValid =
                    placementValidator != null &&
                    placementValidator.CanPlaceUnit(
                        CurrentAnchorCell);

                placementPreview?.ShowUnit(
                    CurrentAnchorCell,
                    CurrentPlacementValid);
                break;

            case PlacementMode.Factory:
                CurrentPlacementValid =
                    placementValidator != null &&
                    placementValidator.CanPlaceFactory(
                        CurrentAnchorCell);

                placementPreview?.ShowFactory(
                    CurrentAnchorCell,
                    CurrentPlacementValid);
                break;
        }
    }

    // 화면 포인터 좌표를 실제 타일 셀 좌표로 변환
    private bool TryGetPointerCell(out Vector3Int cell)
    {
        cell = default;

        if (worldCamera == null ||
            coordinateManager == null ||
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

        cell = coordinateManager.WorldToCell(worldPosition);

        // 마우스가 실제 맵 밖이면 미리보기 숨김
        return coordinateManager.HasTile(cell);
    }

    // 포인터와 가장 가까우면서 5×5 영역 전체가 맵 안인 공장 앵커 탐색
    private bool TryGetNearestFactoryAnchor(
        Vector3Int pointerCell,
        out Vector3Int anchorCell)
    {
        anchorCell = default;

        if (placementValidator == null)
        {
            return false;
        }

        bool found = false;
        int nearestDistanceSquared = int.MaxValue;

        for (int offsetX = -2; offsetX <= 2; offsetX++)
        {
            for (int offsetY = -2; offsetY <= 2; offsetY++)
            {
                Vector3Int candidate =
                    pointerCell +
                    new Vector3Int(offsetX, offsetY, 0);

                if (!placementValidator
                        .IsFactoryAreaInsideMap(candidate))
                {
                    continue;
                }

                int distanceSquared =
                    offsetX * offsetX +
                    offsetY * offsetY;

                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                anchorCell = candidate;
                found = true;
            }
        }

        return found;
    }

    // 현재 모드에 맞는 프리팹을 생성해 검증된 앵커에 배치
    private void TryPlaceCurrentObject()
    {
        if (CurrentMode == PlacementMode.None ||
            !CurrentPlacementValid ||
            objectProvider == null)
        {
            return;
        }

        TileObjectPlacement createdPlacement =
            objectProvider.Create(CurrentMode);

        if (createdPlacement == null)
        {
            return;
        }

        if (!createdPlacement.TryPlace(CurrentAnchorCell))
        {
            Debug.LogWarning(
                $"{createdPlacement.name}: " +
                $"{CurrentAnchorCell} 타일 배치 실패",
                createdPlacement);

            Destroy(createdPlacement.gameObject);
            return;
        }

        // 새 점유 상태를 즉시 미리보기에 반영
        RefreshPlacementPreview();
    }

    // 배치 상태를 초기화하고 미리보기 숨김
    private void EndPlacementMode()
    {
        CurrentMode = PlacementMode.None;
        HidePreview();
    }

    private void HidePreview()
    {
        CurrentPlacementValid = false;
        placementPreview?.Hide();
    }

    // null인 InputActionReference를 안전하게 처리하는 활성화 도우미
    private static void EnableAction(
        InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Enable();
        }
    }

    // null인 InputActionReference를 안전하게 처리하는 비활성화 도우미
    private static void DisableAction(
        InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Disable();
        }
    }
}
