using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class FactoryWorkArea : MonoBehaviour
{
    [SerializeField]
    private TileObjectPlacement placement;              // TileObjectPlacement.cs

    [SerializeField]
    private TileOccupancyManager occupancyManager;      // TileOccupancyManager.cs

    public List<Vector3Int> WorkCells { get; } = new(); // 공장의 작업 타일들 모은 리스트
    public bool IsRegistered { get; private set; }      // 작업 영역 Dictionary 등록 여부

    private void Awake()
    {
        if (placement == null)
        {
            placement = GetComponent<TileObjectPlacement>();
        }
    }

    // 공장이 제거될 때 작업 영역 정보 제거
    private void OnDestroy()
    {
        if (IsRegistered)
        {
            RemoveWorkArea();
        }
    }

    // 공장 작업 영역을 등록할 수 있는지 검사
    public bool CanRegisterWorkArea(Vector3Int anchorCell)
    {
        if (IsRegistered ||
            placement == null ||
            placement.ObjectType != TileObjectType.Facility ||
            !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells =
            CalculateWorkCells(
                anchorCell,
                manager.CoordinateManager);

        if (candidateCells.Count != 16)
        {
            return false;
        }

        return manager.CanRegisterWorkArea(
            this,
            candidateCells);
    }

    // 공장의 작업 영역을 Dictionary에 등록
    public bool RegisterWorkArea(Vector3Int anchorCell)
    {
        if (!CanRegisterWorkArea(anchorCell) ||
            !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells =
            CalculateWorkCells(
                anchorCell,
                manager.CoordinateManager);

        if (candidateCells.Count != 16)
        {
            return false;
        }

        if (!manager.TryRegisterWorkArea(
                this,
                candidateCells))
        {
            return false;
        }

        WorkCells.AddRange(candidateCells);
        IsRegistered = true;

        return true;
    }

    // Dictionary에서 작업 영역 정보 제거
    public bool RemoveWorkArea()
    {
        if (!IsRegistered || !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        manager.ReleaseWorkArea(this, WorkCells);

        WorkCells.Clear();
        IsRegistered = false;
        return true;
    }

    // 해당 좌표가 이 공장의 작업 타일인지 확인
    public bool Contains(Vector3Int cell)
    {
        return WorkCells.Contains(cell);
    }

    // 공장 작업 영역 좌표 계산
    private List<Vector3Int> CalculateWorkCells(
        Vector3Int anchorCell,
        TileCoordinateManager coordinateManager)
    {
        List<Vector3Int> cells = new();

        // 공장 3×3과 주변 작업 영역을 포함한 5×5 검사
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                bool isInsideFactory =
                    Mathf.Abs(x) <= 1 &&
                    Mathf.Abs(y) <= 1;

                if (isInsideFactory)
                {
                    continue;
                }

                Vector3Int cell =
                    anchorCell + new Vector3Int(x, y, 0);

                // 작업 영역이 하나라도 맵 밖이면 유효하지 않은 배치
                if (!coordinateManager.HasTile(cell))
                {
                    return new List<Vector3Int>();
                }

                cells.Add(cell);
            }
        }

        return cells;
    }

    // 작업 영역에 사용할 점유 관리자를 가져오기
    private bool TryGetOccupancyManager(out TileOccupancyManager manager)
    {
        if (occupancyManager == null)
        {
            occupancyManager = TileOccupancyManager.Instance;               // 런타임 생성 프리팹은 씬의 관리자를 사용
        }

        if (occupancyManager == null)
        {
            occupancyManager = FindAnyObjectByType<TileOccupancyManager>(); // Awake 순서와 무관하게 한 번 더 탐색
        }

        manager = occupancyManager;
        return manager != null && manager.CoordinateManager != null;
    }
}
