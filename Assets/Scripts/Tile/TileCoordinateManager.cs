using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class TileCoordinateManager : MonoBehaviour
{
    private Tilemap tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    // 월드 좌표를 타일 좌표로 변환
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return tilemap.WorldToCell(worldPosition);
    }

    // 타일 좌표를 타일 중앙의 월드 좌표로 변환
    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        return tilemap.GetCellCenterWorld(cell);
    }

    // 해당 좌표 타일이 존재하는지 확인
    public bool HasTile(Vector3Int cell)
    {
        return tilemap.HasTile(cell);
    }
}
