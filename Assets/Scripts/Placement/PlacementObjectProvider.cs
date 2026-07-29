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
