using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class ObjectInfoPopupController
    : MonoBehaviour
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
    [Min(0f)]
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

        // 이 UI는 절대로 게임 입력을 막지 않는다.
        canvasGroup.blocksRaycasts =
            false;

        canvasGroup.interactable =
            false;

        Hide();
    }

    private void Update()
    {
        UpdateTransparency();
    }

    // =========================
    // 단일 유닛
    // =========================

    public void ShowSingle(
        HoverInfoData data)
    {
        isVisible = true;

        SetText(
            nameText,
            data.Name);

        SetText(
            stateText,
            data.State);

        SetText(
            hpLabelText,
            "HP");

        SetText(
            hpValueText,
            data.Hp);

        SetText(
            stressLabelText,
            "스트레스");

        SetText(
            stressValueText,
            data.Stress);

        SetText(
            attackPowerLabelText,
            "공격력");

        SetText(
            attackPowerValueText,
            data.AttackPower);

        SetText(
            defenseLabelText,
            "방어력");

        SetText(
            defenseValueText,
            data.Defense);

        SetText(
            attackSpeedLabelText,
            "공격 속도");

        SetText(
            attackSpeedValueText,
            data.AttackSpeed);

        SetText(
            descriptionText,
            data.Description);

        ApplyCurrentAlpha();
    }

    // =========================
    // 다중 선택
    // =========================

    public void ShowMulti(
        MultiSelectionInfoData data)
    {
        isVisible = true;

        SetText(
            nameText,
            data.Title);

        SetText(
            stateText,
            "다중 선택");

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

        // 기존 공격력/방어력/공속 3개 행을
        // 다중 선택에서는 상태 분포로 재사용
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

        string composition =
            BuildCompositionText(
                data);

        SetText(
            descriptionText,
            composition);

        ApplyCurrentAlpha();
    }

    public void Hide()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    // =========================
    // Multi Description
    // =========================

    private static string BuildCompositionText(
        MultiSelectionInfoData data)
    {
        string composition;

        if (data.RangedCount > 0 &&
            data.MeleeCount > 0)
        {
            composition =
                $"원거리 {data.RangedCount} / " +
                $"근거리 {data.MeleeCount}";
        }
        else if (data.RangedCount > 0)
        {
            composition =
                $"원거리 {data.RangedCount}";
        }
        else if (data.MeleeCount > 0)
        {
            composition =
                $"근거리 {data.MeleeCount}";
        }
        else
        {
            composition =
                "구성 정보 없음";
        }

        if (data.IdleCount > 0)
        {
            composition +=
                $"\n대기 {data.IdleCount}";
        }

        return composition;
    }

    // =========================
    // Transparency
    // =========================

    private void UpdateTransparency()
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (!isVisible)
        {
            canvasGroup.alpha =
                0f;

            return;
        }

        float targetAlpha =
            IsPointerNearPopup()
                ? nearPointerAlpha
                : normalAlpha;

        canvasGroup.alpha =
            Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                fadeSpeed *
                Time.unscaledDeltaTime);
    }

    private void ApplyCurrentAlpha()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha =
            IsPointerNearPopup()
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
            Mouse.current.position
                .ReadValue();

        Camera uiCamera =
            null;

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
            value ??
            string.Empty;
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
            canvasGroup.blocksRaycasts =
                false;

            canvasGroup.interactable =
                false;
        }
    }

#endif
}