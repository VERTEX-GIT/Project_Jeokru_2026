using System;
using UnityEngine;

// 배치 모드와 세이브 복원에서 사용할 공장 프리팹을 제공
[DisallowMultipleComponent]
public sealed class PlacementObjectProvider : MonoBehaviour
{
    [Serializable]
    private sealed class FactoryPrefabEntry
    {
        [SerializeField]
        private FactoryType factoryType;

        [SerializeField]
        private TileObjectPlacement prefab;

        public FactoryType FactoryType =>
            factoryType;

        public TileObjectPlacement Prefab =>
            prefab;
    }

    [Header("Placement Prefab")]
    [SerializeField]
    private TileObjectPlacement factoryPrefab;

    [Header("Factory Prefabs")]
    [SerializeField]
    private FactoryPrefabEntry[] factoryPrefabs;

    public FactoryDefinition FactoryDefinition =>
        factoryPrefab != null &&
        factoryPrefab.TryGetComponent<
            FactoryCore>(
            out var factory)
            ? factory.Definition
            : null;

    public bool SelectFactory(
        TileObjectPlacement prefab)
    {
        if (!IsValidFactoryPrefab(
                prefab))
        {
            Debug.LogError(
                "FactoryDefinition이 연결된 " +
                "3×3 공장 프리팹이 필요합니다.",
                this);

            return false;
        }

        factoryPrefab =
            prefab;

        return true;
    }

    public bool TryGetFactoryPrefab(
        FactoryType factoryType,
        out TileObjectPlacement prefab)
    {
        prefab = null;

        if (factoryPrefabs != null)
        {
            foreach (FactoryPrefabEntry entry
                     in factoryPrefabs)
            {
                if (entry == null ||
                    entry.FactoryType !=
                        factoryType ||
                    !IsValidFactoryPrefab(
                        entry.Prefab))
                {
                    continue;
                }

                prefab =
                    entry.Prefab;

                return true;
            }
        }

        if (IsValidFactoryPrefab(
                factoryPrefab) &&
            factoryPrefab.TryGetComponent(
                out FactoryCore currentFactory) &&
            currentFactory.Definition
                .FactoryType ==
            factoryType)
        {
            prefab =
                factoryPrefab;

            return true;
        }

        return false;
    }

    public TileObjectPlacement Create(
        PlacementMode mode)
    {
        if (mode !=
            PlacementMode.Factory)
        {
            Debug.LogError(
                $"{name}: 지원하지 않는 " +
                $"배치 모드입니다. {mode}",
                this);

            return null;
        }

        if (!IsValidFactoryPrefab(
                factoryPrefab))
        {
            Debug.LogError(
                $"{name}: 공장 배치 프리팹이 " +
                "연결되지 않았습니다.",
                this);

            return null;
        }

        return Instantiate(
            factoryPrefab);
    }

    public TileObjectPlacement CreateFactory(
        FactoryType factoryType)
    {
        if (!TryGetFactoryPrefab(
                factoryType,
                out TileObjectPlacement prefab))
        {
            Debug.LogError(
                $"{name}: {factoryType} 공장 프리팹이 " +
                "등록되지 않았습니다.",
                this);

            return null;
        }

        return Instantiate(
            prefab);
    }

    private static bool IsValidFactoryPrefab(
        TileObjectPlacement prefab)
    {
        return prefab != null &&
            prefab.ObjectType ==
                TileObjectType.Facility &&
            prefab.Size ==
                new Vector2Int(3, 3) &&
            prefab.TryGetComponent(
                out FactoryCore factory) &&
            factory.Definition != null;
    }
}
