using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ObjectHoverDetector
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera worldCamera;

    [Header("Detection")]
    [SerializeField]
    private LayerMask hoverLayerMask = ~0;

    public IHoverInfoProvider CurrentProvider
    {
        get;
        private set;
    }

    public Component CurrentProviderComponent
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    private void Update()
    {
        UpdateHoveredObject();
    }

    private void UpdateHoveredObject()
    {
        CurrentProvider = null;
        CurrentProviderComponent = null;

        if (worldCamera == null ||
            Mouse.current == null)
        {
            return;
        }

        Vector2 screenPosition =
            Mouse.current.position
                .ReadValue();

        float cameraDistance =
            Mathf.Abs(
                worldCamera
                    .transform
                    .position.z);

        Vector3 worldPosition =
            worldCamera
                .ScreenToWorldPoint(
                    new Vector3(
                        screenPosition.x,
                        screenPosition.y,
                        cameraDistance));

        worldPosition.z = 0f;

        Collider2D[] hits =
            Physics2D.OverlapPointAll(
                worldPosition,
                hoverLayerMask);

        TryFindProvider(
            hits,
            out IHoverInfoProvider provider,
            out Component providerComponent);

        CurrentProvider =
            provider;

        CurrentProviderComponent =
            providerComponent;
    }

    private static bool TryFindProvider(
        Collider2D[] hits,
        out IHoverInfoProvider provider,
        out Component providerComponent)
    {
        provider = null;
        providerComponent = null;

        if (hits == null ||
            hits.Length == 0)
        {
            return false;
        }

        foreach (Collider2D hit
                 in hits)
        {
            if (hit == null)
            {
                continue;
            }

            MonoBehaviour[] behaviours =
                hit.GetComponentsInParent<
                    MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour
                     in behaviours)
            {
                if (behaviour is not
                    IHoverInfoProvider
                    hoverProvider)
                {
                    continue;
                }

                provider =
                    hoverProvider;

                providerComponent =
                    behaviour;

                return true;
            }
        }

        return false;
    }
}