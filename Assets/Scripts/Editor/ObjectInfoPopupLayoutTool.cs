#if UNITY_EDITOR

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ObjectInfoPopupLayoutTool
{
    private const string DefaultProfileFolder =
        "Assets/GameData/UI";

    private const string DefaultProfilePath =
        DefaultProfileFolder +
        "/ObjectInfoLayoutProfile.asset";

    // =====================================================
    // Apply Layout
    // =====================================================

    [MenuItem(
        "Tools/Jeokru/Layout Object Info Popup")]
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
            selected.GetComponent<
                RectTransform>();

        if (popup == null)
        {
            Debug.LogError(
                "선택한 오브젝트에 RectTransform이 없습니다.");

            return;
        }

        ObjectInfoLayoutProfile profile =
            FindLayoutProfile();

        if (profile == null)
        {
            Debug.LogError(
                "ObjectInfoLayoutProfile을 찾지 못했습니다.\n" +
                "Tools > Jeokru > Create Default Object Info Layout Profile을 먼저 실행하세요.");

            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            selected,
            "Layout Object Info Popup");

        SetupPopupRoot(
            popup,
            profile);

        SetupBackground(
            popup);

        ApplyTextLayouts(
            popup,
            profile);

        ApplyRectLayouts(
            popup,
            profile);

        EditorUtility.SetDirty(
            selected);

        Debug.Log(
            $"ObjectInfoPopup 배치 완료. " +
            $"Profile = {profile.name}, " +
            $"UiScale = {profile.UiScale}");
    }

    // =====================================================
    // Create Default Profile
    // =====================================================

    [MenuItem(
        "Tools/Jeokru/Create Default Object Info Layout Profile")]
    private static void
        CreateDefaultLayoutProfile()
    {
        ObjectInfoLayoutProfile existing =
            AssetDatabase.LoadAssetAtPath<
                ObjectInfoLayoutProfile>(
                    DefaultProfilePath);

        if (existing != null)
        {
            Selection.activeObject =
                existing;

            EditorGUIUtility.PingObject(
                existing);

            Debug.LogWarning(
                $"이미 Layout Profile이 존재합니다: " +
                $"{DefaultProfilePath}");

            return;
        }

        EnsureFolderExists(
            DefaultProfileFolder);

        ObjectInfoLayoutProfile profile =
            ScriptableObject.CreateInstance<
                ObjectInfoLayoutProfile>();

        List<
            ObjectInfoLayoutProfile.TextLayout>
            textLayouts =
                BuildDefaultTextLayouts();

        List<
            ObjectInfoLayoutProfile.RectLayout>
            rectLayouts =
                BuildDefaultRectLayouts();

        profile.SetDefaults(
            scale: 1.6f,
            size:
                new Vector2(
                    264f,
                    385f),
            margin:
                new Vector2(
                    24f,
                    105f),
            texts:
                textLayouts,
            rects:
                rectLayouts);

        AssetDatabase.CreateAsset(
            profile,
            DefaultProfilePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject =
            profile;

        EditorGUIUtility.PingObject(
            profile);

        Debug.Log(
            $"ObjectInfoLayoutProfile 생성 완료: " +
            $"{DefaultProfilePath}");
    }

    // =====================================================
    // Default Data
    // =====================================================

    private static List<
        ObjectInfoLayoutProfile.TextLayout>
        BuildDefaultTextLayouts()
    {
        return new List<
            ObjectInfoLayoutProfile.TextLayout>
        {
            new(
                "NameText",
                "원거리 유닛",
                new Vector2(
                    17f,
                    17f),
                new Vector2(
                    150f,
                    30f),
                20f,
                TextAlignmentOptions.Left),

            new(
                "StateText",
                "작업 중",
                new Vector2(
                    188f,
                    20f),
                new Vector2(
                    58f,
                    22f),
                12f,
                TextAlignmentOptions.Center),

            new(
                "HpLabelText",
                "HP",
                new Vector2(
                    22f,
                    68f),
                new Vector2(
                    75f,
                    24f),
                16f,
                TextAlignmentOptions.Left),

            new(
                "HpValueText",
                "72/100",
                new Vector2(
                    145f,
                    68f),
                new Vector2(
                    99f,
                    24f),
                16f,
                TextAlignmentOptions.Right),

            new(
                "StressLabelText",
                "스트레스",
                new Vector2(
                    22f,
                    101f),
                new Vector2(
                    90f,
                    24f),
                16f,
                TextAlignmentOptions.Left),

            new(
                "StressValueText",
                "35/100",
                new Vector2(
                    145f,
                    101f),
                new Vector2(
                    99f,
                    24f),
                16f,
                TextAlignmentOptions.Right),

            new(
                "AttackLabelText",
                "공격력",
                new Vector2(
                    22f,
                    157f),
                new Vector2(
                    95f,
                    24f),
                15f,
                TextAlignmentOptions.Left),

            new(
                "AttackValueText",
                "18.6",
                new Vector2(
                    145f,
                    157f),
                new Vector2(
                    99f,
                    24f),
                15f,
                TextAlignmentOptions.Right),

            new(
                "DefenseLabelText",
                "방어력",
                new Vector2(
                    22f,
                    193f),
                new Vector2(
                    95f,
                    24f),
                15f,
                TextAlignmentOptions.Left),

            new(
                "DefenseValueText",
                "7.2",
                new Vector2(
                    145f,
                    193f),
                new Vector2(
                    99f,
                    24f),
                15f,
                TextAlignmentOptions.Right),

            new(
                "AttackSpeedLabelText",
                "공격 속도",
                new Vector2(
                    22f,
                    229f),
                new Vector2(
                    100f,
                    24f),
                15f,
                TextAlignmentOptions.Left),

            new(
                "AttackSpeedValueText",
                "1.4s",
                new Vector2(
                    145f,
                    229f),
                new Vector2(
                    99f,
                    24f),
                15f,
                TextAlignmentOptions.Right),

            new(
                "IdleLabelText",
                "대기",
                new Vector2(
                    22f,
                    265f),
                new Vector2(
                    100f,
                    24f),
                15f,
                TextAlignmentOptions.Left),

            new(
                "IdleValueText",
                "0",
                new Vector2(
                    145f,
                    265f),
                new Vector2(
                    99f,
                    24f),
                15f,
                TextAlignmentOptions.Right),

            new(
                "DescriptionText",
                "붉은 약 공장에서 작업 중",
                new Vector2(
                    22f,
                    320f),
                new Vector2(
                    222f,
                    46f),
                13f,
                TextAlignmentOptions.TopLeft,
                allowWrapping: true)
        };
    }

    private static List<
        ObjectInfoLayoutProfile.RectLayout>
        BuildDefaultRectLayouts()
    {
        return new List<
            ObjectInfoLayoutProfile.RectLayout>
        {
            new(
                "Divider1",
                new Vector2(
                    22f,
                    137f),
                new Vector2(
                    222f,
                    1f)),

            new(
                "Divider2",
                new Vector2(
                    22f,
                    301f),
                new Vector2(
                    222f,
                    1f))
        };
    }

    // =====================================================
    // Apply Profile
    // =====================================================

    private static void ApplyTextLayouts(
        RectTransform popup,
        ObjectInfoLayoutProfile profile)
    {
        foreach (
            ObjectInfoLayoutProfile.TextLayout
                layout
            in profile.TextLayouts)
        {
            SetupText(
                popup,
                profile,
                layout);
        }
    }

    private static void ApplyRectLayouts(
        RectTransform popup,
        ObjectInfoLayoutProfile profile)
    {
        foreach (
            ObjectInfoLayoutProfile.RectLayout
                layout
            in profile.RectLayouts)
        {
            SetupRect(
                popup,
                profile,
                layout);
        }
    }

    // =====================================================
    // Popup Root
    // =====================================================

    private static void SetupPopupRoot(
        RectTransform popup,
        ObjectInfoLayoutProfile profile)
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
            profile.PopupSize *
            profile.UiScale;

        popup.anchoredPosition =
            new Vector2(
                -profile.PopupMargin.x,
                -profile.PopupMargin.y);

        popup.localScale =
            Vector3.one;

        EditorUtility.SetDirty(
            popup);
    }

    // =====================================================
    // Background
    // =====================================================

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

    // =====================================================
    // Text
    // =====================================================

    private static void SetupText(
        RectTransform parent,
        ObjectInfoLayoutProfile profile,
        ObjectInfoLayoutProfile.TextLayout
            layout)
    {
        if (string.IsNullOrWhiteSpace(
                layout.ObjectName))
        {
            return;
        }

        RectTransform rect =
            FindRect(
                parent,
                layout.ObjectName);

        if (rect == null)
        {
            Debug.LogWarning(
                $"{layout.ObjectName}을 찾지 못했습니다.");

            return;
        }

        SetTopLeftRect(
            rect,
            layout.Position *
                profile.UiScale,
            layout.Size *
                profile.UiScale);

        TMP_Text text =
            rect.GetComponent<TMP_Text>();

        if (text == null)
        {
            Debug.LogWarning(
                $"{layout.ObjectName}에 TMP_Text가 없습니다.");

            return;
        }

        text.text =
            layout.PreviewText;

        text.fontSize =
            layout.FontSize *
            profile.UiScale;

        text.enableAutoSizing =
            false;

        text.alignment =
            layout.Alignment;

        text.enableWordWrapping =
            layout.AllowWrapping;

        text.overflowMode =
            layout.AllowWrapping
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;

        if (layout.OverrideFontStyle)
        {
            text.fontStyle =
                layout.FontStyle;
        }

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

    // =====================================================
    // Rect
    // =====================================================

    private static void SetupRect(
        RectTransform parent,
        ObjectInfoLayoutProfile profile,
        ObjectInfoLayoutProfile.RectLayout
            layout)
    {
        if (string.IsNullOrWhiteSpace(
                layout.ObjectName))
        {
            return;
        }

        RectTransform rect =
            FindRect(
                parent,
                layout.ObjectName);

        if (rect == null)
        {
            Debug.LogWarning(
                $"{layout.ObjectName}을 찾지 못했습니다.");

            return;
        }

        SetTopLeftRect(
            rect,
            layout.Position *
                profile.UiScale,
            layout.Size *
                profile.UiScale);
    }

    // =====================================================
    // RectTransform Helper
    // =====================================================

    private static void SetTopLeftRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
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
                position.x,
                -position.y);

        rect.sizeDelta =
            size;

        rect.localScale =
            Vector3.one;

        EditorUtility.SetDirty(
            rect);
    }

    // =====================================================
    // Profile Search
    // =====================================================

    private static ObjectInfoLayoutProfile
        FindLayoutProfile()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:ObjectInfoLayoutProfile");

        if (guids == null ||
            guids.Length == 0)
        {
            return null;
        }

        if (guids.Length > 1)
        {
            Debug.LogError(
                "ObjectInfoLayoutProfile이 여러 개 존재합니다. " +
                "현재는 하나의 공통 프로필만 사용하도록 설계되어 있습니다.");

            return null;
        }

        string path =
            AssetDatabase
                .GUIDToAssetPath(
                    guids[0]);

        return AssetDatabase
            .LoadAssetAtPath<
                ObjectInfoLayoutProfile>(
                    path);
    }

    // =====================================================
    // Search Helper
    // =====================================================

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

    // =====================================================
    // Folder Helper
    // =====================================================

    private static void EnsureFolderExists(
        string folderPath)
    {
        string[] parts =
            folderPath.Split('/');

        if (parts.Length <= 1)
        {
            return;
        }

        string currentPath =
            parts[0];

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string nextPath =
                currentPath +
                "/" +
                parts[i];

            if (!AssetDatabase.IsValidFolder(
                    nextPath))
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    parts[i]);
            }

            currentPath =
                nextPath;
        }
    }
}

#endif