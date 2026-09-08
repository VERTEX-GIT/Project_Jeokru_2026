using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private UnitHealth unitHealth;

    [SerializeField]
    private Transform fillTransform;

    [SerializeField]
    private SpriteRenderer backgroundRenderer;

    [SerializeField]
    private SpriteRenderer fillRenderer;

    [Header("Visibility")]
    [SerializeField]
    private bool hideWhenFull;

    [Header("Size")]
    [SerializeField]
    [Min(0.001f)]
    private float width = 1f;

    [SerializeField]
    [Min(0.001f)]
    private float height = 0.1f;

    private Vector3 fillBaseLocalPosition;

    private bool initialized;

    private void Awake()
    {
        if (unitHealth == null)
        {
            unitHealth =
                GetComponentInParent<
                    UnitHealth>();
        }

        if (fillTransform == null &&
            fillRenderer != null)
        {
            fillTransform =
                fillRenderer.transform;
        }

        if (fillTransform != null)
        {
            fillBaseLocalPosition =
                fillTransform.localPosition;
        }

        initialized = true;
    }

    private void OnEnable()
    {
        if (unitHealth == null)
        {
            unitHealth =
                GetComponentInParent<
                    UnitHealth>();
        }

        if (unitHealth != null)
        {
            unitHealth.HealthChanged +=
                OnHealthChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (unitHealth != null)
        {
            unitHealth.HealthChanged -=
                OnHealthChanged;
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

        if (!Application.isPlaying)
        {
            RefreshVisualOnly(
                1f);
        }
    }
#endif

    private void OnHealthChanged(
        float currentHp,
        float maxHp)
    {
        float ratio =
            maxHp > 0f
                ? Mathf.Clamp01(
                    currentHp /
                    maxHp)
                : 0f;

        ApplyHealthRatio(
            ratio);
    }

    private void Refresh()
    {
        if (!initialized)
        {
            return;
        }

        if (unitHealth == null)
        {
            SetVisible(false);
            return;
        }

        ApplyHealthRatio(
            unitHealth.HealthRatio);
    }

    private void ApplyHealthRatio(
        float ratio)
    {
        ratio =
            Mathf.Clamp01(
                ratio);

        RefreshVisualOnly(
            ratio);

        bool shouldShow =
            !hideWhenFull ||
            ratio < 1f;

        SetVisible(
            shouldShow);
    }

    // Fill의 왼쪽 끝을 고정한 채
    // 오른쪽만 줄어들도록 크기와 위치를 함께 조절
    private void RefreshVisualOnly(
        float ratio)
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

        // Sprite pivot이 중앙이라는 전제.
        // 전체 체력일 때의 중심에서
        // 줄어든 절반만큼 왼쪽으로 이동시킨다.
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

    private void SetVisible(
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