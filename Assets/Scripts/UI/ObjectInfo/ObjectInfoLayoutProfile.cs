using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ObjectInfoLayoutProfile",
    menuName = "Project Jeokru/UI/Object Info Layout Profile")]
public sealed class ObjectInfoLayoutProfile :
    ScriptableObject
{
    [Header("Global Scale")]
    [SerializeField]
    [Min(0.1f)]
    private float uiScale = 1.6f;

    [Header("Popup")]
    [SerializeField]
    private Vector2 popupSize =
        new Vector2(
            264f,
            385f);

    [SerializeField]
    private Vector2 popupMargin =
        new Vector2(
            24f,
            105f);

    [Header("Text Layouts")]
    [SerializeField]
    private List<TextLayout> textLayouts =
        new();

    [Header("Rect Layouts")]
    [SerializeField]
    private List<RectLayout> rectLayouts =
        new();

    public float UiScale =>
        uiScale;

    public Vector2 PopupSize =>
        popupSize;

    public Vector2 PopupMargin =>
        popupMargin;

    public IReadOnlyList<TextLayout>
        TextLayouts =>
            textLayouts;

    public IReadOnlyList<RectLayout>
        RectLayouts =>
            rectLayouts;

    [Serializable]
    public sealed class TextLayout
    {
        [SerializeField]
        private string objectName;

        [SerializeField]
        private string previewText;

        [SerializeField]
        private Vector2 position;

        [SerializeField]
        private Vector2 size;

        [SerializeField]
        [Min(1f)]
        private float fontSize = 15f;

        [SerializeField]
        private TextAlignmentOptions alignment =
            TextAlignmentOptions.Left;

        [SerializeField]
        private bool allowWrapping;

        [SerializeField]
        private bool overrideFontStyle;

        [SerializeField]
        private FontStyles fontStyle =
            FontStyles.Normal;

        public string ObjectName =>
            objectName;

        public string PreviewText =>
            previewText;

        public Vector2 Position =>
            position;

        public Vector2 Size =>
            size;

        public float FontSize =>
            fontSize;

        public TextAlignmentOptions Alignment =>
            alignment;

        public bool AllowWrapping =>
            allowWrapping;

        public bool OverrideFontStyle =>
            overrideFontStyle;

        public FontStyles FontStyle =>
            fontStyle;

        public TextLayout(
            string objectName,
            string previewText,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment,
            bool allowWrapping = false,
            bool overrideFontStyle = false,
            FontStyles fontStyle =
                FontStyles.Normal)
        {
            this.objectName =
                objectName;

            this.previewText =
                previewText;

            this.position =
                position;

            this.size =
                size;

            this.fontSize =
                fontSize;

            this.alignment =
                alignment;

            this.allowWrapping =
                allowWrapping;

            this.overrideFontStyle =
                overrideFontStyle;

            this.fontStyle =
                fontStyle;
        }
    }

    [Serializable]
    public sealed class RectLayout
    {
        [SerializeField]
        private string objectName;

        [SerializeField]
        private Vector2 position;

        [SerializeField]
        private Vector2 size;

        public string ObjectName =>
            objectName;

        public Vector2 Position =>
            position;

        public Vector2 Size =>
            size;

        public RectLayout(
            string objectName,
            Vector2 position,
            Vector2 size)
        {
            this.objectName =
                objectName;

            this.position =
                position;

            this.size =
                size;
        }
    }

#if UNITY_EDITOR
    public void SetDefaults(
        float scale,
        Vector2 size,
        Vector2 margin,
        List<TextLayout> texts,
        List<RectLayout> rects)
    {
        uiScale =
            scale;

        popupSize =
            size;

        popupMargin =
            margin;

        textLayouts =
            texts ?? new();

        rectLayouts =
            rects ?? new();
    }
#endif
}