using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitStressBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private UnitStress unitStress;

    [SerializeField]
    private Transform fillTransform;

    [SerializeField]
    private SpriteRenderer backgroundRenderer;

    [SerializeField]
    private SpriteRenderer fillRenderer;

    [Header("Size")]
    [SerializeField]
    [Min(0.001f)]
    private float width = 1f;

    [SerializeField]
    [Min(0.001f)]
    private float height = 0.08f;

    private Vector3 fillBaseLocalPosition;

    private bool initialized;

    private void Awake()
    {
        ResolveReferences();

        if (fillTransform != null)
        {
            fillBaseLocalPosition =
                fillTransform.localPosition;
        }

        initialized = true;
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (unitStress != null)
        {
            unitStress.StressChanged +=
                OnStressChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (unitStress != null)
        {
            unitStress.StressChanged -=
                OnStressChanged;
        }
    }

    private void Start()
    {
        Refresh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        width =
            Mathf.Max(
                0.001f,
                width);

        height =
            Mathf.Max(
                0.001f,
                height);

        ResolveReferences();

        if (!Application.isPlaying)
        {
            if (backgroundRenderer != null)
            {
                backgroundRenderer.transform
                    .localScale =
                    new Vector3(
                        width,
                        height,
                        1f);
            }

            if (fillTransform != null)
            {
                fillTransform.localScale =
                    new Vector3(
                        width,
                        height,
                        1f);
            }
        }
    }
#endif

    private void ResolveReferences()
    {
        if (unitStress == null)
        {
            unitStress =
                GetComponentInParent<
                    UnitStress>();
        }

        if (fillTransform == null &&
            fillRenderer != null)
        {
            fillTransform =
                fillRenderer.transform;
        }
    }

    private void OnStressChanged(
        float currentStress,
        float maxStress)
    {
        float ratio =
            maxStress > 0f
                ? Mathf.Clamp01(
                    currentStress /
                    maxStress)
                : 0f;

        RefreshVisualOnly(
            ratio);
    }

    private void Refresh()
    {
        if (!initialized ||
            unitStress == null)
        {
            return;
        }

        RefreshVisualOnly(
            unitStress.StressRatio);
    }

    // Fill의 왼쪽 끝을 고정한 채
    // 오른쪽으로 스트레스가 차오르도록 조절
    private void RefreshVisualOnly(
        float ratio)
    {
        ratio =
            Mathf.Clamp01(
                ratio);

        if (backgroundRenderer != null)
        {
            backgroundRenderer.transform
                .localScale =
                new Vector3(
                    width,
                    height,
                    1f);
        }

        if (fillTransform == null)
        {
            return;
        }

        float fillWidth =
            width * ratio;

        fillTransform.localScale =
            new Vector3(
                fillWidth,
                height,
                1f);

        float leftOffset =
            (width - fillWidth) *
            0.5f;

        fillTransform.localPosition =
            new Vector3(
                fillBaseLocalPosition.x -
                leftOffset,
                fillBaseLocalPosition.y,
                fillBaseLocalPosition.z);
    }

    // 표시 여부는 UnitStatusBarVisibility가 함께 제어
    public void SetVisible(
        bool visible)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled =
                visible;
        }

        if (fillRenderer != null)
        {
            fillRenderer.enabled =
                visible;
        }
    }
}
