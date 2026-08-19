using System.Collections.Generic;
using UnityEngine;

// 선택된 유닛들을 명령 지점과 그 주변의 도달 가능한 목적지에 일대일 배정
[DisallowMultipleComponent]
public sealed class UnitDestinationAssigner : MonoBehaviour
{
    [SerializeField]
    private TileOccupancyManager occupancyManager;

    [SerializeField]
    [Min(1)]
    private int searchRadius = 8; // 명령 지점 주변에서 목적지 후보를 찾을 최대 반경

    private GridPathfinder pathfinder;

    // 씬의 점유 관리자를 찾고 목적지별 이동 비용을 계산할 경로 탐색기 생성
    private void Awake()
    {
        if (occupancyManager == null)
        {
            occupancyManager = TileOccupancyManager.Instance;
        }

        if (occupancyManager == null)
        {
            occupancyManager = FindAnyObjectByType<TileOccupancyManager>();
        }

        if (occupancyManager != null)
        {
            pathfinder = new GridPathfinder(occupancyManager);
        }
    }

    // 이동 가능한 선택 유닛마다 중복되지 않는 목적지를 정해 이동 요청
    public void IssueMoveCommand(
        IReadOnlyList<UnitSelectable> selectedUnits,
        Vector3Int commandCell)
    {
        if (occupancyManager == null || pathfinder == null || selectedUnits == null)
        {
            return;
        }

        List<UnitInfo> units = CollectMovableUnits(selectedUnits);

        if (units.Count == 0)
        {
            return;
        }

        List<Assignment> assignments = new();
        HashSet<int> assignedUnits = new();
        HashSet<Vector3Int> assignedCells = new();

        TryAssignCommandCell(
            commandCell,
            units,
            assignments,
            assignedUnits,
            assignedCells);

        List<CandidateInfo> candidates = CollectCandidates(
            commandCell,
            units,
            assignedUnits,
            assignedCells,
            units.Count - assignedUnits.Count);

        MatchRemainingUnits(
            commandCell,
            units,
            candidates,
            assignments,
            assignedUnits,
            assignedCells);

        assignments.Sort((left, right) => left.Unit.Order.CompareTo(right.Unit.Order));

        foreach (Assignment assignment in assignments)
        {
            if (assignment.Unit.Movement
                    .TryMoveTo(assignment.Cell))
            {
                if (assignment.Unit.Core == null)
                {
                    continue;
                }

                assignment.Unit.Core
                    .SetPlayerMoveCommandActive(true);

                assignment.Unit.Core
                    .SetAutoCombat(false);

                assignment.Unit.Core
                    .ClearTarget();

                if (assignment.Unit.Core.TryGetComponent(
                        out UnitWorkRecovery recovery))
                {
                    recovery.ClearInterruptedWork();
                }
            }
        }
    }

    // 선택된 유닛들을 공장의 사용 가능한 작업 타일에 배정
    public void IssueFactoryCommand(
    IReadOnlyList<UnitSelectable> selectedUnits,
    FactoryCore factory)
    {
        if (occupancyManager == null ||
            pathfinder == null ||
            selectedUnits == null ||
            factory == null ||
            !factory.TryGetComponent(
                out FactoryWorkArea workArea) ||
            !factory.TryGetComponent(
                out TileObjectPlacement factoryPlacement) ||
            !workArea.IsRegistered)
        {
            return;
        }

        List<UnitInfo> units =
            CollectMovableUnits(selectedUnits);

        // UnitCore가 없는 유닛은 공장 작업 명령 대상에서 제외
        units.RemoveAll(unit => unit.Core == null);

        if (units.Count == 0)
        {
            return;
        }

        List<Assignment> assignments = new();
        HashSet<int> assignedUnits = new();
        HashSet<Vector3Int> assignedCells = new();

        // 이미 이 공장의 작업 타일에 정지해 있는 유닛
        foreach (UnitInfo unit in units)
        {
            if (!unit.Movement.IsMoving &&
                unit.Placement.IsPlaced &&
                workArea.Contains(
                    unit.Placement.AnchorCell))
            {
                unit.Core.SetPlayerMoveCommandActive(false);
                unit.Core.SetAutoCombat(false);
                unit.Core.SetTarget(
                    factory.gameObject);

                if (unit.Core.TryGetComponent(
                        out UnitWorkRecovery recovery))
                {
                    recovery.ClearInterruptedWork();
                }

                assignedUnits.Add(
                    unit.Order);

                assignedCells.Add(
                    unit.Placement.AnchorCell);
            }
        }

        // 이미 이 공장의 작업 타일로 이동 중이라면
        // 목적지를 다시 배정하지 않는다.
        foreach (UnitInfo unit in units)
        {
            if (assignedUnits.Contains(
                    unit.Order))
            {
                continue;
            }

            if (unit.Movement.IsMoving &&
                unit.Core.CurrentTarget ==
                    factory.gameObject &&
                workArea.Contains(
                    unit.Movement.DestinationCell))
            {
                unit.Core.SetAutoCombat(false);

                if (unit.Core.TryGetComponent(
                        out UnitWorkRecovery recovery))
                {
                    recovery.ClearInterruptedWork();
                }

                assignedUnits.Add(
                    unit.Order);

                assignedCells.Add(
                    unit.Movement.DestinationCell);
            }
        }

        List<CandidateInfo> candidates =
            CollectFactoryCandidates(
                workArea,
                units,
                assignedUnits,
                assignedCells);

        MatchRemainingUnits(
            factoryPlacement.AnchorCell,
            units,
            candidates,
            assignments,
            assignedUnits,
            assignedCells);

        assignments.Sort(
            (left, right) =>
                left.Unit.Order.CompareTo(
                    right.Unit.Order));

        foreach (Assignment assignment
                in assignments)
        {
            if (assignment.Unit.Movement.TryMoveTo(
                    assignment.Cell))
            {
                assignment.Unit.Core.SetAutoCombat(false);

                assignment.Unit.Core.SetTarget(
                    factory.gameObject);

                if (assignment.Unit.Core.TryGetComponent(
                        out UnitWorkRecovery recovery))
                {
                    recovery.ClearInterruptedWork();
                }
            }
        }
    }

    public void IssueCombatCommand(
        IReadOnlyList<UnitSelectable> selectedUnits)
    {
        if (selectedUnits == null ||
            selectedUnits.Count == 0)
        {
            return;
        }

        UnitCore[] allUnits =
            FindObjectsByType<UnitCore>(
                FindObjectsSortMode.None);

        foreach (UnitSelectable selectable
                in selectedUnits)
        {
            if (selectable == null ||
                !selectable.TryGetComponent(
                    out UnitCore ally) ||
                !ally.IsActive ||
                ally.Data == null ||
                ally.Data.Team != UnitTeam.Ally)
            {
                continue;
            }

            // 여기 추가
            ally.SetAutoCombat(true);

            UnitCore nearestEnemy =
                FindNearestEnemy(
                    ally,
                    allUnits);

            if (nearestEnemy == null)
            {
                ally.ClearTarget();
                continue;
            }

            ally.SetTarget(
                nearestEnemy.gameObject);
        }
    }

    private UnitCore FindNearestEnemy(
        UnitCore ally,
        UnitCore[] allUnits)
    {
        UnitCore nearest = null;
        float nearestDistanceSqr =
            float.MaxValue;

        foreach (UnitCore candidate
                in allUnits)
        {
            if (candidate == null ||
                candidate == ally ||
                !candidate.IsActive ||
                candidate.Data == null ||
                candidate.Data.Team !=
                    UnitTeam.Enemy)
            {
                continue;
            }

            float distanceSqr =
                (candidate.transform.position -
                ally.transform.position)
                .sqrMagnitude;

            if (distanceSqr >=
                nearestDistanceSqr)
            {
                continue;
            }

            nearestDistanceSqr =
                distanceSqr;

            nearest = candidate;
        }

        return nearest;
    }

    // 공장의 비어 있고 도달 가능한 작업 타일 수집
    private List<CandidateInfo> CollectFactoryCandidates(
        FactoryWorkArea workArea,
        List<UnitInfo> units,
        HashSet<int> assignedUnits,
        HashSet<Vector3Int> assignedCells)
    {
        List<CandidateInfo> result = new();

        foreach (Vector3Int cell in workArea.WorkCells)
        {
            if (assignedCells.Contains(cell) ||
                !IsAvailableDestination(cell))
            {
                continue;
            }

            Dictionary<Vector3Int, int> distances =
                pathfinder.BuildDistanceMap(cell);

            bool reachesAnyUnit = false;

            foreach (UnitInfo unit in units)
            {
                if (!assignedUnits.Contains(unit.Order) &&
                    distances.ContainsKey(unit.Cell))
                {
                    reachesAnyUnit = true;
                    break;
                }
            }

            if (!reachesAnyUnit)
            {
                continue;
            }

            result.Add(
                new CandidateInfo(
                    cell,
                    distances));
        }

        return result;
    }

    // 선택 순서를 보존하면서 현재 명령을 받을 수 있는 유닛 수집
    private List<UnitInfo> CollectMovableUnits(
        IReadOnlyList<UnitSelectable> selectedUnits)
    {
        List<UnitInfo> result = new();

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            UnitSelectable selectable = selectedUnits[i];

            if (selectable == null ||
                !selectable.TryGetComponent(out UnitCore unitCore) ||
                !unitCore.IsActive ||
                unitCore.Data == null ||
                unitCore.Data.Team != UnitTeam.Ally ||
                !selectable.TryGetComponent(out UnitMovement movement) ||
                !selectable.TryGetComponent(out TileObjectPlacement placement) ||
                (!movement.IsMoving && !placement.IsPlaced))
            {
                continue;
            }

            result.Add(
                new UnitInfo(
                    movement,
                    placement,
                    unitCore,
                    movement.CurrentCommandCell,
                    i));
        }

        return result;
    }

    // 명령 지점에 도달 가능한 유닛 중 이동 비용이 가장 낮은 유닛을 우선 배정
    private void TryAssignCommandCell(
        Vector3Int commandCell,
        List<UnitInfo> units,
        List<Assignment> assignments,
        HashSet<int> assignedUnits,
        HashSet<Vector3Int> assignedCells)
    {
        if (!IsAvailableDestination(commandCell))
        {
            return;
        }

        Dictionary<Vector3Int, int> distances =
            pathfinder.BuildDistanceMap(commandCell);
        UnitInfo bestUnit = null;
        int bestCost = int.MaxValue;

        foreach (UnitInfo unit in units)
        {
            if (!distances.TryGetValue(unit.Cell, out int cost) ||
                cost > bestCost ||
                cost == bestCost && bestUnit != null && unit.Order > bestUnit.Order)
            {
                continue;
            }

            bestUnit = unit;
            bestCost = cost;
        }

        if (bestUnit == null)
        {
            return;
        }

        assignments.Add(new Assignment(bestUnit, commandCell));
        assignedUnits.Add(bestUnit.Order);
        assignedCells.Add(commandCell);
    }

    // 명령 지점에서 바깥쪽 사각 링 순서로 도달 가능한 목적지 후보 수집
    private List<CandidateInfo> CollectCandidates(
        Vector3Int commandCell,
        List<UnitInfo> units,
        HashSet<int> assignedUnits,
        HashSet<Vector3Int> assignedCells,
        int requiredCount)
    {
        List<CandidateInfo> result = new();
        int reachableCandidateCount = 0;

        for (int radius = 1; radius <= searchRadius; radius++)
        {
            List<Vector3Int> ring = GetRingCells(commandCell, radius);

            foreach (Vector3Int cell in ring)
            {
                if (assignedCells.Contains(cell) || !IsAvailableDestination(cell))
                {
                    continue;
                }

                Dictionary<Vector3Int, int> distances = pathfinder.BuildDistanceMap(cell);
                bool reachesAnyUnit = false;

                foreach (UnitInfo unit in units)
                {
                    if (!assignedUnits.Contains(unit.Order) && distances.ContainsKey(unit.Cell))
                    {
                        reachesAnyUnit = true;
                        break;
                    }
                }

                if (!reachesAnyUnit)
                {
                    continue;
                }

                result.Add(new CandidateInfo(cell, distances));
                reachableCandidateCount++;
            }

            // 현재 반경의 링은 끝까지 수집한 뒤 바깥쪽 탐색을 중단한다.
            if (reachableCandidateCount >= requiredCount)
            {
                break;
            }
        }

        return result;
    }

    // 중심에서 체비쇼프 거리가 radius인 사각 링의 모든 셀 생성
    private static List<Vector3Int> GetRingCells(Vector3Int center, int radius)
    {
        List<Vector3Int> cells = new(radius * 8);

        for (int x = -radius; x <= radius; x++)
        {
            cells.Add(center + new Vector3Int(x, -radius, 0));
            cells.Add(center + new Vector3Int(x, radius, 0));
        }

        for (int y = -radius + 1; y <= radius - 1; y++)
        {
            cells.Add(center + new Vector3Int(-radius, y, 0));
            cells.Add(center + new Vector3Int(radius, y, 0));
        }

        return cells;
    }

    // 아직 배정되지 않은 유닛과 후보 중 가장 좋은 조합을 반복 선택
    private void MatchRemainingUnits(
        Vector3Int commandCell,
        List<UnitInfo> units,
        List<CandidateInfo> candidates,
        List<Assignment> assignments,
        HashSet<int> assignedUnits,
        HashSet<Vector3Int> assignedCells)
    {
        while (true)
        {
            Match best = null;

            foreach (UnitInfo unit in units)
            {
                if (assignedUnits.Contains(unit.Order))
                {
                    continue;
                }

                foreach (CandidateInfo candidate in candidates)
                {
                    if (assignedCells.Contains(candidate.Cell) ||
                        !candidate.Distances.TryGetValue(unit.Cell, out int cost))
                    {
                        continue;
                    }

                    Match current = new(unit, candidate, cost);

                    if (best == null || IsBetterMatch(current, best, commandCell))
                    {
                        best = current;
                    }
                }
            }

            if (best == null)
            {
                return;
            }

            assignments.Add(new Assignment(best.Unit, best.Candidate.Cell));
            assignedUnits.Add(best.Unit.Order);
            assignedCells.Add(best.Candidate.Cell);
        }
    }

    // 경로 비용, 명령 지점과의 거리, 셀 좌표, 선택 순서 순으로 조합 비교
    private static bool IsBetterMatch(Match left, Match right, Vector3Int commandCell)
    {
        if (left.Cost != right.Cost)
        {
            return left.Cost < right.Cost;
        }

        int leftDistance = GetChebyshevDistance(left.Candidate.Cell, commandCell);
        int rightDistance = GetChebyshevDistance(right.Candidate.Cell, commandCell);

        if (leftDistance != rightDistance)
        {
            return leftDistance < rightDistance;
        }

        int cellComparison = CompareCells(left.Candidate.Cell, right.Candidate.Cell);
        return cellComparison != 0
            ? cellComparison < 0
            : left.Unit.Order < right.Unit.Order;
    }

    // 실제 타일이면서 다른 오브젝트가 점유하거나 예약하지 않은 목적지인지 확인
    private bool IsAvailableDestination(Vector3Int cell)
    {
        return occupancyManager.CoordinateManager != null &&
               occupancyManager.CoordinateManager.HasTile(cell) &&
               !occupancyManager.HasOccupant(cell) &&
               !occupancyManager.IsReserved(cell);
    }

    // 대각선 이동을 포함한 사각 링의 반경 계산
    private static int GetChebyshevDistance(Vector3Int left, Vector3Int right)
    {
        return Mathf.Max(Mathf.Abs(left.x - right.x), Mathf.Abs(left.y - right.y));
    }

    // 결과가 항상 같도록 셀의 X좌표와 Y좌표 순으로 비교
    private static int CompareCells(Vector3Int left, Vector3Int right)
    {
        int x = left.x.CompareTo(right.x);
        return x != 0 ? x : left.y.CompareTo(right.y);
    }

    // 유닛의 이동 컴포넌트, 현재 셀, 원래 선택 순서를 묶은 배정용 정보
    private sealed class UnitInfo
    {
        public readonly UnitMovement Movement;
        public readonly TileObjectPlacement Placement;
        public readonly UnitCore Core;
        public readonly Vector3Int Cell;
        public readonly int Order;

        public UnitInfo(
            UnitMovement movement,
            TileObjectPlacement placement,
            UnitCore core,
            Vector3Int cell,
            int order)
        {
            Movement = movement;
            Placement = placement;
            Core = core;
            Cell = cell;
            Order = order;
        }
    }

    // 목적지 후보와 그 셀에서 각 셀까지 계산한 최단 이동 비용
    private sealed class CandidateInfo
    {
        public readonly Vector3Int Cell;
        public readonly Dictionary<Vector3Int, int> Distances;

        public CandidateInfo(
            Vector3Int cell,
            Dictionary<Vector3Int, int> distances)
        {
            Cell = cell;
            Distances = distances;
        }
    }

    // 최종적으로 확정된 유닛과 목적지의 조합
    private sealed class Assignment
    {
        public readonly UnitInfo Unit;
        public readonly Vector3Int Cell;

        public Assignment(UnitInfo unit, Vector3Int cell)
        {
            Unit = unit;
            Cell = cell;
        }
    }

    // 배정 후보 비교에 사용하는 유닛, 목적지 후보, 이동 비용의 조합
    private sealed class Match
    {
        public readonly UnitInfo Unit;
        public readonly CandidateInfo Candidate;
        public readonly int Cost;

        public Match(UnitInfo unit, CandidateInfo candidate, int cost)
        {
            Unit = unit;
            Candidate = candidate;
            Cost = cost;
        }
    }
}
