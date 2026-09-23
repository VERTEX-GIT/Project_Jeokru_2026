using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 공장 배치 모드의 입력, 미리보기, 실제 배치를 제어
[DisallowMultipleComponent]
public sealed class ObjectPlacementController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference pointerPositionAction; // 마우스 화면 좌표

    [SerializeField]
    private InputActionReference primaryClickAction; // 좌클릭 배치

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
    private PlacementObjectProvider objectProvider; // 실제 공장 프리팹 생성

    private int consumedInputFrame = -1;
    private readonly List<RaycastResult> uiHits = new();

    public bool InputConsumedThisFrame =>
        consumedInputFrame == Time.frameCount;

    public bool BlocksWorldInput =>
        CurrentMode != PlacementMode.None ||
        InputConsumedThisFrame;

    // 현재 배치 상태와 미리보기에서 사용하는 기준 좌표
    public PlacementMode CurrentMode
    {
        get;
        private set;
    }

    public Vector3Int CurrentAnchorCell
    {
        get;
        private set;
    }

    public bool CurrentPlacementValid
    {
        get;
        private set;
    }

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

        EndPlacementMode();
    }

    // 배치 모드 동안 포인터 위치에 맞춰 미리보기 갱신
    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            if (CurrentMode != PlacementMode.None)
            {
                EndPlacementMode();
            }

            return;
        }

        if (CurrentMode == PlacementMode.None)
        {
            return;
        }

        if ((Mouse.current != null &&
             Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null &&
             Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        RefreshPlacementPreview();
    }

    public void BeginFactoryPlacement(
        TileObjectPlacement prefab)
    {
        if (!isActiveAndEnabled ||
            PauseMenu.IsPaused)
        {
            return;
        }

        if (objectProvider == null ||
            !objectProvider.SelectFactory(prefab))
        {
            return;
        }

        MedicineUseController.Instance?
            .CancelMedicineSelection();

        consumedInputFrame =
            Time.frameCount;

        CurrentMode =
            PlacementMode.Factory;

        RefreshPlacementPreview();
    }

    public bool CancelPlacement()
    {
        if (CurrentMode ==
            PlacementMode.None)
        {
            return false;
        }

        EndPlacementMode();
        return true;
    }

    private void OnPrimaryClick(
        InputAction.CallbackContext context)
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        TryPlaceCurrentObject();
    }

    // 현재 포인터 위치의 배치 가능 여부와 미리보기 상태 갱신
    private void RefreshPlacementPreview()
    {
        if (CurrentMode !=
            PlacementMode.Factory)
        {
            HidePreview();
            return;
        }

        if (!TryGetPointerCell(
                out Vector3Int pointerCell))
        {
            HidePreview();
            return;
        }

        if (!TryGetNearestFactoryAnchor(
                pointerCell,
                out Vector3Int resolvedAnchor))
        {
            HidePreview();
            return;
        }

        CurrentAnchorCell =
            resolvedAnchor;

        CurrentPlacementValid =
            placementValidator != null &&
            placementValidator.CanPlaceFactory(
                CurrentAnchorCell) &&
            CanAffordFactory(false);

        placementPreview?.ShowFactory(
            CurrentAnchorCell,
            CurrentPlacementValid);
    }

    // 화면 포인터 좌표를 실제 타일 셀 좌표로 변환
    private bool TryGetPointerCell(
        out Vector3Int cell)
    {
        cell = default;

        if (worldCamera == null ||
            coordinateManager == null ||
            pointerPositionAction == null)
        {
            return false;
        }

        Vector2 screenPosition =
            pointerPositionAction.action
                .ReadValue<Vector2>();

        if (!worldCamera.pixelRect.Contains(
                screenPosition))
        {
            return false;
        }

        if (IsPointerOverUI())
        {
            return false;
        }

        float cameraDistance =
            Mathf.Abs(
                worldCamera.transform
                    .position.z);

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    cameraDistance));

        worldPosition.z = 0f;

        cell =
            coordinateManager.WorldToCell(
                worldPosition);

        return coordinateManager.HasTile(
            cell);
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
        int nearestDistanceSquared =
            int.MaxValue;

        for (int offsetX = -2;
             offsetX <= 2;
             offsetX++)
        {
            for (int offsetY = -2;
                 offsetY <= 2;
                 offsetY++)
            {
                Vector3Int candidate =
                    pointerCell +
                    new Vector3Int(
                        offsetX,
                        offsetY,
                        0);

                if (!placementValidator
                        .IsFactoryAreaInsideMap(
                            candidate))
                {
                    continue;
                }

                int distanceSquared =
                    offsetX * offsetX +
                    offsetY * offsetY;

                if (distanceSquared >=
                    nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared =
                    distanceSquared;

                anchorCell =
                    candidate;

                found = true;
            }
        }

        return found;
    }

    // 현재 선택된 공장을 검증된 앵커에 배치
    private void TryPlaceCurrentObject()
    {
        if (PauseMenu.IsPaused ||
            InputConsumedThisFrame ||
            IsPointerOverUI())
        {
            return;
        }

        if (CurrentMode !=
                PlacementMode.Factory ||
            objectProvider == null)
        {
            return;
        }

        if ((Mouse.current != null &&
             Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null &&
             Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        // 클릭 시점의 좌표·점유 상태·보유량으로 다시 검사한다.
        RefreshPlacementPreview();

        if (!CurrentPlacementValid)
        {
            CanAffordFactory(true);
            return;
        }

        TryPlaceAt(
            CurrentAnchorCell);
    }

    private bool TryPlaceAt(
        Vector3Int cell)
    {
        if (PauseMenu.IsPaused ||
            objectProvider == null ||
            placementValidator == null ||
            CurrentMode !=
                PlacementMode.Factory)
        {
            return false;
        }

        if (!placementValidator
                .CanPlaceFactory(cell) ||
            !CanAffordFactory(true))
        {
            return false;
        }

        IReadOnlyList<ResourceCost> costs =
            objectProvider
                .FactoryDefinition
                .InstallationCosts;

        ResourceInventory inventory =
            ResourceInventory.Inventory;

        TileObjectPlacement
            createdPlacement =
                objectProvider.Create(
                    PlacementMode.Factory);

        if (createdPlacement == null)
        {
            return false;
        }

        if (!createdPlacement.TryPlace(
                cell))
        {
            Debug.LogWarning(
                $"{createdPlacement.name}: " +
                $"{cell} 타일 배치 실패",
                createdPlacement);

            Destroy(
                createdPlacement.gameObject);

            return false;
        }

        if (costs.Count > 0 &&
            (inventory == null ||
             !inventory.Spend(costs)))
        {
            createdPlacement
                .RemoveFromTiles();

            createdPlacement
                .gameObject
                .SetActive(false);

            Destroy(
                createdPlacement.gameObject);

            RefreshPlacementPreview();
            return false;
        }

        EndPlacementMode();
        return true;
    }

    private bool CanAffordFactory(
        bool logFailure)
    {
        FactoryDefinition definition =
            objectProvider != null
                ? objectProvider
                    .FactoryDefinition
                : null;

        if (definition == null)
        {
            return false;
        }

        IReadOnlyList<ResourceCost> costs =
            definition.InstallationCosts;

        if (costs.Count == 0)
        {
            return true;
        }

        ResourceInventory inventory =
            ResourceInventory.Inventory;

        return inventory != null &&
               inventory.CanAfford(
                   costs,
                   logFailure);
    }

    // InputAction 콜백에서도 이번 포인터 위치를 사용한다.
    public bool IsPointerOverUI()
    {
        if (EventSystem.current == null ||
            pointerPositionAction == null)
        {
            return false;
        }

        PointerEventData pointer =
            new(EventSystem.current)
            {
                position =
                    pointerPositionAction
                        .action
                        .ReadValue<Vector2>()
            };

        uiHits.Clear();

        EventSystem.current.RaycastAll(
            pointer,
            uiHits);

        foreach (RaycastResult hit
                 in uiHits)
        {
            if (hit.module is
                GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    // 배치 상태를 초기화하고 미리보기 숨김
    private void EndPlacementMode()
    {
        if (CurrentMode !=
            PlacementMode.None)
        {
            consumedInputFrame =
                Time.frameCount;
        }

        CurrentMode =
            PlacementMode.None;

        HidePreview();
    }

    private void HidePreview()
    {
        CurrentPlacementValid =
            false;

        placementPreview?.Hide();
    }

    // null인 InputActionReference를 안전하게 처리하는 활성화 도우미
    private static void EnableAction(
        InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference
                .action
                .Enable();
        }
    }

    // null인 InputActionReference를 안전하게 처리하는 비활성화 도우미
    private static void DisableAction(
        InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference
                .action
                .Disable();
        }
    }
}
