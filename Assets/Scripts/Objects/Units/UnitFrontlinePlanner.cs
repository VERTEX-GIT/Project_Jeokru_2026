using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 우클릭 드래그로 생성된 전선 타일을 관리하고
// 선택된 유닛을 전선 위 목적지에 배정
[DisallowMultipleComponent]
public sealed class UnitFrontlinePlanner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TileOccupancyManager occupancyManager;

    [SerializeField]
    private Tilemap previewTilemap;

    [SerializeField]
    private TileBase previewTile;

    private readonly List<Vector3Int> frontlineCells = new();
    private readonly HashSet<Vector3Int> frontlineCellSet = new();

    private GridPathfinder pathfinder;

    private bool isDrawing;
    private Vector3Int lastCell;

    public bool IsDrawing => isDrawing;

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

        if (occupancyManager != null)
        {
            pathfinder =
                new GridPathfinder(
                    occupancyManager);
        }

        ClearPreview();
    }

    public bool BeginFrontline(
        Vector3Int startCell)
    {
        if (!IsMapCell(startCell))
        {
            return false;
        }

        ClearPreview();

        isDrawing = true;
        lastCell = startCell;

        AddFrontlineCell(startCell);

        return true;
    }

    public void UpdateFrontline(
        Vector3Int currentCell)
    {
        if (!isDrawing ||
            !IsMapCell(currentCell))
        {
            return;
        }

        if (currentCell == lastCell)
        {
            return;
        }

        AddCellsBetween(
            lastCell,
            currentCell);

        lastCell = currentCell;
    }

    public bool CompleteFrontline(
        IReadOnlyList<UnitSelectable> selectedUnits)
    {
        if (!isDrawing)
        {
            return false;
        }

        isDrawing = false;

        if (selectedUnits == null ||
            selectedUnits.Count == 0 ||
            occupancyManager == null ||
            pathfinder == null)
        {
            ClearPreview();
            return false;
        }

        List<UnitInfo> units =
            CollectMovableUnits(
                selectedUnits);

        if (units.Count == 0)
        {
            ClearPreview();
            return false;
        }

        List<Vector3Int> availableCells =
            CollectAvailableFrontlineCells();

        if (availableCells.Count < units.Count)
        {
            ExpandFrontlineFromTail(
                availableCells,
                units.Count);
        }

        if (availableCells.Count < units.Count)
        {
            Debug.LogWarning(
                $"전선 확장 실패: " +
                $"{availableCells.Count}칸 / " +
                $"{units.Count}명",
                this);

            ClearPreview();
            return false;
        }

        List<Vector3Int> destinationCells =
            SelectEvenlyDistributedCells(
                availableCells,
                units.Count);

        List<Assignment> assignments =
            BuildOrderedAssignments(
                units,
                destinationCells);

        if (assignments.Count != units.Count)
        {
            Debug.LogWarning(
                "전선 목적지에 모든 유닛이 " +
                "도달할 수 없어 배치를 취소합니다.",
                this);

            ClearPreview();
            return false;
        }

        bool issuedAnyCommand = false;

        foreach (Assignment assignment
                in assignments)
        {
            if (!assignment.Unit.Movement
                    .TryMoveTo(
                        assignment.Cell))
            {
                continue;
            }

            issuedAnyCommand = true;

            UnitCore core =
                assignment.Unit.Core;

            if (core == null)
            {
                continue;
            }

            core.SetPlayerMoveCommandActive(true);
            core.SetAutoCombat(false);
            core.ClearTarget();

            if (core.TryGetComponent(
                    out UnitWorkRecovery recovery))
            {
                recovery.ClearInterruptedWork();
            }
        }

        ClearPreview();

        return issuedAnyCommand;
    }

    // =========================
    // 전선 자동 확장
    // =========================

    private void ExpandFrontlineFromTail(
        List<Vector3Int> availableCells,
        int requiredCount)
    {
        if (availableCells == null ||
            availableCells.Count == 0 ||
            availableCells.Count >= requiredCount)
        {
            return;
        }

        if (frontlineCells.Count < 2)
        {
            return;
        }

        Vector3Int previous =
            frontlineCells[
                frontlineCells.Count - 2];

        Vector3Int tail =
            frontlineCells[
                frontlineCells.Count - 1];

        Vector3Int direction =
            NormalizeGridDirection(
                tail - previous);

        if (direction == Vector3Int.zero)
        {
            return;
        }

        HashSet<Vector3Int> availableSet =
            new(availableCells);

        Vector3Int currentTail = tail;
        Vector3Int currentDirection = direction;

        while (availableCells.Count <
               requiredCount)
        {
            if (!TryGetNextExpansionCell(
                    currentTail,
                    currentDirection,
                    availableSet,
                    out Vector3Int nextCell,
                    out Vector3Int nextDirection))
            {
                return;
            }

            availableSet.Add(nextCell);
            availableCells.Add(nextCell);

            AddFrontlineCell(nextCell);

            currentTail = nextCell;
            currentDirection = nextDirection;
        }
    }

    private bool TryGetNextExpansionCell(
        Vector3Int tail,
        Vector3Int direction,
        HashSet<Vector3Int> existingCells,
        out Vector3Int nextCell,
        out Vector3Int nextDirection)
    {
        nextCell = default;
        nextDirection = default;

        Vector3Int[] candidates =
            GetForwardCandidates(
                direction);

        foreach (Vector3Int candidateDirection
                in candidates)
        {
            Vector3Int candidateCell =
                tail + candidateDirection;

            if (existingCells.Contains(
                    candidateCell))
            {
                continue;
            }

            if (!IsAvailableDestination(
                    candidateCell))
            {
                continue;
            }

            nextCell = candidateCell;
            nextDirection =
                candidateDirection;

            return true;
        }

        return false;
    }

    private static Vector3Int[]
        GetForwardCandidates(
            Vector3Int direction)
    {
        Vector3Int normalized =
            NormalizeGridDirection(
                direction);

        return new[]
        {
            normalized,
            RotateDirectionLeft45(normalized),
            RotateDirectionRight45(normalized)
        };
    }

    private static Vector3Int
        NormalizeGridDirection(
            Vector3Int direction)
    {
        return new Vector3Int(
            Mathf.Clamp(
                direction.x,
                -1,
                1),
            Mathf.Clamp(
                direction.y,
                -1,
                1),
            0);
    }

    private static Vector3Int
        RotateDirectionLeft45(
            Vector3Int direction)
    {
        if (direction == Vector3Int.right)
        {
            return new Vector3Int(1, 1, 0);
        }

        if (direction == new Vector3Int(1, 1, 0))
        {
            return Vector3Int.up;
        }

        if (direction == Vector3Int.up)
        {
            return new Vector3Int(-1, 1, 0);
        }

        if (direction == new Vector3Int(-1, 1, 0))
        {
            return Vector3Int.left;
        }

        if (direction == Vector3Int.left)
        {
            return new Vector3Int(-1, -1, 0);
        }

        if (direction == new Vector3Int(-1, -1, 0))
        {
            return Vector3Int.down;
        }

        if (direction == Vector3Int.down)
        {
            return new Vector3Int(1, -1, 0);
        }

        if (direction == new Vector3Int(1, -1, 0))
        {
            return Vector3Int.right;
        }

        return direction;
    }

    private static Vector3Int
        RotateDirectionRight45(
            Vector3Int direction)
    {
        if (direction == Vector3Int.right)
        {
            return new Vector3Int(1, -1, 0);
        }

        if (direction == new Vector3Int(1, -1, 0))
        {
            return Vector3Int.down;
        }

        if (direction == Vector3Int.down)
        {
            return new Vector3Int(-1, -1, 0);
        }

        if (direction == new Vector3Int(-1, -1, 0))
        {
            return Vector3Int.left;
        }

        if (direction == Vector3Int.left)
        {
            return new Vector3Int(-1, 1, 0);
        }

        if (direction == new Vector3Int(-1, 1, 0))
        {
            return Vector3Int.up;
        }

        if (direction == Vector3Int.up)
        {
            return new Vector3Int(1, 1, 0);
        }

        if (direction == new Vector3Int(1, 1, 0))
        {
            return Vector3Int.right;
        }

        return direction;
    }

    // =========================
    // 유닛 ↔ 전선 배정
    // =========================

    // 전선 진행 방향을 기준으로 유닛의 공간 순서를 정렬하고
    // 정방향/역방향 중 총 이동 비용이 더 낮은 배치를 사용
    private List<Assignment>
        BuildOrderedAssignments(
            List<UnitInfo> units,
            List<Vector3Int> destinationCells)
    {
        List<Assignment> empty = new();

        if (units == null ||
            destinationCells == null ||
            units.Count == 0 ||
            units.Count != destinationCells.Count)
        {
            return empty;
        }

        Vector3Int lineStart =
            destinationCells[0];

        Vector3Int lineEnd =
            destinationCells[
                destinationCells.Count - 1];

        Vector2 lineDirection =
            new Vector2(
                lineEnd.x - lineStart.x,
                lineEnd.y - lineStart.y);

        List<UnitInfo> orderedUnits =
            new(units);

        // 시작점과 끝점이 다른 정상적인 전선
        if (lineDirection.sqrMagnitude > 0.001f)
        {
            orderedUnits.Sort(
                (left, right) =>
                {
                    float leftProjection =
                        GetProjection(
                            left.Cell,
                            lineStart,
                            lineDirection);

                    float rightProjection =
                        GetProjection(
                            right.Cell,
                            lineStart,
                            lineDirection);

                    int comparison =
                        leftProjection.CompareTo(
                            rightProjection);

                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    return left.Order.CompareTo(
                        right.Order);
                });
        }
        else
        {
            // 극단적인 형태의 전선에서는
            // 가장 가까운 전선 인덱스로 순서를 정한다.
            orderedUnits.Sort(
                (left, right) =>
                {
                    int leftIndex =
                        FindNearestCellIndex(
                            left.Cell,
                            destinationCells);

                    int rightIndex =
                        FindNearestCellIndex(
                            right.Cell,
                            destinationCells);

                    int comparison =
                        leftIndex.CompareTo(
                            rightIndex);

                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    return left.Order.CompareTo(
                        right.Order);
                });
        }

        // 유닛 순서 그대로 시작→끝에 배정
        AssignmentResult forward =
            TryBuildOrderedAssignment(
                orderedUnits,
                destinationCells,
                false);

        // 유닛 순서 그대로 끝→시작에 배정
        AssignmentResult reverse =
            TryBuildOrderedAssignment(
                orderedUnits,
                destinationCells,
                true);

        if (forward.IsValid &&
            reverse.IsValid)
        {
            return forward.TotalCost <=
                   reverse.TotalCost
                ? forward.Assignments
                : reverse.Assignments;
        }

        if (forward.IsValid)
        {
            return forward.Assignments;
        }

        if (reverse.IsValid)
        {
            return reverse.Assignments;
        }

        // 순서를 유지한 배치가 불가능한 특수 상황에서는
        // 기존 경로 비용 기반 매칭으로 최후의 시도
        return BuildGreedyAssignments(
            units,
            destinationCells);
    }

    private AssignmentResult
        TryBuildOrderedAssignment(
            List<UnitInfo> orderedUnits,
            List<Vector3Int> destinationCells,
            bool reverse)
    {
        List<Assignment> assignments =
            new();

        int totalCost = 0;

        for (int i = 0;
             i < orderedUnits.Count;
             i++)
        {
            int cellIndex =
                reverse
                    ? destinationCells.Count - 1 - i
                    : i;

            Vector3Int destination =
                destinationCells[cellIndex];

            Dictionary<Vector3Int, int> distances =
                pathfinder.BuildDistanceMap(
                    destination);

            UnitInfo unit =
                orderedUnits[i];

            if (!distances.TryGetValue(
                    unit.Cell,
                    out int cost))
            {
                return new AssignmentResult(
                    false,
                    int.MaxValue,
                    new List<Assignment>());
            }

            totalCost += cost;

            assignments.Add(
                new Assignment(
                    unit,
                    destination));
        }

        return new AssignmentResult(
            true,
            totalCost,
            assignments);
    }

    // 순서 보존 배치가 아예 불가능할 때만 사용하는 fallback
    private List<Assignment>
        BuildGreedyAssignments(
            List<UnitInfo> units,
            List<Vector3Int> destinationCells)
    {
        List<Assignment> result = new();

        HashSet<int> assignedUnits = new();
        HashSet<Vector3Int> assignedCells = new();

        Dictionary<
            Vector3Int,
            Dictionary<Vector3Int, int>>
            distanceMaps = new();

        foreach (Vector3Int cell
                in destinationCells)
        {
            distanceMaps[cell] =
                pathfinder.BuildDistanceMap(
                    cell);
        }

        while (result.Count < units.Count)
        {
            UnitInfo bestUnit = null;
            Vector3Int bestCell = default;
            int bestCost = int.MaxValue;
            bool found = false;

            foreach (UnitInfo unit in units)
            {
                if (assignedUnits.Contains(
                        unit.Order))
                {
                    continue;
                }

                foreach (Vector3Int cell
                        in destinationCells)
                {
                    if (assignedCells.Contains(
                            cell))
                    {
                        continue;
                    }

                    if (!distanceMaps[cell]
                            .TryGetValue(
                                unit.Cell,
                                out int cost))
                    {
                        continue;
                    }

                    if (found &&
                        cost >= bestCost)
                    {
                        continue;
                    }

                    found = true;
                    bestUnit = unit;
                    bestCell = cell;
                    bestCost = cost;
                }
            }

            if (!found ||
                bestUnit == null)
            {
                break;
            }

            result.Add(
                new Assignment(
                    bestUnit,
                    bestCell));

            assignedUnits.Add(
                bestUnit.Order);

            assignedCells.Add(
                bestCell);
        }

        return result;
    }

    private static float GetProjection(
        Vector3Int cell,
        Vector3Int origin,
        Vector2 direction)
    {
        Vector2 offset =
            new Vector2(
                cell.x - origin.x,
                cell.y - origin.y);

        return Vector2.Dot(
            offset,
            direction);
    }

    private static int FindNearestCellIndex(
        Vector3Int unitCell,
        List<Vector3Int> cells)
    {
        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0;
             i < cells.Count;
             i++)
        {
            Vector3Int difference =
                cells[i] - unitCell;

            int distance =
                difference.x * difference.x +
                difference.y * difference.y;

            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestIndex = i;
        }

        return bestIndex;
    }

    // =========================
    // 전선 셀 처리
    // =========================

    public void CancelFrontline()
    {
        isDrawing = false;
        ClearPreview();
    }

    private List<Vector3Int>
        CollectAvailableFrontlineCells()
    {
        List<Vector3Int> result = new();

        foreach (Vector3Int cell
                in frontlineCells)
        {
            if (!IsAvailableDestination(cell))
            {
                continue;
            }

            result.Add(cell);
        }

        return result;
    }

    private static List<Vector3Int>
        SelectEvenlyDistributedCells(
            List<Vector3Int> cells,
            int requiredCount)
    {
        List<Vector3Int> result =
            new(requiredCount);

        if (requiredCount <= 0 ||
            cells == null ||
            cells.Count == 0)
        {
            return result;
        }

        if (requiredCount == 1)
        {
            int middleIndex =
                (cells.Count - 1) / 2;

            result.Add(
                cells[middleIndex]);

            return result;
        }

        for (int i = 0;
             i < requiredCount;
             i++)
        {
            float t =
                i /
                (float)(requiredCount - 1);

            int index =
                Mathf.RoundToInt(
                    t *
                    (cells.Count - 1));

            Vector3Int cell =
                cells[index];

            if (!result.Contains(cell))
            {
                result.Add(cell);
            }
        }

        if (result.Count < requiredCount)
        {
            foreach (Vector3Int cell
                    in cells)
            {
                if (result.Contains(cell))
                {
                    continue;
                }

                result.Add(cell);

                if (result.Count >=
                    requiredCount)
                {
                    break;
                }
            }
        }

        return result;
    }

    private static List<UnitInfo>
        CollectMovableUnits(
            IReadOnlyList<UnitSelectable>
                selectedUnits)
    {
        List<UnitInfo> result = new();

        for (int i = 0;
             i < selectedUnits.Count;
             i++)
        {
            UnitSelectable selectable =
                selectedUnits[i];

            if (selectable == null ||
                !selectable.TryGetComponent(
                    out UnitCore core) ||
                !core.IsActive ||
                core.Data == null ||
                core.Data.Team != UnitTeam.Ally ||
                !selectable.TryGetComponent(
                    out UnitMovement movement) ||
                !selectable.TryGetComponent(
                    out TileObjectPlacement placement) ||
                (!movement.IsMoving &&
                 !placement.IsPlaced))
            {
                continue;
            }

            result.Add(
                new UnitInfo(
                    movement,
                    core,
                    movement.CurrentCommandCell,
                    i));
        }

        return result;
    }

    // =========================
    // 드래그 선 생성
    // =========================

    private void AddCellsBetween(
        Vector3Int from,
        Vector3Int to)
    {
        int x0 = from.x;
        int y0 = from.y;

        int x1 = to.x;
        int y1 = to.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx =
            x0 < x1
                ? 1
                : -1;

        int sy =
            y0 < y1
                ? 1
                : -1;

        int error =
            dx - dy;

        while (true)
        {
            Vector3Int cell =
                new Vector3Int(
                    x0,
                    y0,
                    from.z);

            AddFrontlineCell(cell);

            if (x0 == x1 &&
                y0 == y1)
            {
                break;
            }

            int doubledError =
                error * 2;

            if (doubledError > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (doubledError < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void AddFrontlineCell(
        Vector3Int cell)
    {
        if (!IsMapCell(cell) ||
            !frontlineCellSet.Add(cell))
        {
            return;
        }

        frontlineCells.Add(cell);

        if (previewTilemap != null &&
            previewTile != null)
        {
            previewTilemap.SetTile(
                cell,
                previewTile);
        }
    }

    private bool IsMapCell(
        Vector3Int cell)
    {
        return occupancyManager != null &&
            occupancyManager.CoordinateManager != null &&
            occupancyManager.CoordinateManager
                .HasTile(cell);
    }

    private bool IsAvailableDestination(
        Vector3Int cell)
    {
        if (!IsMapCell(cell))
        {
            return false;
        }

        if (occupancyManager.HasOccupant(cell))
        {
            return false;
        }

        if (occupancyManager.IsReserved(cell))
        {
            return false;
        }

        return true;
    }

    private void ClearPreview()
    {
        frontlineCells.Clear();
        frontlineCellSet.Clear();

        if (previewTilemap != null)
        {
            previewTilemap.ClearAllTiles();
        }
    }

    // =========================
    // 내부 데이터
    // =========================

    private sealed class UnitInfo
    {
        public UnitMovement Movement { get; }
        public UnitCore Core { get; }
        public Vector3Int Cell { get; }
        public int Order { get; }

        public UnitInfo(
            UnitMovement movement,
            UnitCore core,
            Vector3Int cell,
            int order)
        {
            Movement = movement;
            Core = core;
            Cell = cell;
            Order = order;
        }
    }

    private sealed class Assignment
    {
        public UnitInfo Unit { get; }
        public Vector3Int Cell { get; }

        public Assignment(
            UnitInfo unit,
            Vector3Int cell)
        {
            Unit = unit;
            Cell = cell;
        }
    }

    private sealed class AssignmentResult
    {
        public bool IsValid { get; }
        public int TotalCost { get; }
        public List<Assignment> Assignments { get; }

        public AssignmentResult(
            bool isValid,
            int totalCost,
            List<Assignment> assignments)
        {
            IsValid = isValid;
            TotalCost = totalCost;
            Assignments = assignments;
        }
    }
}