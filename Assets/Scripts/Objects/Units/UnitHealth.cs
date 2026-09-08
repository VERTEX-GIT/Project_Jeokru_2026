using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
public sealed class UnitHealth :
    MonoBehaviour,
    IDamageable
{
    [field: SerializeField]
    public float CurrentHp
    {
        get;
        private set;
    }

    public float MaxHp =>
        unitCore != null &&
        unitCore.Data != null
            ? unitCore.Data.MaxHp
            : 0f;

    public float HealthRatio =>
        MaxHp > 0f
            ? Mathf.Clamp01(
                CurrentHp / MaxHp)
            : 0f;

    public bool IsAlive =>
        CurrentHp > 0f;

    public event Action<float, float>
        HealthChanged;

    private UnitCore unitCore;

    private void Awake()
    {
        unitCore =
            GetComponent<UnitCore>();

        if (unitCore.Data == null)
        {
            Debug.LogError(
                $"{name}: UnitHealth가 사용할 " +
                $"UnitData가 없습니다.",
                this);

            return;
        }

        CurrentHp =
            unitCore.Data.MaxHp;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    public void TakeDamage(
        float attackPower,
        GameObject attacker)
    {
        if (!IsAlive ||
            unitCore == null ||
            !unitCore.IsActive ||
            unitCore.Data == null)
        {
            return;
        }

        float damage =
            Mathf.Max(
                0f,
                attackPower -
                unitCore.Data.Defense);

        if (damage <= 0f)
        {
            return;
        }

        CurrentHp =
            Mathf.Max(
                0f,
                CurrentHp -
                damage);

        NotifyHealthChanged();

        if (CurrentHp <= 0f)
        {
            HandleDeath();
            return;
        }

        if (TryGetComponent(
                out UnitWorkRecovery
                    workRecovery))
        {
            workRecovery
                .TrySaveCurrentWork();
        }

        TryRetargetToAttacker(
            attacker);
    }

    public void EnsureMinimumHp(
        float minimumHp)
    {
        if (unitCore == null ||
            unitCore.Data == null)
        {
            return;
        }

        float clampedMinimum =
            Mathf.Clamp(
                minimumHp,
                0f,
                MaxHp);

        if (CurrentHp >=
            clampedMinimum)
        {
            return;
        }

        CurrentHp =
            clampedMinimum;

        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(
            CurrentHp,
            MaxHp);
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

        if (unitCore
            .IsPlayerMoveCommandActive)
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

        unitCore.SetTarget(
            attacker);
    }

    private void HandleDeath()
    {
        if (unitCore == null ||
            unitCore.Data == null)
        {
            return;
        }

        if (unitCore.Data.Team ==
                UnitTeam.Ally &&
            unitCore.Data.IsBasicUnit)
        {
            HandleBasicAllyDown();
            return;
        }

        Destroy(gameObject);
    }

    private void HandleBasicAllyDown()
    {
        unitCore.SetUnitActive(
            false);

        unitCore.SetAutoCombat(
            false);

        unitCore
            .SetPlayerMoveCommandActive(
                false);

        unitCore.ClearTarget();

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

        if (TryGetComponent(
                out UnitWorkRecovery recovery))
        {
            recovery
                .ClearInterruptedWork();
        }
    }
}