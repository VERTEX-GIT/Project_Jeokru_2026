using System.Collections.Generic;
using UnityEngine;

// A* 경로를 따라 유닛을 이동시키고 출발·목적지의 타일 점유 상태를 전환
[DisallowMultipleComponent]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class UnitMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 3f;

    [SerializeField]
    private TileOccupancyManager occupancyManager;

    private readonly List<Vector3Int> path = new();
    private TileObjectPlacement placement;
    private TileCoordinateManager coordinateManager;
    private GridPathfinder pathfinder;
    private int waypointIndex;
    private bool hasDestinationReservation;

    public bool IsMoving { get; private set; }
    public Vector3Int DestinationCell { get; private set; }

    // 배치 및 타일 관리자 참조를 준비하고 경로 탐색기 생성
    private void Awake()
    {
        placement = GetComponent<TileObjectPlacement>();

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
            coordinateManager = occupancyManager.CoordinateManager;
            pathfinder = new GridPathfinder(occupancyManager);
        }
    }

    // 이동 중인 유닛을 현재 경로의 다음 웨이포인트로 이동
    private void Update()
    {
        if (IsMoving)
        {
            MoveTowardWaypoint();
        }
    }

    // 이동 도중 제거되면 해당 유닛이 보유한 목적지 예약 해제
    private void OnDestroy()
    {
        if (hasDestinationReservation && occupancyManager != null)
        {
            occupancyManager.ReleaseReservation(DestinationCell, placement);
        }
    }

    // 목적지를 예약하고 경로를 계산한 뒤 출발 셀 점유를 해제하여 이동 시작
    public bool TryMoveTo(Vector3Int destinationCell)
    {
        if (IsMoving || placement == null || occupancyManager == null ||
            coordinateManager == null || pathfinder == null || !placement.IsPlaced ||
            destinationCell == placement.AnchorCell ||
            !IsAvailableDestination(destinationCell))
        {
            return false;
        }

        Vector3Int startCell = placement.AnchorCell;

        if (!occupancyManager.TryReserve(destinationCell, placement))
        {
            return false;
        }

        DestinationCell = destinationCell;
        hasDestinationReservation = true;

        if (!pathfinder.TryFindPath(startCell, destinationCell, out List<Vector3Int> newPath))
        {
            occupancyManager.ReleaseReservation(destinationCell, placement);
            hasDestinationReservation = false;
            return false;
        }

        if (!placement.RemoveFromTiles())
        {
            occupancyManager.ReleaseReservation(destinationCell, placement);
            hasDestinationReservation = false;
            return false;
        }

        path.Clear();
        path.AddRange(newPath);
        waypointIndex = path.Count > 0 && path[0] == startCell ? 1 : 0;
        IsMoving = true;
        return true;
    }

    // 실제 타일이면서 다른 오브젝트가 점유하거나 예약하지 않은 목적지인지 확인
    private bool IsAvailableDestination(Vector3Int cell)
    {
        return coordinateManager.HasTile(cell) &&
               !occupancyManager.HasOccupant(cell) &&
               !occupancyManager.IsReserved(cell);
    }

    // 현재 웨이포인트의 타일 중앙까지 일정한 속도로 이동
    private void MoveTowardWaypoint()
    {
        if (waypointIndex >= path.Count)
        {
            CompleteMovement();
            return;
        }

        Vector3 targetPosition =
            coordinateManager.CellToWorldCenter(path[waypointIndex]);
        targetPosition.z = transform.position.z;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (transform.position != targetPosition)
        {
            return;
        }

        waypointIndex++;

        if (waypointIndex >= path.Count)
        {
            CompleteMovement();
        }
    }

    // 목적지 예약을 해제하고 도착 셀에 유닛 점유를 다시 등록
    private void CompleteMovement()
    {
        occupancyManager.ReleaseReservation(DestinationCell, placement);
        hasDestinationReservation = false;

        if (!placement.TryPlace(DestinationCell))
        {
            // 예기치 않은 점유 충돌 시에도 목적지 소유권을 가능한 한 보존한다.
            hasDestinationReservation =
                occupancyManager.TryReserve(DestinationCell, placement);
            Debug.LogError($"{name}: 이동 완료 후 {DestinationCell} 점유 등록 실패", this);
        }

        path.Clear();
        waypointIndex = 0;
        IsMoving = false;
    }
}
