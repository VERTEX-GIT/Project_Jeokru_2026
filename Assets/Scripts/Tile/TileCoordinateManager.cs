using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TileCoordinateManager : MonoBehaviour
{
    private Tilemap tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    private void Start()
    {
        PrintAllTileCoordinates();
    }

    public Vector3Int GetTilePosition(Vector3 worldPosition)
    {
        EnsureTilemap();
        return tilemap.WorldToCell(worldPosition);
    }

    public Vector3 GetWorldPosition(Vector3Int tilePosition)
    {
        EnsureTilemap();
        return tilemap.GetCellCenterWorld(tilePosition);
    }

    [ContextMenu("Print All Tile Coordinates")]
    public void PrintAllTileCoordinates()
    {
        EnsureTilemap();

        foreach (Vector3Int tilePosition in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(tilePosition))
            {
                continue;
            }

            Vector3 worldPosition = tilemap.GetCellCenterWorld(tilePosition);
            Debug.Log($"Tile: {tilePosition}, World: {worldPosition}", this);
        }
    }

    private void EnsureTilemap()
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }
    }
}
