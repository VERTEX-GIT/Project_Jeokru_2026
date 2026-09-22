using UnityEngine;
using UnityEngine.UI;

// Texture-free circular sector: starts at twelve o'clock and fills clockwise.
[ExecuteAlways]
public sealed class ProductionCircleGraphic : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float amount = 1f;

    public float FillAmount
    {
        get => amount;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(amount, value)) return;
            amount = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (amount <= 0f) return;
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        int segments = Mathf.Max(1, Mathf.CeilToInt(96 * amount));
        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = amount * Mathf.PI * 2f * i / segments;
            vh.AddVert(center + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius, color, Vector2.zero);
            if (i > 0) vh.AddTriangle(0, i, i + 1);
        }
    }
}
