using UnityEngine;

// 씬에 미리 배치된 오브젝트를 시작 시 타일 점유 정보에 등록
[DisallowMultipleComponent]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class SceneTilePlacementInitializer : MonoBehaviour
{
    [SerializeField]
    private bool placeOnStart = true; // 시작 시 자동 등록 여부

    private TileObjectPlacement placement;

    // 같은 오브젝트의 타일 배치 컴포넌트 참조 저장
    private void Awake()
    {
        placement = GetComponent<TileObjectPlacement>();
    }

    // 현재 월드 위치를 셀 좌표로 변환해 최초 배치 등록
    private void Start()
    {
        if (!placeOnStart || placement.IsPlaced)
        {
            return;
        }

        TileOccupancyManager occupancyManager =
            TileOccupancyManager.Instance;

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<TileOccupancyManager>();
        }

        if (occupancyManager == null ||
            occupancyManager.CoordinateManager == null)
        {
            Debug.LogError(
                $"{name}: 타일 점유 또는 좌표 관리자를 찾을 수 없습니다.",
                this);

            return;
        }

        TileCoordinateManager coordinateManager =
            occupancyManager.CoordinateManager;

        Vector3Int currentCell =
            coordinateManager.WorldToCell(transform.position);

        if (!coordinateManager.HasTile(currentCell))
        {
            Debug.LogError(
                $"{name}: {currentCell} 좌표에 실제 타일이 없습니다.",
                this);

            return;
        }

        if (!placement.TryPlace(currentCell))
        {
            Debug.LogError(
                $"{name}: {currentCell} 타일에 초기 배치하지 못했습니다.",
                this);
        }
    }
}
