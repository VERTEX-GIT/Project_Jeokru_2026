using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AllySpawnZone : MonoBehaviour
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
        ResolveReferences();
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
                "AllySpawnZone: UnitData 또는 UnitPrefab이 없습니다.",
                this);

            return false;
        }

        if (unitData.Team !=
            UnitTeam.Ally)
        {
            Debug.LogError(
                "AllySpawnZone: 아군 UnitData만 생성할 수 있습니다.",
                this);

            return false;
        }

        ResolveReferences();

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
                "AllySpawnZone: Spawn Top/Bottom이 연결되지 않았습니다.",
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

        // 동적 생성 유닛은 SceneTilePlacementInitializer의
        // Start 자동 배치를 사용하지 않고 진입 지점에서 등록한다.
        SceneTilePlacementInitializer
            sceneInitializer =
                spawnedObject.GetComponent<
                    SceneTilePlacementInitializer>();

        if (sceneInitializer != null)
        {
            sceneInitializer.enabled =
                false;
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

    private void ResolveReferences()
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

    private bool TryReserveRandomEntry(
        TileObjectPlacement placement,
        out Vector3Int entryCell)
    {
        entryCell = default;

        if (entryPoints == null ||
            entryPoints.Length == 0 ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager ==
                null)
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
                Random.Range(
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
            Random.Range(
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
