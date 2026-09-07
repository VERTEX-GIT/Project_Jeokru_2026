#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;

public static class ObjectInfoPopupLayoutTool
{
    // 전체 UI 확대 배율
    private const float UiScale = 1.6f;

    // 정보창 기준 크기
    private const float PopupWidth = 264f;
    private const float PopupHeight = 385f;

    // 오른쪽 위 여백
    private const float RightMargin = 24f;
    private const float TopMargin = 105f;

    [MenuItem("Tools/Jeokru/Layout Object Info Popup")]
    private static void LayoutPopup()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError(
                "ObjectInfoPopup 오브젝트를 선택한 뒤 실행하세요.");

            return;
        }

        RectTransform popup =
            selected.GetComponent<RectTransform>();

        if (popup == null)
        {
            Debug.LogError(
                "선택한 오브젝트에 RectTransform이 없습니다.");

            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            selected,
            "Layout Object Info Popup");

        SetupPopupRoot(popup);
        SetupBackground(popup);

        // =========================
        // Header
        // =========================

        SetupText(
            popup,
            "NameText",
            "원거리 유닛",
            x: 17f,
            y: 17f,
            width: 150f,
            height: 30f,
            fontSize: 20f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "StateText",
            "작업 중",
            x: 188f,
            y: 20f,
            width: 58f,
            height: 22f,
            fontSize: 12f,
            alignment:
                TextAlignmentOptions.Center);

        // =========================
        // HP
        // =========================

        SetupText(
            popup,
            "HpLabelText",
            "HP",
            x: 22f,
            y: 68f,
            width: 75f,
            height: 24f,
            fontSize: 16f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "HpValueText",
            "72/100",
            x: 145f,
            y: 68f,
            width: 99f,
            height: 24f,
            fontSize: 16f,
            alignment:
                TextAlignmentOptions.Right);

        // =========================
        // Stress
        // =========================

        SetupText(
            popup,
            "StressLabelText",
            "스트레스",
            x: 22f,
            y: 101f,
            width: 90f,
            height: 24f,
            fontSize: 16f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "StressValueText",
            "35/100",
            x: 145f,
            y: 101f,
            width: 99f,
            height: 24f,
            fontSize: 16f,
            alignment:
                TextAlignmentOptions.Right);

        // =========================
        // Divider 1
        // =========================

        SetupRect(
            popup,
            "Divider1",
            x: 22f,
            y: 137f,
            width: 222f,
            height: 1f);

        // =========================
        // Row 1
        // =========================

        SetupText(
            popup,
            "AttackLabelText",
            "공격력",
            x: 22f,
            y: 157f,
            width: 95f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "AttackValueText",
            "18.6",
            x: 145f,
            y: 157f,
            width: 99f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Right);

        // =========================
        // Row 2
        // =========================

        SetupText(
            popup,
            "DefenseLabelText",
            "방어력",
            x: 22f,
            y: 193f,
            width: 95f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "DefenseValueText",
            "7.2",
            x: 145f,
            y: 193f,
            width: 99f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Right);

        // =========================
        // Row 3
        // =========================

        SetupText(
            popup,
            "AttackSpeedLabelText",
            "공격 속도",
            x: 22f,
            y: 229f,
            width: 100f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "AttackSpeedValueText",
            "1.4s",
            x: 145f,
            y: 229f,
            width: 99f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Right);

        // =========================
        // Idle Row
        // =========================

        SetupText(
            popup,
            "IdleLabelText",
            "대기",
            x: 22f,
            y: 265f,
            width: 100f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Left);

        SetupText(
            popup,
            "IdleValueText",
            "0",
            x: 145f,
            y: 265f,
            width: 99f,
            height: 24f,
            fontSize: 15f,
            alignment:
                TextAlignmentOptions.Right);

        // =========================
        // Divider 2
        // =========================

        SetupRect(
            popup,
            "Divider2",
            x: 22f,
            y: 301f,
            width: 222f,
            height: 1f);

        // =========================
        // Description
        // =========================

        SetupText(
            popup,
            "DescriptionText",
            "붉은 약 공장에서 작업 중",
            x: 22f,
            y: 320f,
            width: 222f,
            height: 46f,
            fontSize: 13f,
            alignment:
                TextAlignmentOptions.TopLeft,
            allowWrapping: true);

        EditorUtility.SetDirty(
            selected);

        Debug.Log(
            $"ObjectInfoPopup 배치 완료. UiScale = {UiScale}");
    }

    // =========================
    // Popup Root
    // =========================

    private static void SetupPopupRoot(
        RectTransform popup)
    {
        popup.anchorMin =
            new Vector2(
                1f,
                1f);

        popup.anchorMax =
            new Vector2(
                1f,
                1f);

        popup.pivot =
            new Vector2(
                1f,
                1f);

        popup.sizeDelta =
            new Vector2(
                PopupWidth *
                UiScale,
                PopupHeight *
                UiScale);

        popup.anchoredPosition =
            new Vector2(
                -RightMargin,
                -TopMargin);

        popup.localScale =
            Vector3.one;

        EditorUtility.SetDirty(
            popup);
    }

    // =========================
    // Background
    // =========================

    private static void SetupBackground(
        RectTransform popup)
    {
        RectTransform background =
            FindRect(
                popup,
                "Background");

        if (background == null)
        {
            Debug.LogWarning(
                "Background을 찾지 못했습니다.");

            return;
        }

        background.anchorMin =
            Vector2.zero;

        background.anchorMax =
            Vector2.one;

        background.pivot =
            new Vector2(
                0.5f,
                0.5f);

        background.offsetMin =
            Vector2.zero;

        background.offsetMax =
            Vector2.zero;

        background.localScale =
            Vector3.one;

        background.SetAsFirstSibling();

        EditorUtility.SetDirty(
            background);
    }

    // =========================
    // Text
    // =========================

    private static void SetupText(
        RectTransform parent,
        string objectName,
        string content,
        float x,
        float y,
        float width,
        float height,
        float fontSize,
        TextAlignmentOptions alignment,
        bool bold = false,
        bool allowWrapping = false)
    {
        RectTransform rect =
            FindRect(
                parent,
                objectName);

        if (rect == null)
        {
            Debug.LogWarning(
                $"{objectName}을 찾지 못했습니다.");

            return;
        }

        SetTopLeftRect(
            rect,
            x * UiScale,
            y * UiScale,
            width * UiScale,
            height * UiScale);

        TMP_Text text =
            rect.GetComponent<TMP_Text>();

        if (text == null)
        {
            Debug.LogWarning(
                $"{objectName}에 TMP_Text가 없습니다.");

            return;
        }

        text.text =
            content;

        text.fontSize =
            fontSize *
            UiScale;

        text.enableAutoSizing =
            false;

        text.alignment =
            alignment;

        text.enableWordWrapping =
            allowWrapping;

        text.overflowMode =
            allowWrapping
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;

        text.fontStyle =
            bold
                ? FontStyles.Bold
                : FontStyles.Normal;

        text.characterSpacing =
            0f;

        text.wordSpacing =
            0f;

        text.lineSpacing =
            0f;

        text.margin =
            Vector4.zero;

        text.raycastTarget =
            false;

        EditorUtility.SetDirty(
            text);
    }

    // =========================
    // Divider / Rect
    // =========================

    private static void SetupRect(
        RectTransform parent,
        string objectName,
        float x,
        float y,
        float width,
        float height)
    {
        RectTransform rect =
            FindRect(
                parent,
                objectName);

        if (rect == null)
        {
            Debug.LogWarning(
                $"{objectName}을 찾지 못했습니다.");

            return;
        }

        SetTopLeftRect(
            rect,
            x * UiScale,
            y * UiScale,
            width * UiScale,
            height * UiScale);

        EditorUtility.SetDirty(
            rect);
    }

    // =========================
    // RectTransform Helper
    // =========================

    private static void SetTopLeftRect(
        RectTransform rect,
        float x,
        float y,
        float width,
        float height)
    {
        rect.anchorMin =
            new Vector2(
                0f,
                1f);

        rect.anchorMax =
            new Vector2(
                0f,
                1f);

        rect.pivot =
            new Vector2(
                0f,
                1f);

        rect.anchoredPosition =
            new Vector2(
                x,
                -y);

        rect.sizeDelta =
            new Vector2(
                width,
                height);

        rect.localScale =
            Vector3.one;

        EditorUtility.SetDirty(
            rect);
    }

    // =========================
    // Search Helper
    // =========================

    private static RectTransform FindRect(
        RectTransform parent,
        string objectName)
    {
        RectTransform[] rects =
            parent.GetComponentsInChildren<
                RectTransform>(
                true);

        foreach (RectTransform rect
                 in rects)
        {
            if (rect.name ==
                objectName)
            {
                return rect;
            }
        }

        return null;
    }
}

#endif