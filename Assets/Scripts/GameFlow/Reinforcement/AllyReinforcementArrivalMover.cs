using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class AllyReinforcementArrivalMover : MonoBehaviour
{
    private const int FallbackSearchRadius = 3;

    private UnitCore unitCore;
    private UnitMovement movement;
    private TileObjectPlacement placement;
    private TileOccupancyManager occupancyManager;

    private Transform[] rallyPoints;
    private bool isJoining;

    public void Initialize(
        Transform[] targetRallyPoints,
        TileOccupancyManager manager)
    {
        rallyPoints = targetRallyPoints;
        occupancyManager = manager;

        unitCore = GetComponent<UnitCore>();
        movement = GetComponent<UnitMovement>();
        placement = GetComponent<TileObjectPlacement>();

        enabled = false;
    }

    public void BeginArrival()
    {
        if (GameplayPauseController.IsPaused)
        {
            return;
        }

        if (unitCore == null ||
            movement == null ||
            placement == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager == null ||
            !placement.IsPlaced)
        {
            ActivateAtCurrentPosition();
            return;
        }

        unitCore.SetUnitActive(false);
        unitCore.SetAutoCombat(false);
        unitCore.SetPlayerMoveCommandActive(false);
        unitCore.ClearTarget();

        if (!TryMoveToRally())
        {
            ActivateAtCurrentPosition();
            return;
        }

        isJoining = true;
        enabled = true;
    }

    private void Update()
    {
        if (GameplayPauseController.IsPaused ||
            !isJoining)
        {
            return;
        }

        if (!movement.IsMoving)
        {
            CompleteArrival();
        }
    }

    private bool TryMoveToRally()
    {
        List<Vector3Int> rallyCells =
            CollectRallyCells();

        rallyCells.Sort(
            (left, right) =>
                GetDistanceSquared(left).CompareTo(
                    GetDistanceSquared(right)));

        foreach (Vector3Int cell in rallyCells)
        {
            if (movement.TryMoveTo(cell))
            {
                return true;
            }
        }

        foreach (Vector3Int rallyCell in rallyCells)
        {
            if (TryMoveToNearbyCell(rallyCell))
            {
                return true;
            }
        }

        return false;
    }

    private List<Vector3Int> CollectRallyCells()
    {
        List<Vector3Int> result = new();

        if (rallyPoints == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager == null)
        {
            return result;
        }

        foreach (Transform rallyPoint in rallyPoints)
        {
            if (rallyPoint == null)
            {
                continue;
            }

            Vector3Int cell =
                occupancyManager.CoordinateManager
                    .WorldToCell(rallyPoint.position);

            if (!occupancyManager.CoordinateManager
                    .HasTile(cell) ||
                result.Contains(cell))
            {
                continue;
            }

            result.Add(cell);
        }

        return result;
    }

    private bool TryMoveToNearbyCell(
        Vector3Int center)
    {
        for (int radius = 1;
             radius <= FallbackSearchRadius;
             radius++)
        {
            for (int x = -radius;
                 x <= radius;
                 x++)
            {
                for (int y = -radius;
                     y <= radius;
                     y++)
                {
                    if (Mathf.Abs(x) != radius &&
                        Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    Vector3Int candidate =
                        center +
                        new Vector3Int(x, y, 0);

                    if (movement.TryMoveTo(candidate))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private int GetDistanceSquared(
        Vector3Int cell)
    {
        Vector3Int currentCell =
            placement.IsPlaced
                ? placement.AnchorCell
                : occupancyManager.CoordinateManager
                    .WorldToCell(transform.position);

        Vector3Int difference =
            cell - currentCell;

        return
            difference.x * difference.x +
            difference.y * difference.y;
    }

    private void CompleteArrival()
    {
        isJoining = false;

        unitCore.SetUnitActive(true);
        unitCore.SetAutoCombat(true);

        enabled = false;
    }

    private void ActivateAtCurrentPosition()
    {
        isJoining = false;

        if (unitCore != null)
        {
            unitCore.SetUnitActive(true);
            unitCore.SetAutoCombat(true);
        }

        enabled = false;
    }
}
