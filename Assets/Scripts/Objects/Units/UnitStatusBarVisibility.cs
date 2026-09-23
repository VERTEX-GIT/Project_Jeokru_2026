using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitStatusBarVisibility : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private UnitHealth unitHealth;

    [SerializeField]
    private UnitStress unitStress;

    [SerializeField]
    private UnitHealthBar healthBar;

    [SerializeField]
    private UnitStressBar stressBar;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (unitHealth != null)
        {
            unitHealth.HealthChanged +=
                OnHealthChanged;
        }

        if (unitStress != null)
        {
            unitStress.StressChanged +=
                OnStressChanged;
        }

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (unitHealth != null)
        {
            unitHealth.HealthChanged -=
                OnHealthChanged;
        }

        if (unitStress != null)
        {
            unitStress.StressChanged -=
                OnStressChanged;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            RefreshVisibility();
        }
    }
#endif

    private void ResolveReferences()
    {
        if (unitHealth == null)
        {
            unitHealth =
                GetComponentInParent<
                    UnitHealth>();
        }

        if (unitStress == null)
        {
            unitStress =
                GetComponentInParent<
                    UnitStress>();
        }

        if (healthBar == null)
        {
            healthBar =
                GetComponentInChildren<
                    UnitHealthBar>(
                    true);
        }

        if (stressBar == null)
        {
            stressBar =
                GetComponentInChildren<
                    UnitStressBar>(
                    true);
        }
    }

    private void OnHealthChanged(
        float currentHp,
        float maxHp)
    {
        RefreshVisibility();
    }

    private void OnStressChanged(
        float currentStress,
        float maxStress)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool hasHealthIssue =
            unitHealth != null &&
            unitHealth.HealthRatio < 1f;

        bool hasStress =
            unitStress != null &&
            unitStress.CurrentStress > 0f;

        bool shouldShow =
            hasHealthIssue ||
            hasStress;

        healthBar?.SetVisible(
            shouldShow);

        stressBar?.SetVisible(
            shouldShow);
    }
}
