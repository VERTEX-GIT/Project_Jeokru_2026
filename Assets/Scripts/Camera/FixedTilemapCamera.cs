using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class FixedTilemapCamera : MonoBehaviour
{
    [SerializeField]
    private Tilemap targetTilemap;

    [SerializeField]
    private Vector2Int targetAspectRatio = new Vector2Int(16, 9);

    [SerializeField, Range(0f, 0.5f)]
    private float topBarHeightRatio = 1f / 9f;

    [SerializeField]
    private RectTransform topBar;

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

        targetTilemap.CompressBounds();
        Bounds mapBounds = targetTilemap.localBounds;
        Vector3 mapScale = targetTilemap.transform.lossyScale;
        float mapWorldWidth = mapBounds.size.x * Mathf.Abs(mapScale.x);
        float mapWorldHeight = mapBounds.size.y * Mathf.Abs(mapScale.y);
        float targetAspect = GetGameAspect();

        targetCamera.orthographicSize = Mathf.Max(0.01f, Mathf.Max(
            mapWorldHeight * 0.5f,
            mapWorldWidth / (targetAspect * 2f)));

        Vector3 mapWorldCenter =
            targetTilemap.transform.TransformPoint(
                mapBounds.center
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

        float targetAspect = GetFrameAspect();

        float currentAspect =
            (float)renderWidth / renderHeight;

        Rect frame;
        if (currentAspect > targetAspect)
        {
            float viewportWidth = targetAspect / currentAspect;
            float viewportX = (1f - viewportWidth) * 0.5f;

            frame = new Rect(
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

            frame = new Rect(
                0f,
                viewportY,
                1f,
                viewportHeight
            );
        }

        float gameHeight = frame.height * (1f - Mathf.Clamp(topBarHeightRatio, 0f, 0.5f));
        targetCamera.rect = new Rect(frame.x, frame.y, frame.width, gameHeight);
        targetCamera.aspect = GetGameAspect();

        // TopBar is a direct child of the full-screen overlay canvas.
        if (topBar != null)
        {
            topBar.anchorMin = new Vector2(frame.xMin, frame.yMin + gameHeight);
            topBar.anchorMax = new Vector2(frame.xMax, frame.yMax);
            topBar.offsetMin = Vector2.zero;
            topBar.offsetMax = Vector2.zero;
        }
    }

    private float GetFrameAspect()
    {
        return (float)Mathf.Max(1, targetAspectRatio.x) / Mathf.Max(1, targetAspectRatio.y);
    }

    private float GetGameAspect()
    {
        return GetFrameAspect() / (1f - Mathf.Clamp(topBarHeightRatio, 0f, 0.5f));
    }

    private void GetFullRenderSize(
        out int renderWidth,
        out int renderHeight)
    {
        if (targetCamera.targetTexture != null)
        {
            renderWidth = targetCamera.targetTexture.width;
            renderHeight = targetCamera.targetTexture.height;
            return;
        }

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
