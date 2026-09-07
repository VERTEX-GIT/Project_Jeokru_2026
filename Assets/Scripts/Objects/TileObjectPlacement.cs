using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TileObjectPlacement : MonoBehaviour
{
    [SerializeField]
    private TileOccupancyManager occupancyManager;

    [SerializeField]
    private TileObjectType objectType;

    [SerializeField]
    [Min(1)]
    private Vector2Int size =
        Vector2Int.one;

    public Vector3Int AnchorCell
    {
        get;
        private set;
    }

    public List<Vector3Int> OccupiedCells
    {
        get;
    } = new();

    public bool IsPlaced
    {
        get;
        private set;
    }

    public TileObjectType ObjectType =>
        objectType;

    public Vector2Int Size =>
        size;

    private void Awake()
    {
        ValidateSize();
    }

    private void OnValidate()
    {
        ValidateSize();
    }

    private void OnDestroy()
    {
        if (IsPlaced)
        {
            RemoveFromTiles();
        }
    }

    private void ValidateSize()
    {
        // 유닛은 항상 1×1
        if (objectType ==
            TileObjectType.Unit)
        {
            size =
                Vector2Int.one;

            return;
        }

        // 시설은 Inspector 설정값 유지
        size =
            new Vector2Int(
                Mathf.Max(
                    1,
                    size.x),
                Mathf.Max(
                    1,
                    size.y));
    }

    public bool CanPlace(
        Vector3Int anchorCell)
    {
        if (IsPlaced ||
            !TryGetOccupancyManager(
                out TileOccupancyManager
                    manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells =
            CalculateOccupiedCells(
                anchorCell);

        foreach (Vector3Int cell
                 in candidateCells)
        {
            if (!manager
                .CoordinateManager
                .HasTile(cell))
            {
                return false;
            }
        }

        if (!manager.CanOccupy(
                this,
                candidateCells))
        {
            return false;
        }

        if (TryGetComponent(
                out FactoryWorkArea
                    workArea) &&
            !workArea.CanRegisterWorkArea(
                anchorCell))
        {
            return false;
        }

        return true;
    }

    public bool TryPlace(
        Vector3Int anchorCell)
    {
        if (!CanPlace(anchorCell) ||
            !TryGetOccupancyManager(
                out TileOccupancyManager
                    manager))
        {
            return false;
        }

        List<Vector3Int> candidateCells =
            CalculateOccupiedCells(
                anchorCell);

        if (!manager.TryOccupy(
                this,
                candidateCells))
        {
            return false;
        }

        if (TryGetComponent(
                out FactoryWorkArea
                    workArea) &&
            !workArea.RegisterWorkArea(
                anchorCell))
        {
            manager.ReleaseOccupancy(
                this,
                candidateCells);

            return false;
        }

        AnchorCell =
            anchorCell;

        OccupiedCells.Clear();

        OccupiedCells.AddRange(
            candidateCells);

        IsPlaced =
            true;

        SetWorldPosition(
            manager.CoordinateManager);

        return true;
    }

    public bool RemoveFromTiles()
    {
        if (!IsPlaced ||
            !TryGetOccupancyManager(
                out TileOccupancyManager
                    manager))
        {
            return false;
        }

        if (TryGetComponent(
                out FactoryWorkArea
                    workArea))
        {
            workArea.RemoveWorkArea();
        }

        manager.ReleaseOccupancy(
            this,
            OccupiedCells);

        OccupiedCells.Clear();

        IsPlaced =
            false;

        return true;
    }

    private List<Vector3Int>
        CalculateOccupiedCells(
            Vector3Int anchorCell)
    {
        List<Vector3Int> cells =
            new();

        if (objectType ==
            TileObjectType.Unit)
        {
            cells.Add(
                anchorCell);

            return cells;
        }

        // 홀수 크기:
        // anchor가 중앙 셀.
        //
        // 짝수 크기:
        // anchor가 중앙 4칸 중
        // 왼쪽 아래 셀 역할을 한다.
        int startX =
            -(size.x - 1) / 2;

        int startY =
            -(size.y - 1) / 2;

        for (int x = 0;
             x < size.x;
             x++)
        {
            for (int y = 0;
                 y < size.y;
                 y++)
            {
                cells.Add(
                    anchorCell +
                    new Vector3Int(
                        startX + x,
                        startY + y,
                        0));
            }
        }

        return cells;
    }

    private void SetWorldPosition(
        TileCoordinateManager
            coordinateManager)
    {
        Vector3 worldPosition =
            coordinateManager
                .CellToWorldCenter(
                    AnchorCell);

        // 짝수 크기 시설은 anchor 셀과
        // 시설 기하 중심 사이가 반 칸 어긋난다.
        if (objectType ==
            TileObjectType.Facility)
        {
            if (size.x % 2 == 0)
            {
                worldPosition.x +=
                    0.5f;
            }

            if (size.y % 2 == 0)
            {
                worldPosition.y +=
                    0.5f;
            }
        }

        worldPosition.z =
            transform.position.z;

        transform.position =
            worldPosition;
    }

    private bool TryGetOccupancyManager(
        out TileOccupancyManager manager)
    {
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

        manager =
            occupancyManager;

        return manager != null &&
            manager.CoordinateManager !=
                null;
    }
}