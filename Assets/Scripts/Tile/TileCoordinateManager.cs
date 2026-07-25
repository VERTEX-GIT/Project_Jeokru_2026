using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class TileCoordinateManager : MonoBehaviour
{
    private Tilemap tilemap; // 좌표 변환에 사용하는 현재 오브젝트의 Tilemap

    // 현재 오브젝트에 연결된 Tilemap을 가져온다.
    private void Awake()
    {
        tilemap = GetComponent<Tilemap>(); // RequireComponent로 존재가 보장된다.
    }

    // 월드 좌표를 타일 좌표로 변환한다.
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return tilemap.WorldToCell(worldPosition);
    }

    // 타일 좌표를 해당 타일 중앙의 월드 좌표로 변환한다.
    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        return tilemap.GetCellCenterWorld(cell);
    }

    // 해당 좌표에 실제 타일이 존재하는지 확인한다.
    public bool HasTile(Vector3Int cell)
    {
        return tilemap.HasTile(cell);
    }
}
