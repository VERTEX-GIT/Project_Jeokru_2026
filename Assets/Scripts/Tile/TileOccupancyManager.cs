using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TileOccupancyManager : MonoBehaviour
{
    public static TileOccupancyManager Instance { get; private set; }   // 싱글톤

    [SerializeField]
    private TileCoordinateManager coordinateManager;                    // TileCoordinateManager.cs

    /* --- 해시맵 --- */
    private readonly Dictionary<Vector3Int, TileObjectPlacement> occupants = new();         // 현재 타일을 점유한 오브젝트
    private readonly Dictionary<Vector3Int, TileObjectPlacement> unitReservations = new();  // 추후 이동에 사용할 유닛 목적지
    private readonly Dictionary<Vector3Int, FactoryWorkArea> workAreaOwners = new();        // 각 작업 타일을 소유한 공장

    public TileCoordinateManager CoordinateManager => coordinateManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("TileOccupancyManager가 씬에 두 개 이상 존재합니다.", this);
            enabled = false;

            return;
        }

        Instance = this;

        if (coordinateManager == null)
        {
            coordinateManager = GetComponentInChildren<TileCoordinateManager>(); // Grid 자식의 Tilemap에서 자동 탐색
        }

        if (coordinateManager == null)
        {
            Debug.LogError("TileCoordinateManager를 찾을 수 없습니다.", this);
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 오브젝트 배치 가능 여부 검사
    public bool CanOccupy(TileObjectPlacement occupant, List<Vector3Int> cells)
    {
        if (coordinateManager == null || occupant == null || cells == null || cells.Count == 0)
        {
            return false;
        }

        HashSet<Vector3Int> checkedCells = new(); // 전달된 좌표의 중복 등록 방지

        foreach (Vector3Int cell in cells)
        {
            if (!checkedCells.Add(cell) || !coordinateManager.HasTile(cell) || occupants.ContainsKey(cell) || unitReservations.ContainsKey(cell))
            {
                return false;
            }

            if (occupant.ObjectType == TileObjectType.Facility && workAreaOwners.ContainsKey(cell))
            {
                return false;
            }
        }

        return true;
    }

    // occupants에 타일 정보 등록
    public bool TryOccupy(TileObjectPlacement occupant, List<Vector3Int> cells)
    {
        if (!CanOccupy(occupant, cells))
        {
            return false;
        }

        foreach (Vector3Int cell in cells)
        {
            occupants.Add(cell, occupant);
        }

        return true;
    }

    // 지정한 오브젝트 occupants 등록 내용 제거
    public void ReleaseOccupancy(TileObjectPlacement occupant, List<Vector3Int> cells)
    {
        if (occupant == null || cells == null)
        {
            return;
        }

        foreach (Vector3Int cell in cells)
        {
            if (occupants.TryGetValue(cell, out TileObjectPlacement currentOccupant) && currentOccupant == occupant)
            {
                occupants.Remove(cell);
            }
        }
    }

    // 공장의 모든 '작업 타일'이 등록 가능한지 검사
    public bool CanRegisterWorkArea(FactoryWorkArea owner, List<Vector3Int> cells)
    {
        if (coordinateManager == null || owner == null || cells == null)
        {
            return false;
        }

        HashSet<Vector3Int> checkedCells = new(); // 전달된 좌표의 중복 등록 방지

        foreach (Vector3Int cell in cells)
        {
            if (!checkedCells.Add(cell) || !coordinateManager.HasTile(cell) || workAreaOwners.ContainsKey(cell))
            {
                return false;
            }
        }

        return true;
    }

    // 작업 타일 FactoryWorkArea에 등록
    public bool TryRegisterWorkArea(FactoryWorkArea owner, List<Vector3Int> cells)
    {
        if (!CanRegisterWorkArea(owner, cells))
        {
            return false;
        }

        foreach (Vector3Int cell in cells)
        {
            workAreaOwners.Add(cell, owner);
        }

        return true;
    }

    // 지정된 공장 작업 타일 정보 제거
    public void ReleaseWorkArea(FactoryWorkArea owner, List<Vector3Int> cells)
    {
        if (owner == null || cells == null)
        {
            return;
        }

        foreach (Vector3Int cell in cells)
        {
            if (workAreaOwners.TryGetValue(cell, out FactoryWorkArea currentOwner) &&
                currentOwner == owner)
            {
                workAreaOwners.Remove(cell);
            }
        }
    }

    // 해당 타일에 오브젝트 유무 확인
    public bool HasOccupant(Vector3Int cell)
    {
        return occupants.ContainsKey(cell);
    }

    // 경로 탐색에서 고정 장애물로 취급할 시설이 있는지 확인
    public bool HasFacility(Vector3Int cell)
    {
        return occupants.TryGetValue(cell, out TileObjectPlacement occupant) &&
               occupant != null &&
               occupant.ObjectType == TileObjectType.Facility;
    }

    // 유닛의 이동 목적지 예약
    public bool TryReserve(
        Vector3Int cell,
        TileObjectPlacement unit)
    {
        if (coordinateManager == null ||
            unit == null ||
            unit.ObjectType != TileObjectType.Unit)
        {
            return false;
        }

        if (!coordinateManager.HasTile(cell) ||
            occupants.ContainsKey(cell) ||
            unitReservations.ContainsKey(cell))
        {
            return false;
        }

        unitReservations.Add(cell, unit);
        return true;
    }

    // 해당 유닛이 소유한 목적지 예약 해제
    public void ReleaseReservation(
        Vector3Int cell,
        TileObjectPlacement unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unitReservations.TryGetValue(
                cell,
                out TileObjectPlacement currentUnit) &&
            currentUnit == unit)
        {
            unitReservations.Remove(cell);
        }
    }

    // 해당 타일이 유닛의 목적지로 예약되었는지 확인
    public bool IsReserved(Vector3Int cell)
    {
        return unitReservations.ContainsKey(cell);
    }

    // 해당 타일이 공장의 작업 영역인지 확인
    public bool IsWorkArea(Vector3Int cell)
    {
        return workAreaOwners.ContainsKey(cell);
    }

    // 해당 타일을 점유한 오브젝트 정보 가져오기
    public bool TryGetOccupant(Vector3Int cell, out TileObjectPlacement occupant)
    {
        return occupants.TryGetValue(cell, out occupant);
    }

    // 해당 타일을 작업 영역으로 사용하는 공장을 가져오기
    public bool TryGetWorkAreaOwner(Vector3Int cell, out FactoryWorkArea owner)
    {
        return workAreaOwners.TryGetValue(cell, out owner);
    }
}
