using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TileObjectPlacement : MonoBehaviour
{
    [SerializeField]
    private TileOccupancyManager occupancyManager;  // TileOccupancyManager.cs

    [SerializeField]
    private TileObjectType objectType;              // 유닛 / 공장 구분

    [SerializeField]
    private Vector2Int size = Vector2Int.one;      // 오브젝트가 점유하는 가로·세로 타일 수

    public Vector3Int AnchorCell { get; private set; }      // 기준 좌표(점유 범위 중 '좌측 하단'이 기준 좌표)
    public List<Vector3Int> OccupiedCells { get; } = new(); // 점유 중인 타일 좌표
    public bool IsPlaced { get; private set; }              // Dictionary 등록 여부

    // private이라서 쓴 람다인데, 솔직히 public으로 해도 되고, 아예 외부에서 이 값들 안 읽을 거면 필요없을 듯
    public TileObjectType ObjectType => objectType;
    public Vector2Int Size => size;

    // OnValidate: Inspector에서 속성이 수정될 때 호출
    // Inspector에서 타일 크기가 1보다 작아지지 않도록 제한한다.
    private void OnValidate()
    {
        size.x = Mathf.Max(1, size.x);
        size.y = Mathf.Max(1, size.y);
    }

    // 오브젝트가 제거될 때 해시맵에 타일 점유 정보 제거
    private void OnDestroy()
    {
        if (IsPlaced)
        {
            RemoveFromTiles();
        }
    }

    // 타일에 오브젝트를 배치할 수 있는지 검사
    public bool CanPlace(Vector3Int anchorCell)
    {
        if (IsPlaced || !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells = CalculateOccupiedCells(anchorCell); // 검사할 점유 후보 타일

        if (!manager.CanOccupy(this, candidateCells))
        {
            return false;
        }

        if (TryGetComponent(out FactoryWorkArea workArea) &&
            !workArea.CanRegisterWorkArea(anchorCell))
        {
            return false;
        }

        return true;
    }

    // 타일에 오브젝트 배치
    public bool TryPlace(Vector3Int anchorCell)
    {
        if (!CanPlace(anchorCell) || !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells = CalculateOccupiedCells(anchorCell); // 실제 등록할 점유 타일

        if (!manager.TryOccupy(this, candidateCells))
        {
            return false;
        }

        if (TryGetComponent(out FactoryWorkArea workArea) && !workArea.RegisterWorkArea(anchorCell))
        {
            manager.ReleaseOccupancy(this, candidateCells); // 작업 영역 등록 실패 시 점유 등록 복구
            return false;
        }

        AnchorCell = anchorCell;
        OccupiedCells.AddRange(candidateCells);
        IsPlaced = true;

        SetWorldPosition(manager.CoordinateManager);

        return true;
    }

    // 작업 영역과 오브젝트 점유 정보 제거
    public bool RemoveFromTiles()
    {
        if (!IsPlaced || !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        if (TryGetComponent(out FactoryWorkArea workArea))
        {
            workArea.RemoveWorkArea();
        }

        manager.ReleaseOccupancy(this, OccupiedCells);

        OccupiedCells.Clear();
        IsPlaced = false;

        return true;
    }

    // 오브젝트의 점유 타일 목록 계산
    private List<Vector3Int> CalculateOccupiedCells(Vector3Int anchorCell)
    {
        List<Vector3Int> cells = new(); // 계산 결과를 담는 임시 좌표 목록

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                cells.Add(anchorCell + new Vector3Int(x, y, 0));
            }
        }

        return cells;
    }

    // 오브젝트를 전체 점유 영역 중앙에 배치
    private void SetWorldPosition(TileCoordinateManager coordinateManager)
    {
        Vector3 bottomLeft = coordinateManager.CellToWorldCenter(AnchorCell);               // 왼쪽 아래 타일 중앙
        Vector3Int topRightCell = AnchorCell + new Vector3Int(size.x - 1, size.y - 1, 0);   // 오른쪽 위 타일
        Vector3 topRight = coordinateManager.CellToWorldCenter(topRightCell);               // 오른쪽 위 타일 중앙
        Vector3 worldCenter = (bottomLeft + topRight) * 0.5f;                               // 전체 점유 영역의 중앙

        worldCenter.z = transform.position.z;   // 기존 Sprite 정렬용 Z좌표 유지
        transform.position = worldCenter;
    }

    // 오브젝트 배치에 사용할 점유 관리자 가져오기
    private bool TryGetOccupancyManager(out TileOccupancyManager manager)
    {
        if (occupancyManager == null)
        {
            occupancyManager = TileOccupancyManager.Instance;
        }

        if (occupancyManager == null)
        {
            occupancyManager = FindAnyObjectByType<TileOccupancyManager>();
        }

        manager = occupancyManager;

        return manager != null && manager.CoordinateManager != null;
    }
}
