using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class UnitTargetFollower : MonoBehaviour
{
    private UnitCore unitCore;
    private UnitMovement movement;
    private TileObjectPlacement placement;

    private TileOccupancyManager occupancyManager;
    private TileCoordinateManager coordinateManager;
    private GridPathfinder pathfinder;

    private GameObject lastTarget;
    private Vector3Int lastTargetReferenceCell;
    private bool hasLastTargetReferenceCell;

    private void Awake()
    {
        unitCore = GetComponent<UnitCore>();
        movement = GetComponent<UnitMovement>();
        placement = GetComponent<TileObjectPlacement>();

        occupancyManager = TileOccupancyManager.Instance;

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<TileOccupancyManager>();
        }

        if (occupancyManager != null)
        {
            coordinateManager =
                occupancyManager.CoordinateManager;

            pathfinder =
                new GridPathfinder(occupancyManager);
        }
    }

    private void Update()
    {
        if (!CanFollowTarget())
        {
            ClearTargetCache();
            return;
        }

        GameObject target = unitCore.CurrentTarget;

        if (target == null)
        {
            ClearTargetCache();
            return;
        }

        bool targetChanged = target != lastTarget;

        // 공장은 움직이지 않으므로
        // 타겟이 새 공장으로 변경됐을 때만 목적지를 계산한다.
        if (target.TryGetComponent(out FactoryCore factory) &&
            factory.TryGetComponent(
                out TileObjectPlacement factoryPlacement))
        {
            if (!targetChanged)
            {
                return;
            }

            if (TryUpdateFactoryDestination(factoryPlacement))
            {
                lastTarget = target;
                hasLastTargetReferenceCell = false;
            }

            return;
        }

        // 유닛 타겟
        if (!TryGetTargetReferenceCell(
                out Vector3Int targetCell))
        {
            return;
        }

        // 같은 유닛이고 최종 목적지 타일도 같다면
        // 이동 중/도착 후 모두 재계산할 필요가 없다.
        if (!targetChanged &&
            hasLastTargetReferenceCell &&
            targetCell == lastTargetReferenceCell)
        {
            return;
        }

        if (TryUpdateDestination(targetCell))
        {
            lastTarget = target;
            lastTargetReferenceCell = targetCell;
            hasLastTargetReferenceCell = true;
        }
    }

    private bool CanFollowTarget()
    {
        return unitCore != null &&
            unitCore.IsActive &&
            unitCore.Data != null &&
            unitCore.CurrentTarget != null &&
            movement != null &&
            placement != null &&
            occupancyManager != null &&
            coordinateManager != null &&
            pathfinder != null &&
            IsValidCombatTarget();
    }

    private bool IsValidCombatTarget()
    {
        GameObject target = unitCore.CurrentTarget;

        if (target == null)
            return false;

        if (target.TryGetComponent(out UnitCore targetUnit))
        {
            return targetUnit.IsActive &&
                targetUnit.Data != null &&
                targetUnit.Data.Team != unitCore.Data.Team;
        }

        // 공장은 적군만 추적 가능
        if (unitCore.Data.Team == UnitTeam.Enemy &&
            target.TryGetComponent(out FactoryHealth factoryHealth))
        {
            return factoryHealth.IsAlive;
        }

        return false;
    }

    // 타겟의 추적 기준 타일 계산
    private bool TryGetTargetReferenceCell(
        out Vector3Int cell)
    {
        cell = default;

        GameObject target =
            unitCore.CurrentTarget;

        if (target == null)
        {
            return false;
        }

        if (target.TryGetComponent(
                out UnitCore targetCore) &&
            target.TryGetComponent(
                out UnitMovement targetMovement) &&
            target.TryGetComponent(
                out TileObjectPlacement targetPlacement))
        {
            if (!targetMovement.IsMoving)
            {
                cell = targetPlacement.AnchorCell;
                return true;
            }

            // 타겟도 전투 중이라면 서로의 DestinationCell을
            // 참조하는 순환을 막기 위해 현재 위치 타일을 사용한다.
            if (targetCore.CurrentTarget != null)
            {
                cell = coordinateManager.WorldToCell(
                    target.transform.position);

                return true;
            }

            // 일반 이동 중인 타겟은 기존 규칙대로
            // 최종 목적지 타일을 기준으로 추적한다.
            cell = targetMovement.DestinationCell;

            return true;
        }

        return false;
    }

    private bool TryUpdateDestination(
        Vector3Int targetCell)
    {
        float preferredDistance =
            unitCore.Data.PreferredDistance;

        List<Vector3Int> candidates =
            CollectDistanceCandidates(
                targetCell,
                preferredDistance);

        if (candidates.Count == 0)
        {
            return false;
        }

        // 이동 중 유지거리 후보 타일에 들어왔다면
        // 그 타일을 최종 목적지로 확정한다.
        if (movement.IsMoving)
        {
            Vector3Int currentCell =
                coordinateManager.WorldToCell(
                    transform.position);

            if (candidates.Contains(currentCell))
            {
                if (movement.DestinationCell ==
                    currentCell)
                {
                    return true;
                }

                return movement.TryMoveTo(
                    currentCell);
            }
        }

        // 이미 유지거리 타일 중앙에 정지해 있다면 유지
        if (!movement.IsMoving &&
            placement.IsPlaced &&
            candidates.Contains(
                placement.AnchorCell))
        {
            return true;
        }

        return TryMoveToBestCandidate(
            candidates);
    }

    private bool TryMoveToBestCandidate(
        List<Vector3Int> candidates)
    {
        Vector3Int startCell =
            GetCurrentReferenceCell();

        // 현재 위치에서 맵 전체 거리 계산은 딱 한 번만 한다.
        Dictionary<Vector3Int, int> distances =
            pathfinder.BuildDistanceMap(startCell);

        if (distances.Count == 0)
        {
            return false;
        }

        bool found = false;
        Vector3Int bestCell = default;
        int bestCost = int.MaxValue;

        foreach (Vector3Int candidate in candidates)
        {
            if (!CanUseCandidate(candidate))
            {
                continue;
            }

            if (!distances.TryGetValue(
                    candidate,
                    out int cost))
            {
                continue;
            }

            if (cost >= bestCost)
            {
                continue;
            }

            bestCost = cost;
            bestCell = candidate;
            found = true;
        }

        if (!found)
        {
            return false;
        }

        if (movement.IsMoving &&
            movement.DestinationCell == bestCell)
        {
            return true;
        }

        return movement.TryMoveTo(bestCell);
    }

    // 공장 전체 점유 영역을 기준으로 유지거리 후보 계산
    private bool TryUpdateFactoryDestination(
        TileObjectPlacement factoryPlacement)
    {
        if (factoryPlacement == null ||
            factoryPlacement.OccupiedCells == null ||
            factoryPlacement.OccupiedCells.Count == 0)
        {
            return false;
        }

        float preferredDistance =
            unitCore.Data.PreferredDistance;

        List<Vector3Int> candidates =
            CollectFactoryDistanceCandidates(
                factoryPlacement,
                preferredDistance);

        if (candidates.Count == 0)
        {
            return false;
        }

        // 이미 공장 기준 올바른 유지거리 타일에 서 있으면 그대로 정지
        if (!movement.IsMoving &&
            placement.IsPlaced &&
            candidates.Contains(placement.AnchorCell))
        {
            return true;
        }

        return TryMoveToBestCandidate(candidates);
    }

    // 공장의 모든 점유 타일을 기준으로 유지거리 후보를 만든다.
    private List<Vector3Int> CollectFactoryDistanceCandidates(
        TileObjectPlacement factoryPlacement,
        float radius)
    {
        HashSet<Vector3Int> candidates = new();
        HashSet<Vector3Int> occupiedCells =
            new(factoryPlacement.OccupiedCells);

        foreach (Vector3Int occupiedCell
                in factoryPlacement.OccupiedCells)
        {
            List<Vector3Int> aroundCell =
                CollectDistanceCandidates(
                    occupiedCell,
                    radius);

            foreach (Vector3Int candidate in aroundCell)
            {
                // 공장 내부는 목적지가 될 수 없음
                if (occupiedCells.Contains(candidate))
                {
                    continue;
                }

                candidates.Add(candidate);
            }
        }

        return new List<Vector3Int>(candidates);
    }

    // 현재 추적 유닛이 경로 계산을 시작할 기준 타일
    private Vector3Int GetCurrentReferenceCell()
    {
        if (movement.IsMoving)
        {
            return coordinateManager.WorldToCell(
                transform.position);
        }

        return placement.AnchorCell;
    }

    // preferredDistance 반지름의 원 둘레와 닿는 타일 계산
    private List<Vector3Int> CollectDistanceCandidates(
        Vector3Int center,
        float radius)
    {
        List<Vector3Int> result = new();

        if (radius <= 0f)
        {
            result.Add(center);
            return result;
        }

        int searchRadius =
            Mathf.CeilToInt(radius + 1f);

        float radiusSqr =
            radius * radius;

        for (int x = -searchRadius;
             x <= searchRadius;
             x++)
        {
            for (int y = -searchRadius;
                 y <= searchRadius;
                 y++)
            {
                Vector3Int cell =
                    center +
                    new Vector3Int(x, y, 0);

                if (!coordinateManager.HasTile(cell))
                {
                    continue;
                }

                // 타일은 중심에서 ±0.5 크기의 사각형이다.
                float minX =
                    Mathf.Max(
                        Mathf.Abs(x) - 0.5f,
                        0f);

                float minY =
                    Mathf.Max(
                        Mathf.Abs(y) - 0.5f,
                        0f);

                float maxX =
                    Mathf.Abs(x) + 0.5f;

                float maxY =
                    Mathf.Abs(y) + 0.5f;

                float minDistanceSqr =
                    minX * minX +
                    minY * minY;

                float maxDistanceSqr =
                    maxX * maxX +
                    maxY * maxY;

                // 원 둘레가 타일 사각형을 통과하는 경우
                if (minDistanceSqr <= radiusSqr &&
                    maxDistanceSqr >= radiusSqr)
                {
                    result.Add(cell);
                }
            }
        }

        return result;
    }

    private bool CanUseCandidate(
        Vector3Int cell)
    {
        if (!coordinateManager.HasTile(cell))
        {
            return false;
        }

        // 현재 내가 점유 중인 타일
        if (!movement.IsMoving &&
            placement.IsPlaced &&
            placement.AnchorCell == cell)
        {
            return true;
        }

        // 현재 내가 예약한 목적지
        if (movement.IsMoving &&
            movement.DestinationCell == cell)
        {
            return true;
        }

        return !occupancyManager.HasOccupant(cell) &&
            !occupancyManager.IsReserved(cell);
    }

    private void ClearTargetCache()
    {
        lastTarget = null;
        hasLastTargetReferenceCell = false;
    }
}