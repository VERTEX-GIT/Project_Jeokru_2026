using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class FixedTilemapCamera : MonoBehaviour
{
    [SerializeField]
    private Tilemap targetTilemap;

    [SerializeField]
    private Vector2Int visibleTileCount = new Vector2Int(32, 18);

    [SerializeField]
    private Vector2Int targetAspectRatio = new Vector2Int(16, 9);

    private Camera targetCamera;
    private Vector3 fixedPosition;
    private int previousRenderWidth;
    private int previousRenderHeight;

    private void Awake()
    {
        ApplyCameraSettings();
    }

    private void OnEnable()
    {
        ApplyCameraSettings();
    }

    private void LateUpdate()
    {
        transform.position = fixedPosition;

        GetFullRenderSize(out int renderWidth, out int renderHeight);

        if (renderWidth != previousRenderWidth ||
            renderHeight != previousRenderHeight)
        {
            ApplyLetterbox();
        }
    }

    public void ApplyCameraSettings()
    {
        targetCamera = GetComponent<Camera>();

        if (targetTilemap == null)
        {
            Debug.LogError("Target Tilemap is not assigned.", this);
            enabled = false;
            return;
        }

        targetCamera.orthographic = true;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;

        float tileWorldHeight =
            targetTilemap.layoutGrid.cellSize.y *
            Mathf.Abs(targetTilemap.transform.lossyScale.y);

        targetCamera.orthographicSize =
            visibleTileCount.y * tileWorldHeight * 0.5f;

        Vector3 mapWorldCenter =
            targetTilemap.transform.TransformPoint(
                targetTilemap.localBounds.center
            );

        fixedPosition = new Vector3(
            mapWorldCenter.x,
            mapWorldCenter.y,
            transform.position.z
        );

        transform.position = fixedPosition;
        ApplyLetterbox();
    }

    private void ApplyLetterbox()
    {
        GetFullRenderSize(out int renderWidth, out int renderHeight);

        previousRenderWidth = renderWidth;
        previousRenderHeight = renderHeight;

        if (renderWidth <= 0 || renderHeight <= 0)
        {
            return;
        }

        float targetAspect =
            (float)targetAspectRatio.x / targetAspectRatio.y;

        float currentAspect =
            (float)renderWidth / renderHeight;

        if (currentAspect > targetAspect)
        {
            float viewportWidth = targetAspect / currentAspect;
            float viewportX = (1f - viewportWidth) * 0.5f;

            targetCamera.rect = new Rect(
                viewportX,
                0f,
                viewportWidth,
                1f
            );
        }
        else
        {
            float viewportHeight = currentAspect / targetAspect;
            float viewportY = (1f - viewportHeight) * 0.5f;

            targetCamera.rect = new Rect(
                0f,
                viewportY,
                1f,
                viewportHeight
            );
        }

        targetCamera.aspect = targetAspect;
    }

    private void GetFullRenderSize(
        out int renderWidth,
        out int renderHeight)
    {
        Rect viewport = targetCamera.rect;

        renderWidth = Mathf.RoundToInt(
            targetCamera.pixelWidth /
            Mathf.Max(viewport.width, Mathf.Epsilon)
        );

        renderHeight = Mathf.RoundToInt(
            targetCamera.pixelHeight /
            Mathf.Max(viewport.height, Mathf.Epsilon)
        );
    }
}
