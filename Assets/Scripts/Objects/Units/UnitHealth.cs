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
            HandleDeath();
            return;
        }

        if (TryGetComponent(
                out UnitWorkRecovery workRecovery))
        {
            workRecovery.TrySaveCurrentWork();
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

        // 플레이어가 직접 내린 이동 명령 수행 중에는
        // 피격으로 전투 타겟을 설정하지 않는다.
        if (unitCore.IsPlayerMoveCommandActive)
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

    private void HandleDeath()
    {
        if (unitCore == null ||
            unitCore.Data == null)
        {
            return;
        }

        // 기본 아군만 게임에서 제거하지 않고 활동 정지
        if (unitCore.Data.Team == UnitTeam.Ally &&
            unitCore.Data.IsBasicUnit)
        {
            HandleBasicAllyDown();
            return;
        }

        // 비기본 아군과 모든 적군은 제거
        Destroy(gameObject);
    }

    private void HandleBasicAllyDown()
    {
        unitCore.SetUnitActive(false);

        unitCore.SetAutoCombat(false);
        unitCore.SetPlayerMoveCommandActive(false);
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
            recovery.ClearInterruptedWork();
        }
    }
}