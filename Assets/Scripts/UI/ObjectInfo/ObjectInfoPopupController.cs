using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class ObjectInfoPopupController :
    MonoBehaviour
{
    [Header("Root")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform popupRect;

    [Header("Header")]
    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text stateText;

    [Header("HP")]
    [SerializeField]
    private TMP_Text hpLabelText;

    [SerializeField]
    private TMP_Text hpValueText;

    [Header("Stress")]
    [SerializeField]
    private TMP_Text stressLabelText;

    [SerializeField]
    private TMP_Text stressValueText;

    [Header("Row 1")]
    [SerializeField]
    private TMP_Text attackPowerLabelText;

    [SerializeField]
    private TMP_Text attackPowerValueText;

    [Header("Row 2")]
    [SerializeField]
    private TMP_Text defenseLabelText;

    [SerializeField]
    private TMP_Text defenseValueText;

    [Header("Row 3")]
    [SerializeField]
    private TMP_Text attackSpeedLabelText;

    [SerializeField]
    private TMP_Text attackSpeedValueText;

    [Header("Idle Row")]
    [SerializeField]
    private TMP_Text idleLabelText;

    [SerializeField]
    private TMP_Text idleValueText;

    [Header("Description")]
    [SerializeField]
    private TMP_Text descriptionText;

    [Header("Mouse Transparency")]
    [SerializeField]
    [Range(0f, 1f)]
    private float normalAlpha = 1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float nearPointerAlpha = 0.25f;

    [SerializeField]
    [Min(0f)]
    private float fadePadding = 50f;

    [SerializeField]
    [Min(0.01f)]
    private float fadeSpeed = 8f;

    private Canvas parentCanvas;

    private bool isVisible;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (popupRect == null)
        {
            popupRect =
                transform as RectTransform;
        }

        parentCanvas =
            GetComponentInParent<Canvas>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        HideImmediate();
    }

    private void Update()
    {
        UpdateTransparency();
    }

    public void ShowSingle(
        HoverInfoData data)
    {
        bool wasHidden =
            !isVisible;

        isVisible = true;

        ApplySingleLayout();

        SetText(
            nameText,
            data.Name);

        SetText(
            stateText,
            data.State);

        SetText(
            hpLabelText,
            data.HpLabel);

        SetText(
            hpValueText,
            data.Hp);

        SetText(
            stressLabelText,
            data.StressLabel);

        SetText(
            stressValueText,
            data.Stress);

        SetText(
            attackPowerLabelText,
            data.AttackPowerLabel);

        SetText(
            attackPowerValueText,
            data.AttackPower);

        SetText(
            defenseLabelText,
            data.DefenseLabel);

        SetText(
            defenseValueText,
            data.Defense);

        SetText(
            attackSpeedLabelText,
            data.AttackSpeedLabel);

        SetText(
            attackSpeedValueText,
            data.AttackSpeed);

        SetText(
            descriptionText,
            data.Description);

        if (wasHidden &&
            canvasGroup != null)
        {
            canvasGroup.alpha =
                GetTargetAlpha();
        }
    }

    public void ShowMulti(
        MultiSelectionInfoData data)
    {
        bool wasHidden =
            !isVisible;

        isVisible = true;

        ApplyMultiLayout();

        SetText(
            nameText,
            data.Title);

        SetText(
            hpLabelText,
            "HP 평균");

        SetText(
            hpValueText,
            data.AverageHp);

        SetText(
            stressLabelText,
            "스트레스 평균");

        SetText(
            stressValueText,
            data.AverageStress);

        SetText(
            attackPowerLabelText,
            "이동");

        SetText(
            attackPowerValueText,
            data.MovingCount.ToString());

        SetText(
            defenseLabelText,
            "전투");

        SetText(
            defenseValueText,
            data.CombatCount.ToString());

        SetText(
            attackSpeedLabelText,
            "작업");

        SetText(
            attackSpeedValueText,
            data.WorkingCount.ToString());

        SetText(
            idleLabelText,
            "대기");

        SetText(
            idleValueText,
            data.IdleCount.ToString());

        SetText(
            descriptionText,
            BuildCompositionText(data));

        if (wasHidden &&
            canvasGroup != null)
        {
            canvasGroup.alpha =
                GetTargetAlpha();
        }
    }

    public void Hide()
    {
        isVisible = false;
    }

    private void HideImmediate()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void ApplySingleLayout()
    {
        SetActive(
            stateText,
            true);

        SetActive(
            idleLabelText,
            false);

        SetActive(
            idleValueText,
            false);
    }

    private void ApplyMultiLayout()
    {
        SetActive(
            stateText,
            false);

        SetActive(
            idleLabelText,
            true);

        SetActive(
            idleValueText,
            true);
    }

    private static string
        BuildCompositionText(
            MultiSelectionInfoData data)
    {
        if (data.RangedCount > 0 &&
            data.MeleeCount > 0)
        {
            return
                $"원거리 {data.RangedCount} / " +
                $"근거리 {data.MeleeCount}";
        }

        if (data.RangedCount > 0)
        {
            return
                $"원거리 {data.RangedCount}";
        }

        if (data.MeleeCount > 0)
        {
            return
                $"근거리 {data.MeleeCount}";
        }

        return "구성 정보 없음";
    }

    private void UpdateTransparency()
    {
        if (canvasGroup == null)
        {
            return;
        }

        float targetAlpha =
            isVisible
                ? GetTargetAlpha()
                : 0f;

        float lerpFactor =
            1f -
            Mathf.Exp(
                -fadeSpeed *
                Time.unscaledDeltaTime);

        canvasGroup.alpha =
            Mathf.Lerp(
                canvasGroup.alpha,
                targetAlpha,
                lerpFactor);

        if (!isVisible &&
            canvasGroup.alpha < 0.001f)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private float GetTargetAlpha()
    {
        return IsPointerNearPopup()
            ? nearPointerAlpha
            : normalAlpha;
    }

    private bool IsPointerNearPopup()
    {
        if (popupRect == null ||
            Mouse.current == null)
        {
            return false;
        }

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Camera uiCamera = null;

        if (parentCanvas != null &&
            parentCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                parentCanvas.worldCamera;
        }

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    popupRect,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPoint))
        {
            return false;
        }

        Rect expandedRect =
            popupRect.rect;

        expandedRect.xMin -=
            fadePadding;

        expandedRect.xMax +=
            fadePadding;

        expandedRect.yMin -=
            fadePadding;

        expandedRect.yMax +=
            fadePadding;

        return expandedRect.Contains(
            localPoint);
    }

    private static void SetText(
        TMP_Text target,
        string value)
    {
        if (target == null)
        {
            return;
        }

        target.text =
            value ?? string.Empty;
    }

    private static void SetActive(
        TMP_Text target,
        bool active)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(
            active);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (popupRect == null)
        {
            popupRect =
                transform as RectTransform;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
#endif
}