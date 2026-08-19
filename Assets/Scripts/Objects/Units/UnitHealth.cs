using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
public sealed class UnitHealth : MonoBehaviour, IDamageable
{
    [field: SerializeField]
    public float CurrentHp { get; private set; }

    public bool IsAlive => CurrentHp > 0f;

    private UnitCore unitCore;

    private void Awake()
    {
        unitCore = GetComponent<UnitCore>();

        if (unitCore.Data == null)
        {
            Debug.LogError(
                $"{name}: UnitHealth가 사용할 UnitData가 없습니다.",
                this);

            return;
        }

        CurrentHp = unitCore.Data.MaxHp;
    }

    public void TakeDamage(
        float attackPower,
        GameObject attacker)
    {
        if (!IsAlive ||
            unitCore == null ||
            unitCore.Data == null)
        {
            return;
        }

        float damage =
            Mathf.Max(
                0f,
                attackPower - unitCore.Data.Defense);

        CurrentHp =
            Mathf.Max(
                0f,
                CurrentHp - damage);

        if (CurrentHp <= 0f)
        {
            HandleBasicUnitDown();
            return;
        }

        TryRetargetToAttacker(attacker);
    }

    private void TryRetargetToAttacker(
        GameObject attacker)
    {
        if (attacker == null ||
            unitCore == null ||
            unitCore.Data == null)
        {
            return;
        }

        if (!attacker.TryGetComponent(
                out UnitCore attackerUnit) ||
            attackerUnit.Data == null ||
            !attackerUnit.IsActive)
        {
            return;
        }

        if (attackerUnit.Data.Team ==
            unitCore.Data.Team)
        {
            return;
        }

        unitCore.SetTarget(attacker);
    }

    private void HandleBasicUnitDown()
    {
        unitCore.SetUnitActive(false);

        if (TryGetComponent(
                out UnitMovement movement))
        {
            movement.CancelMovement();
        }

        if (TryGetComponent(
                out UnitSelectable selectable))
        {
            selectable.Deselect();
        }
    }
}