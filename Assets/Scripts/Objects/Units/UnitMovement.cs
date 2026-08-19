using System.Collections.Generic;
using UnityEngine;

// A* 경로를 따라 유닛을 이동시키고 출발·목적지의 타일 점유 상태를 전환
[DisallowMultipleComponent]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class UnitMovement : MonoBehaviour
{
    [SerializeField]
    private TileOccupancyManager occupancyManager;

    private readonly List<Vector3Int> path = new();
    private UnitCore unitCore;
    private TileObjectPlacement placement;
    private TileCoordinateManager coordinateManager;
    private GridPathfinder pathfinder;
    private int waypointIndex;
    private bool hasDestinationReservation;

    public bool IsMoving { get; private set; }
    public Vector3Int DestinationCell { get; private set; }

    public Vector3Int CurrentCommandCell
    {
        get
        {
            if (coordinateManager == null || placement == null)
            {
                return default;
            }

            return IsMoving
                ? coordinateManager.WorldToCell(transform.position)
                : placement.AnchorCell;
        }
    }

    // 배치 및 타일 관리자 참조를 준비하고 경로 탐색기 생성
    private void Awake()
    {
        placement = GetComponent<TileObjectPlacement>();
        unitCore = GetComponent<UnitCore>();

        if (occupancyManager == null)
        {
            occupancyManager = TileOccupancyManager.Instance;
        }

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

    // 새 목적지를 예약하고 경로를 계산한다.
    // 이미 이동 중이라면 기존 목적지 예약과 경로를 새 명령으로 교체한다.
    public bool TryMoveTo(Vector3Int destinationCell)
    {
        if (placement == null ||
            occupancyManager == null ||
            coordinateManager == null ||
            pathfinder == null ||
            (!IsMoving && !placement.IsPlaced))
        {
            return false;
        }

        // 이미 같은 목적지로 이동 중이면 현재 이동을 그대로 사용한다.
        if (IsMoving && destinationCell == DestinationCell)
        {
            return true;
        }

        // 정지 중 현재 타일로 다시 이동할 필요는 없다.
        if (!IsMoving && destinationCell == placement.AnchorCell)
        {
            return false;
        }

        if (!IsAvailableDestination(destinationCell))
        {
            return false;
        }

        Vector3Int startCell = CurrentCommandCell;

        // 새 목적지를 먼저 확보한다.
        // 새 명령이 실패하더라도 기존 이동을 유지하기 위함이다.
        if (!occupancyManager.TryReserve(destinationCell, placement))
        {
            return false;
        }

        if (!pathfinder.TryFindPath(
                startCell,
                destinationCell,
                out List<Vector3Int> newPath))
        {
            occupancyManager.ReleaseReservation(
                destinationCell,
                placement);

            return false;
        }

        if (IsMoving)
        {
            // 새 목적지와 경로가 정상적으로 확보된 뒤
            // 기존 목적지 예약을 해제한다.
            if (hasDestinationReservation)
            {
                occupancyManager.ReleaseReservation(
                    DestinationCell,
                    placement);
            }
        }
        else
        {
            // 처음 이동을 시작하는 경우에만
            // 현재 점유 타일을 해제한다.
            if (!placement.RemoveFromTiles())
            {
                occupancyManager.ReleaseReservation(
                    destinationCell,
                    placement);

                return false;
            }
        }

        DestinationCell = destinationCell;
        hasDestinationReservation = true;

        path.Clear();
        path.AddRange(newPath);

        waypointIndex =
            path.Count > 0 && path[0] == startCell
                ? 1
                : 0;

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
        float moveSpeed =
            unitCore != null && unitCore.Data != null
                ? unitCore.Data.MoveSpeed
                : 0f;

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

        public void CancelMovement()
        {
            if (!IsMoving)
            {
                return;
            }

            if (hasDestinationReservation &&
                occupancyManager != null)
            {
                occupancyManager.ReleaseReservation(
                    DestinationCell,
                    placement);

                hasDestinationReservation = false;
            }

            path.Clear();
            waypointIndex = 0;
            IsMoving = false;

            TryPlaceAtCurrentPosition();
        }

    private void TryPlaceAtCurrentPosition()
    {
        if (placement == null ||
            coordinateManager == null ||
            occupancyManager == null)
        {
            return;
        }

        Vector3Int currentCell =
            coordinateManager.WorldToCell(
                transform.position);

        if (placement.TryPlace(currentCell))
        {
            return;
        }

        // 현재 셀이 이미 점유되어 있다면
        // 가까운 빈 셀을 찾는다.
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int candidate =
                        currentCell +
                        new Vector3Int(x, y, 0);

                    if (!coordinateManager.HasTile(candidate) ||
                        occupancyManager.HasOccupant(candidate) ||
                        occupancyManager.IsReserved(candidate))
                    {
                        continue;
                    }

                    if (placement.TryPlace(candidate))
                    {
                        return;
                    }
                }
            }
        }

        Debug.LogError(
            $"{name}: 이동 취소 후 배치할 타일을 찾지 못했습니다.",
            this);
    }
}
