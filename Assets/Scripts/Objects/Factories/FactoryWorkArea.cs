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

    // 작업 영역 등록 가능 여부 검사
    public bool CanRegisterWorkArea(Vector3Int anchorCell)
    {
        if (IsRegistered || placement == null || placement.ObjectType != TileObjectType.Facility || !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells = CalculateWorkCells(anchorCell, manager.CoordinateManager); // 검사할 작업 후보 타일

        return manager.CanRegisterWorkArea(this, candidateCells);
    }

    // 작업 타일 Dictionary에 추가
    public bool RegisterWorkArea(Vector3Int anchorCell)
    {
        if (!CanRegisterWorkArea(anchorCell) || !TryGetOccupancyManager(out TileOccupancyManager manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells = CalculateWorkCells(anchorCell, manager.CoordinateManager); // 실제 등록할 작업 타일

        if (!manager.TryRegisterWorkArea(this, candidateCells))
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
    private List<Vector3Int> CalculateWorkCells(Vector3Int anchorCell, TileCoordinateManager coordinateManager)
    {
        List<Vector3Int> cells = new(); // 맵 안에 존재하는 작업 좌표 목록

        for (int x = -1; x <= placement.Size.x; x++)
        {
            for (int y = -1; y <= placement.Size.y; y++)
            {
                bool isInsideFactory = x >= 0 && x < placement.Size.x && y >= 0 && y < placement.Size.y; // 공장 점유 영역 여부

                if (isInsideFactory)
                {
                    continue;
                }

                Vector3Int cell = anchorCell + new Vector3Int(x, y, 0); // 현재 작업 후보 좌표

                if (coordinateManager.HasTile(cell))
                {
                    cells.Add(cell);
                }
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
