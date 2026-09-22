using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class FactoryStatusView : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private GameObject productionIndicator;
    [SerializeField] private ProductionCircleGraphic productionFill;
    [SerializeField] private Image productIcon;
    [Tooltip("ResourceType order: Iron, Red, Blue, Purple, Green")]
    [SerializeField] private Sprite[] productSprites;

    private FactoryHealth health;
    private FactoryProduction production;
    private FactoryCore core;

    private void OnEnable()
    {
        health = GetComponentInParent<FactoryHealth>();
        production = GetComponentInParent<FactoryProduction>();
        core = GetComponentInParent<FactoryCore>();
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (health == null || production == null || core == null) return;
        bool alive = !Application.isPlaying || health.IsAlive;
        if (healthFill != null)
            healthFill.fillAmount = !Application.isPlaying ? 1f :
                (health.MaxHp > 0f ? Mathf.Clamp01(health.CurrentHp / health.MaxHp) : 0f);
        if (productionIndicator != null && productionIndicator.activeSelf != alive)
            productionIndicator.SetActive(alive);
        if (productionFill != null)
            productionFill.FillAmount = Application.isPlaying && alive
                ? production.ProductionProgressRate : 0f;
        if (productIcon != null && core.Definition != null)
        {
            int index = (int)core.Definition.ProductionType;
            productIcon.sprite = productSprites != null && index >= 0 && index < productSprites.Length
                ? productSprites[index] : null;
            productIcon.enabled = productIcon.sprite != null;
        }
    }
}
