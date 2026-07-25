using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TileOccupancyManager : MonoBehaviour
{
    /* -----< 해시 맵에 저장 될 타일 정보 딕셔너리들 | [(x, y), 그 타일과 관련된 오브젝트] >----- */
    private readonly Dictionary<Vector3Int, GameObject> unitOccupants = new(); // 현재 정지한 유닛
    private readonly Dictionary<Vector3Int, GameObject> facilityOccupants = new(); // 공장과 상담실 등의 시설
    private readonly Dictionary<Vector3Int, GameObject> unitReservations = new(); // 이동 중인 유닛의 목적지
    private readonly Dictionary<Vector3Int, GameObject> workAreaOwners = new(); // 이 작업 타일을 소유한 공장

    // 해당 타일에 정지한 유닛이 있는지 확인한다.
    public bool HasUnit(Vector3Int cell)
    {
        return unitOccupants.ContainsKey(cell);
    }

    // 해당 타일에 공장이나 상담실 등의 시설이 있는지 확인한다.
    public bool HasFacility(Vector3Int cell)
    {
        return facilityOccupants.ContainsKey(cell);
    }

    // 해당 타일이 유닛의 목적지로 예약되었는지 확인한다.
    public bool IsReserved(Vector3Int cell)
    {
        return unitReservations.ContainsKey(cell);
    }

    // 해당 타일이 공장의 작업 영역인지 확인한다.
    public bool IsWorkArea(Vector3Int cell)
    {
        return workAreaOwners.ContainsKey(cell);
    }

    // 해당 타일을 점유한 유닛을 가져온다.
    public bool TryGetUnit(Vector3Int cell, out GameObject unit)
    {
        return unitOccupants.TryGetValue(cell, out unit);
    }

    // 해당 타일을 점유한 시설을 가져온다.
    public bool TryGetFacility(Vector3Int cell, out GameObject facility)
    {
        return facilityOccupants.TryGetValue(cell, out facility);
    }

    // 해당 타일을 작업 영역으로 사용하는 공장을 가져온다.
    public bool TryGetWorkAreaOwner(Vector3Int cell, out GameObject factory)
    {
        return workAreaOwners.TryGetValue(cell, out factory);
    }
}
