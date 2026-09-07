#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsMenuLayoutTool
{
    private const float PanelWidth = 1120f;
    private const float PanelHeight = 760f;

    private const float HorizontalPadding = 90f;
    private const float ContentWidth =
        PanelWidth - HorizontalPadding * 2f;

    [MenuItem("Tools/Jeokru UI/Arrange Existing Settings Menu")]
    public static void ArrangeExistingSettingsMenu()
    {
        GameObject rootObject =
            GameObject.Find("SettingsPanel");

        if (rootObject == null)
        {
            Debug.LogError(
                "SettingsMenuLayoutTool: SettingsPanel을 찾을 수 없습니다.");

            return;
        }

        RectTransform root =
            rootObject.GetComponent<RectTransform>();

        if (root == null)
        {
            Debug.LogError(
                "SettingsMenuLayoutTool: SettingsPanel에 RectTransform이 없습니다.");

            return;
        }

        Undo.IncrementCurrentGroup();

        int undoGroup =
            Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Arrange Settings Menu");

        ArrangeRoot(root);
        ArrangeBackground(root);
        ArrangeTitle(root);
        ArrangeFullscreenRow(root);
        ArrangeResolutionRow(root);
        ArrangeVolumeRow(root);
        ArrangeButtons(root);

        EditorUtility.SetDirty(
            rootObject);

        Selection.activeGameObject =
            rootObject;

        Undo.CollapseUndoOperations(
            undoGroup);

        Debug.Log(
            "SettingsMenuLayoutTool: SettingsPanel 배치 및 텍스트 수정 완료.");
    }

    private static void ArrangeRoot(
        RectTransform root)
    {
        Record(root);

        root.anchorMin =
            new Vector2(0.5f, 0.5f);

        root.anchorMax =
            new Vector2(0.5f, 0.5f);

        root.pivot =
            new Vector2(0.5f, 0.5f);

        root.sizeDelta =
            new Vector2(
                PanelWidth,
                PanelHeight);

        root.anchoredPosition =
            Vector2.zero;
    }

    private static void ArrangeBackground(
        Transform root)
    {
        RectTransform background =
            FindDirectChildRect(
                root,
                "Background");

        if (background == null)
        {
            Debug.LogWarning(
                "SettingsMenuLayoutTool: Background를 찾지 못했습니다.");

            return;
        }

        Record(background);

        background.anchorMin =
            Vector2.zero;

        background.anchorMax =
            Vector2.one;

        background.pivot =
            new Vector2(0.5f, 0.5f);

        background.offsetMin =
            Vector2.zero;

        background.offsetMax =
            Vector2.zero;

        background.SetAsFirstSibling();

        Image image =
            background.GetComponent<Image>();

        if (image != null)
        {
            Record(image);

            image.color =
                new Color(
                    0.045f,
                    0.052f,
                    0.060f,
                    0.98f);
        }
    }

    private static void ArrangeTitle(
        Transform root)
    {
        RectTransform title =
            FindDirectChildRect(
                root,
                "TitleText");

        if (title == null)
        {
            return;
        }

        Record(title);

        SetTopLeft(
            title,
            HorizontalPadding,
            -64f,
            ContentWidth,
            70f);

        TMP_Text text =
            title.GetComponent<TMP_Text>();

        if (text == null)
        {
            return;
        }

        Record(text);

        text.text =
            "설정";

        text.fontSize =
            42f;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Left;

        text.color =
            new Color(
                0.95f,
                0.96f,
                0.97f,
                1f);
    }

    private static void ArrangeFullscreenRow(
        Transform root)
    {
        RectTransform row =
            FindDirectChildRect(
                root,
                "FullscreenRow");

        if (row == null)
        {
            return;
        }

        ArrangeRow(
            row,
            -175f);

        SetRowLabel(
            row,
            "전체 화면");

        RectTransform toggle =
            FindDirectChildRect(
                row,
                "FullscreenToggle");

        if (toggle == null)
        {
            return;
        }

        Record(toggle);

        toggle.anchorMin =
            new Vector2(1f, 0.5f);

        toggle.anchorMax =
            new Vector2(1f, 0.5f);

        toggle.pivot =
            new Vector2(1f, 0.5f);

        toggle.sizeDelta =
            new Vector2(
                72f,
                38f);

        toggle.anchoredPosition =
            new Vector2(
                -32f,
                0f);

        RectTransform toggleBackground =
            FindDirectChildRect(
                toggle,
                "Background");

        if (toggleBackground != null)
        {
            Record(toggleBackground);

            Stretch(toggleBackground);

            Image image =
                toggleBackground.GetComponent<Image>();

            if (image != null)
            {
                Record(image);

                image.color =
                    new Color(
                        0.13f,
                        0.15f,
                        0.17f,
                        1f);
            }
        }

        RectTransform checkmark =
            FindRecursiveRect(
                toggle,
                "Checkmark");

        if (checkmark != null)
        {
            Record(checkmark);

            checkmark.anchorMin =
                new Vector2(0f, 0.5f);

            checkmark.anchorMax =
                new Vector2(0f, 0.5f);

            checkmark.pivot =
                new Vector2(0.5f, 0.5f);

            checkmark.sizeDelta =
                new Vector2(
                    30f,
                    30f);

            checkmark.anchoredPosition =
                new Vector2(
                    19f,
                    0f);
        }
    }

    private static void ArrangeResolutionRow(
        Transform root)
    {
        RectTransform row =
            FindDirectChildRect(
                root,
                "ResolutionRow");

        if (row == null)
        {
            return;
        }

        ArrangeRow(
            row,
            -280f);

        SetRowLabel(
            row,
            "해상도");

        RectTransform dropdown =
            FindDirectChildRect(
                row,
                "ResolutionDropdown");

        if (dropdown == null)
        {
            return;
        }

        Record(dropdown);

        dropdown.anchorMin =
            new Vector2(1f, 0.5f);

        dropdown.anchorMax =
            new Vector2(1f, 0.5f);

        dropdown.pivot =
            new Vector2(1f, 0.5f);

        dropdown.sizeDelta =
            new Vector2(
                360f,
                52f);

        dropdown.anchoredPosition =
            new Vector2(
                -32f,
                0f);

        Image dropdownImage =
            dropdown.GetComponent<Image>();

        if (dropdownImage != null)
        {
            Record(dropdownImage);

            dropdownImage.color =
                new Color(
                    0.10f,
                    0.115f,
                    0.13f,
                    1f);
        }

        TMP_Dropdown dropdownComponent =
            dropdown.GetComponent<TMP_Dropdown>();

        if (dropdownComponent != null &&
            dropdownComponent.captionText != null)
        {
            Record(
                dropdownComponent.captionText);

            dropdownComponent.captionText.fontSize =
                19f;

            dropdownComponent.captionText.color =
                new Color(
                    0.90f,
                    0.92f,
                    0.94f,
                    1f);
        }

        RectTransform label =
            FindDirectChildRect(
                dropdown,
                "Label");

        if (label != null)
        {
            Record(label);

            label.anchorMin =
                Vector2.zero;

            label.anchorMax =
                Vector2.one;

            label.offsetMin =
                new Vector2(
                    18f,
                    0f);

            label.offsetMax =
                new Vector2(
                    -55f,
                    0f);
        }

        RectTransform arrow =
            FindDirectChildRect(
                dropdown,
                "Arrow");

        if (arrow != null)
        {
            Record(arrow);

            arrow.anchorMin =
                new Vector2(1f, 0.5f);

            arrow.anchorMax =
                new Vector2(1f, 0.5f);

            arrow.pivot =
                new Vector2(0.5f, 0.5f);

            arrow.sizeDelta =
                new Vector2(
                    28f,
                    28f);

            arrow.anchoredPosition =
                new Vector2(
                    -24f,
                    0f);
        }
    }

    private static void ArrangeVolumeRow(
        Transform root)
    {
        RectTransform row =
            FindDirectChildRect(
                root,
                "VolumeRow");

        if (row == null)
        {
            return;
        }

        ArrangeRow(
            row,
            -385f);

        SetRowLabel(
            row,
            "전체 음량");

        RectTransform slider =
            FindDirectChildRect(
                row,
                "MasterVolumeSlider");

        if (slider == null)
        {
            return;
        }

        Record(slider);

        slider.anchorMin =
            new Vector2(1f, 0.5f);

        slider.anchorMax =
            new Vector2(1f, 0.5f);

        slider.pivot =
            new Vector2(1f, 0.5f);

        slider.sizeDelta =
            new Vector2(
                360f,
                44f);

        slider.anchoredPosition =
            new Vector2(
                -32f,
                0f);

        RectTransform sliderBackground =
            FindDirectChildRect(
                slider,
                "Background");

        if (sliderBackground != null)
        {
            Record(sliderBackground);

            sliderBackground.anchorMin =
                new Vector2(
                    0f,
                    0.5f);

            sliderBackground.anchorMax =
                new Vector2(
                    1f,
                    0.5f);

            sliderBackground.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            sliderBackground.sizeDelta =
                new Vector2(
                    0f,
                    8f);

            sliderBackground.anchoredPosition =
                Vector2.zero;
        }

        RectTransform fillArea =
            FindDirectChildRect(
                slider,
                "Fill Area");

        if (fillArea != null)
        {
            Record(fillArea);

            fillArea.anchorMin =
                new Vector2(
                    0f,
                    0.5f);

            fillArea.anchorMax =
                new Vector2(
                    1f,
                    0.5f);

            fillArea.offsetMin =
                new Vector2(
                    0f,
                    -5f);

            fillArea.offsetMax =
                new Vector2(
                    -4f,
                    5f);
        }

        RectTransform handleArea =
            FindDirectChildRect(
                slider,
                "Handle Slide Area");

        if (handleArea != null)
        {
            Record(handleArea);

            handleArea.anchorMin =
                Vector2.zero;

            handleArea.anchorMax =
                Vector2.one;

            handleArea.offsetMin =
                new Vector2(
                    8f,
                    0f);

            handleArea.offsetMax =
                new Vector2(
                    -8f,
                    0f);

            RectTransform handle =
                FindDirectChildRect(
                    handleArea,
                    "Handle");

            if (handle != null)
            {
                Record(handle);

                handle.sizeDelta =
                    new Vector2(
                        22f,
                        22f);
            }
        }
    }

    private static void ArrangeRow(
        RectTransform row,
        float y)
    {
        Record(row);

        row.anchorMin =
            new Vector2(
                0f,
                1f);

        row.anchorMax =
            new Vector2(
                0f,
                1f);

        row.pivot =
            new Vector2(
                0f,
                1f);

        row.sizeDelta =
            new Vector2(
                ContentWidth,
                78f);

        row.anchoredPosition =
            new Vector2(
                HorizontalPadding,
                y);

        Image image =
            row.GetComponent<Image>();

        if (image != null)
        {
            Record(image);

            image.color =
                new Color(
                    0.065f,
                    0.075f,
                    0.085f,
                    1f);
        }
    }

    private static void SetRowLabel(
        Transform row,
        string labelText)
    {
        RectTransform label =
            FindDirectChildRect(
                row,
                "Label");

        if (label == null)
        {
            return;
        }

        Record(label);

        label.anchorMin =
            new Vector2(
                0f,
                0f);

        label.anchorMax =
            new Vector2(
                0.5f,
                1f);

        label.pivot =
            new Vector2(
                0f,
                0.5f);

        label.offsetMin =
            new Vector2(
                28f,
                0f);

        label.offsetMax =
            Vector2.zero;

        TMP_Text text =
            label.GetComponent<TMP_Text>();

        if (text == null)
        {
            return;
        }

        Record(text);

        text.text =
            labelText;

        text.fontSize =
            22f;

        text.fontStyle =
            FontStyles.Normal;

        text.alignment =
            TextAlignmentOptions.MidlineLeft;

        text.color =
            new Color(
                0.90f,
                0.92f,
                0.94f,
                1f);
    }

    private static void ArrangeButtons(
        Transform root)
    {
        RectTransform buttons =
            FindDirectChildRect(
                root,
                "Buttons");

        if (buttons == null)
        {
            return;
        }

        Record(buttons);

        buttons.anchorMin =
            new Vector2(
                0f,
                0f);

        buttons.anchorMax =
            new Vector2(
                1f,
                0f);

        buttons.pivot =
            new Vector2(
                0.5f,
                0f);

        buttons.sizeDelta =
            new Vector2(
                0f,
                90f);

        buttons.anchoredPosition =
            new Vector2(
                0f,
                48f);

        RectTransform apply =
            FindDirectChildRect(
                buttons,
                "ApplyButton");

        RectTransform close =
            FindDirectChildRect(
                buttons,
                "CloseButton");

        if (apply != null)
        {
            ArrangeButton(
                apply,
                -95f,
                "적용",
                true);
        }

        if (close != null)
        {
            ArrangeButton(
                close,
                -285f,
                "닫기",
                false);
        }
    }

    private static void ArrangeButton(
        RectTransform button,
        float x,
        string textValue,
        bool primary)
    {
        Record(button);

        button.anchorMin =
            new Vector2(
                1f,
                0.5f);

        button.anchorMax =
            new Vector2(
                1f,
                0.5f);

        button.pivot =
            new Vector2(
                1f,
                0.5f);

        button.sizeDelta =
            new Vector2(
                170f,
                56f);

        button.anchoredPosition =
            new Vector2(
                x,
                0f);

        Image image =
            button.GetComponent<Image>();

        if (image != null)
        {
            Record(image);

            image.color =
                primary
                    ? new Color(
                        0.22f,
                        0.36f,
                        0.44f,
                        1f)
                    : new Color(
                        0.10f,
                        0.12f,
                        0.14f,
                        1f);
        }

        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>(
                true);

        if (text == null)
        {
            return;
        }

        Record(text);

        text.text =
            textValue;

        text.fontSize =
            20f;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            Color.white;

        RectTransform textRect =
            text.rectTransform;

        Record(textRect);

        Stretch(textRect);
    }

    private static RectTransform FindDirectChildRect(
        Transform parent,
        string name)
    {
        Transform child =
            parent.Find(name);

        return child as RectTransform;
    }

    private static RectTransform FindRecursiveRect(
        Transform parent,
        string name)
    {
        foreach (Transform child
                 in parent)
        {
            if (child.name == name)
            {
                return child
                    as RectTransform;
            }

            RectTransform result =
                FindRecursiveRect(
                    child,
                    name);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void SetTopLeft(
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

        rect.sizeDelta =
            new Vector2(
                width,
                height);

        rect.anchoredPosition =
            new Vector2(
                x,
                y);
    }

    private static void Stretch(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    private static void Record(
        Object target)
    {
        if (target == null)
        {
            return;
        }

        Undo.RecordObject(
            target,
            "Arrange Settings Menu");
    }
}

#endif