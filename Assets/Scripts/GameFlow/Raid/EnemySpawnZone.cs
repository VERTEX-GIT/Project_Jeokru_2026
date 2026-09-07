using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpawnZone : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField]
    private Transform spawnTop;

    [SerializeField]
    private Transform spawnBottom;

    [Header("Map Entry Points")]
    [SerializeField]
    private Transform[] entryPoints;

    [Header("References")]
    [SerializeField]
    private TileOccupancyManager occupancyManager;

    private readonly List<int>
        candidateIndices = new();

    private void Awake()
    {
        if (occupancyManager == null)
        {
            occupancyManager =
                TileOccupancyManager.Instance;
        }

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<
                    TileOccupancyManager>();
        }
    }

    public bool TrySpawn(
        UnitData unitData,
        out UnitCore spawnedUnit)
    {
        spawnedUnit = null;

        if (unitData == null ||
            unitData.UnitPrefab == null)
        {
            Debug.LogError(
                "EnemySpawnZone: UnitData 또는 UnitPrefab이 없습니다.",
                this);

            return false;
        }

        if (occupancyManager == null ||
            occupancyManager.CoordinateManager ==
                null)
        {
            return false;
        }

        if (spawnTop == null ||
            spawnBottom == null)
        {
            Debug.LogError(
                "EnemySpawnZone: Spawn Top/Bottom이 연결되지 않았습니다.",
                this);

            return false;
        }

        Vector3 spawnPosition =
            GetRandomSpawnPosition();

        GameObject spawnedObject =
            Instantiate(
                unitData.UnitPrefab,
                spawnPosition,
                Quaternion.identity);

        TileObjectPlacement placement =
            spawnedObject.GetComponent<
                TileObjectPlacement>();

        UnitCore unitCore =
            spawnedObject.GetComponent<
                UnitCore>();

        if (placement == null ||
            unitCore == null)
        {
            Debug.LogError(
                $"{spawnedObject.name}: " +
                "필수 유닛 컴포넌트가 없습니다.",
                spawnedObject);

            Destroy(spawnedObject);

            return false;
        }

        unitCore.SetData(
            unitData);

        if (!TryReserveRandomEntry(
                placement,
                out Vector3Int entryCell))
        {
            Destroy(spawnedObject);

            return false;
        }

        UnitSpawnEntryMover entryMover =
            spawnedObject.GetComponent<
                UnitSpawnEntryMover>();

        if (entryMover == null)
        {
            entryMover =
                spawnedObject.AddComponent<
                    UnitSpawnEntryMover>();
        }

        entryMover.Initialize(
            entryCell,
            occupancyManager);

        spawnedUnit =
            unitCore;

        return true;
    }

    public bool TryMoveToEntry(
        UnitMovement movement,
        out Vector3Int entryCell)
    {
        entryCell = default;

        if (movement == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager ==
                null ||
            entryPoints == null ||
            entryPoints.Length == 0)
        {
            return false;
        }

        candidateIndices.Clear();

        for (int i = 0;
             i < entryPoints.Length;
             i++)
        {
            if (entryPoints[i] != null)
            {
                candidateIndices.Add(i);
            }
        }

        while (candidateIndices.Count > 0)
        {
            int randomListIndex =
                UnityEngine.Random.Range(
                    0,
                    candidateIndices.Count);

            int pointIndex =
                candidateIndices[
                    randomListIndex];

            candidateIndices.RemoveAt(
                randomListIndex);

            Vector3Int candidateCell =
                occupancyManager
                    .CoordinateManager
                    .WorldToCell(
                        entryPoints[
                            pointIndex]
                            .position);

            if (!movement.TryMoveTo(
                    candidateCell))
            {
                continue;
            }

            entryCell =
                candidateCell;

            return true;
        }

        return false;
    }

    public bool TryPlaceAtEntry(
        TileObjectPlacement placement)
    {
        if (placement == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager ==
                null ||
            entryPoints == null)
        {
            return false;
        }

        candidateIndices.Clear();

        for (int i = 0;
             i < entryPoints.Length;
             i++)
        {
            if (entryPoints[i] != null)
            {
                candidateIndices.Add(i);
            }
        }

        while (candidateIndices.Count > 0)
        {
            int randomListIndex =
                UnityEngine.Random.Range(
                    0,
                    candidateIndices.Count);

            int pointIndex =
                candidateIndices[
                    randomListIndex];

            candidateIndices.RemoveAt(
                randomListIndex);

            Vector3Int candidateCell =
                occupancyManager
                    .CoordinateManager
                    .WorldToCell(
                        entryPoints[
                            pointIndex]
                            .position);

            if (!placement.TryPlace(
                    candidateCell))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public Vector3 GetRetreatDestination(
        Vector3 currentPosition)
    {
        float minY =
            Mathf.Min(
                spawnBottom.position.y,
                spawnTop.position.y);

        float maxY =
            Mathf.Max(
                spawnBottom.position.y,
                spawnTop.position.y);

        float y =
            Mathf.Clamp(
                currentPosition.y,
                minY,
                maxY);

        float x =
            (spawnBottom.position.x +
             spawnTop.position.x) *
            0.5f;

        return new Vector3(
            x,
            y,
            currentPosition.z);
    }

    private bool TryReserveRandomEntry(
        TileObjectPlacement placement,
        out Vector3Int entryCell)
    {
        entryCell = default;

        if (entryPoints == null ||
            entryPoints.Length == 0)
        {
            return false;
        }

        candidateIndices.Clear();

        for (int i = 0;
             i < entryPoints.Length;
             i++)
        {
            if (entryPoints[i] != null)
            {
                candidateIndices.Add(i);
            }
        }

        while (candidateIndices.Count > 0)
        {
            int randomListIndex =
                UnityEngine.Random.Range(
                    0,
                    candidateIndices.Count);

            int pointIndex =
                candidateIndices[
                    randomListIndex];

            candidateIndices.RemoveAt(
                randomListIndex);

            Vector3Int candidateCell =
                occupancyManager
                    .CoordinateManager
                    .WorldToCell(
                        entryPoints[
                            pointIndex]
                            .position);

            if (!occupancyManager.TryReserve(
                    candidateCell,
                    placement))
            {
                continue;
            }

            entryCell =
                candidateCell;

            return true;
        }

        return false;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float minY =
            Mathf.Min(
                spawnBottom.position.y,
                spawnTop.position.y);

        float maxY =
            Mathf.Max(
                spawnBottom.position.y,
                spawnTop.position.y);

        float y =
            UnityEngine.Random.Range(
                minY,
                maxY);

        float x =
            (spawnBottom.position.x +
             spawnTop.position.x) *
            0.5f;

        return new Vector3(
            x,
            y,
            transform.position.z);
    }
}