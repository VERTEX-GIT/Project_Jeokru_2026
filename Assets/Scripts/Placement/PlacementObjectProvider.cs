using UnityEngine;

// 배치 모드에 대응하는 프리팹을 생성해 제공
[DisallowMultipleComponent]
public sealed class PlacementObjectProvider : MonoBehaviour
{
    [Header("Placement Prefabs")]
    [SerializeField]
    private TileObjectPlacement unitPrefab;
    [SerializeField]
    private TileObjectPlacement factoryPrefab;

    public FactoryDefinition FactoryDefinition =>
        factoryPrefab != null && factoryPrefab.TryGetComponent<FactoryCore>(out var factory)
            ? factory.Definition : null;

    public bool SelectFactory(TileObjectPlacement prefab)
    {
        // 미리보기와 배치 검사는 기존 3×3 공장 규격을 사용한다.
        if (prefab == null || prefab.ObjectType != TileObjectType.Facility ||
            prefab.Size != new Vector2Int(3, 3) ||
            !prefab.TryGetComponent<FactoryCore>(out var factory) ||
            factory.Definition == null)
        {
            Debug.LogError("FactoryDefinition이 연결된 3×3 공장 프리팹이 필요합니다.", this);
            return false;
        }

        factoryPrefab = prefab;
        return true;
    }

    // 지정한 배치 모드의 프리팹 인스턴스 생성
    public TileObjectPlacement Create(PlacementMode mode)
    {
        TileObjectPlacement sourcePrefab = mode switch
        {
            PlacementMode.Unit => unitPrefab,
            PlacementMode.Factory => factoryPrefab,
            _ => null
        };

        if (sourcePrefab == null)
        {
            Debug.LogError(
                $"{name}: {mode} 배치 프리팹이 연결되지 않았습니다.",
                this);

            return null;
        }

        return Instantiate(sourcePrefab);
    }
}
