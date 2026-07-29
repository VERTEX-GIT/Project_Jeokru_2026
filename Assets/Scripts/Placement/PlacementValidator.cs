using UnityEngine;

// 타일 점유 상태와 맵 범위를 기준으로 오브젝트 배치 가능 여부 검사
[DisallowMultipleComponent]
public sealed class PlacementValidator : MonoBehaviour
{
    [SerializeField]
    private TileOccupancyManager occupancyManager; // 해당 좌표에 실제 타일이 존재하는지 확인
    [SerializeField]
    private TileCoordinateManager coordinateManager; // 오브젝트 점유, 유닛 예약, 공장 작업 영역 확인

    // Inspector 참조가 없으면 씬의 점유 관리자와 좌표 관리자 탐색
    private void Awake()
    {
        if (occupancyManager == null)
        {
            occupancyManager =
                TileOccupancyManager.Instance;
        }

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<TileOccupancyManager>();
        }

        if (coordinateManager == null &&
            occupancyManager != null)
        {
            coordinateManager =
                occupancyManager.CoordinateManager;
        }
    }

    // 지정 셀에 유닛을 배치할 수 있는지 검사
    public bool CanPlaceUnit(Vector3Int cell)
    {
        if (coordinateManager == null ||
            occupancyManager == null)
        {
            return false;
        }

        if (!coordinateManager.HasTile(cell)) // 실제 맵 타일인지
        {
            return false;
        }

        if (occupancyManager.HasOccupant(cell)) // 다른 오브젝트가 점유하지 않았는지
        {
            return false;
        }

        if (occupancyManager.IsReserved(cell)) // 다른 유닛의 이동 목적지로 예약되지 않았는지
        {
            return false;
        }

        // 공장 작업 영역에는 유닛 배치 가능
        return true;
    }

    // 공장 본체와 작업 영역을 포함한 5×5 범위의 배치 가능 여부 검사
    public bool CanPlaceFactory(Vector3Int anchorCell)
    {
        if (coordinateManager == null ||
            occupancyManager == null)
        {
            return false;
        }

        if (!IsFactoryAreaInsideMap(anchorCell))
        {
            return false;
        }

        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector3Int cell =
                    anchorCell + new Vector3Int(x, y, 0);

                if (occupancyManager.HasOccupant(cell)) // 다른 오브젝트가 점유하지 않았는지
                {
                    return false;
                }

                if (occupancyManager.IsReserved(cell)) // 다른 유닛의 이동 목적지로 예약되지 않았는지
                {
                    return false;
                }

                if (occupancyManager.IsWorkArea(cell)) // 다른 공장의 작업 영역인지
                {
                    return false;
                }
            }
        }

        return true;
    }

    // 공장의 5×5 전체 영역이 실제 맵 안에 포함되는지 검사
    public bool IsFactoryAreaInsideMap(
        Vector3Int anchorCell)
    {
        if (coordinateManager == null)
        {
            return false;
        }

        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector3Int cell =
                    anchorCell + new Vector3Int(x, y, 0);

                if (!coordinateManager.HasTile(cell))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
