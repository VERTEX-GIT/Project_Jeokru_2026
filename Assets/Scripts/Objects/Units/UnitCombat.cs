using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
public sealed class UnitCombat : MonoBehaviour
{
    [field: SerializeField]
    public float CooldownRemaining { get; private set; }

    [Header("Ranged Attack")]

    [SerializeField]
    private Projectile projectilePrefab;

    [SerializeField]
    private Transform projectileSpawnPoint;

    private UnitCore unitCore;
    private UnitMovement movement;
    private TileObjectPlacement placement;
    private TileOccupancyManager occupancyManager;
    private TileCoordinateManager coordinateManager;

    private void Awake()
    {
        unitCore = GetComponent<UnitCore>();

        movement = GetComponent<UnitMovement>();
        placement = GetComponent<TileObjectPlacement>();

        occupancyManager = TileOccupancyManager.Instance;

        if (occupancyManager == null)
        {
            occupancyManager =
                FindAnyObjectByType<TileOccupancyManager>();
        }

        if (occupancyManager != null)
        {
            coordinateManager =
                occupancyManager.CoordinateManager;
        }
    }

    private void Update()
    {
        UpdateCooldown();

        if (!CanCombat())
        {
            return;
        }

        switch (unitCore.Data.AttackType)
        {
            case UnitAttackType.Melee:
                TryMeleeAttack();
                break;

            case UnitAttackType.Ranged:
                TryRangedAttack();
                break;
        }
    }

    private void UpdateCooldown()
    {
        if (CooldownRemaining <= 0f)
        {
            return;
        }

        CooldownRemaining =
            Mathf.Max(
                0f,
                CooldownRemaining - Time.deltaTime);
    }

    private bool CanCombat()
    {
        if (unitCore == null ||
            !unitCore.IsActive ||
            unitCore.Data == null ||
            unitCore.CurrentTarget == null)
        {
            return false;
        }

        GameObject target =
            unitCore.CurrentTarget;

        if (target.TryGetComponent(
                out UnitCore targetUnit))
        {
            return targetUnit.IsActive &&
                targetUnit.Data != null &&
                targetUnit.Data.Team !=
                    unitCore.Data.Team;
        }

        if (target.TryGetComponent(
                out FactoryHealth factory))
        {
            return unitCore.Data.Team ==
                    UnitTeam.Enemy &&
                factory.IsAlive;
        }

        return false;
    }

    private void TryMeleeAttack()
    {
        if (CooldownRemaining > 0f)
        {
            return;
        }

        GameObject target =
            unitCore.CurrentTarget;

        // 현재 타겟이 공격 범위 안에 있어야 공격 자체가 발동
        if (target == null ||
            !IsTargetInAttackRange(target))
        {
            return;
        }

        bool hitAnyTarget = false;

        // 공격 범위 안의 모든 상대 유닛에게 피해
        UnitCore[] units =
            FindObjectsByType<UnitCore>(
                FindObjectsSortMode.None);

        foreach (UnitCore candidate in units)
        {
            if (!CanHitUnit(candidate))
            {
                continue;
            }

            if (!IsUnitInAttackRange(candidate))
            {
                continue;
            }

            if (!candidate.TryGetComponent(
                    out IDamageable damageable) ||
                !damageable.IsAlive)
            {
                continue;
            }

            damageable.TakeDamage(
                unitCore.Data.AttackPower,
                gameObject);

            hitAnyTarget = true;
        }

        // 적군이 공장을 현재 타겟으로 삼고 있다면
        // 공장도 근거리 공격 대상이 될 수 있음
        if (unitCore.Data.Team == UnitTeam.Enemy &&
            target.TryGetComponent(
                out FactoryHealth factoryHealth) &&
            factoryHealth.IsAlive &&
            IsTargetInAttackRange(target))
        {
            factoryHealth.TakeDamage(
                unitCore.Data.AttackPower,
                gameObject);

            hitAnyTarget = true;
        }

        if (!hitAnyTarget)
        {
            return;
        }

        CooldownRemaining =
            unitCore.Data.AttackCooldown;
    }

    private bool IsUnitInAttackRange(
        UnitCore targetUnit)
    {
        if (targetUnit == null ||
            coordinateManager == null ||
            placement == null ||
            !targetUnit.TryGetComponent(
                out UnitMovement targetMovement) ||
            !targetUnit.TryGetComponent(
                out TileObjectPlacement targetPlacement))
        {
            return false;
        }

        Vector3Int attackerCell =
            GetCurrentUnitCell();

        Vector3Int targetCell;

        if (targetMovement.IsMoving)
        {
            targetCell =
                coordinateManager.WorldToCell(
                    targetUnit.transform.position);
        }
        else
        {
            targetCell =
                targetPlacement.AnchorCell;
        }

        return IsCellInsideRange(
            attackerCell,
            targetCell,
            unitCore.Data.AttackRange);
    }

    private bool CanHitUnit(
        UnitCore candidate)
    {
        if (candidate == null ||
            candidate == unitCore ||
            !candidate.IsActive ||
            candidate.Data == null)
        {
            return false;
        }

        return candidate.Data.Team !=
            unitCore.Data.Team;
    }

    private bool IsTargetInAttackRange(
        GameObject target)
    {
        if (target == null ||
            coordinateManager == null ||
            placement == null)
        {
            return false;
        }

        Vector3Int attackerCell =
            GetCurrentUnitCell();

        float attackRange =
            unitCore.Data.AttackRange;

        // 유닛 타겟
        if (target.TryGetComponent(
                out UnitMovement targetMovement) &&
            target.TryGetComponent(
                out TileObjectPlacement targetPlacement))
        {
            Vector3Int targetCell;

            if (targetMovement.IsMoving)
            {
                targetCell =
                    coordinateManager.WorldToCell(
                        target.transform.position);
            }
            else
            {
                targetCell =
                    targetPlacement.AnchorCell;
            }

            return IsCellInsideRange(
                attackerCell,
                targetCell,
                attackRange);
        }

        // 공장 타겟
        if (target.TryGetComponent(
                out FactoryCore _) &&
            target.TryGetComponent(
                out TileObjectPlacement factoryPlacement))
        {
            foreach (Vector3Int occupiedCell
                    in factoryPlacement.OccupiedCells)
            {
                if (IsCellInsideRange(
                        attackerCell,
                        occupiedCell,
                        attackRange))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void TryRangedAttack()
    {
        if (CooldownRemaining > 0f)
        {
            return;
        }

        if (movement != null &&
        movement.IsMoving)
        {
            return;
        }

        GameObject target =
            unitCore.CurrentTarget;

        if (target == null ||
            !IsTargetInAttackRange(target))
        {
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError(
                $"{name}: Projectile Prefab이 지정되지 않았습니다.",
                this);

            return;
        }

        Vector3 spawnPosition =
            projectileSpawnPoint != null
                ? projectileSpawnPoint.position
                : transform.position;

        Vector3 targetPosition =
            GetProjectileTargetPosition(target);

        Vector2 direction =
            targetPosition - spawnPosition;

        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Projectile projectile =
            Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity);

        projectile.Initialize(
            direction,
            unitCore.Data.AttackPower,
            unitCore.Data.Team,
            gameObject);

        CooldownRemaining =
            unitCore.Data.AttackCooldown;
    }

    private Vector3 GetProjectileTargetPosition(
        GameObject target)
    {
        if (target == null)
        {
            return transform.position;
        }

        // 유닛은 현재 실제 위치를 향해 발사
        if (target.TryGetComponent<UnitCore>(
                out _))
        {
            return target.transform.position;
        }

        // 공장은 가장 가까운 점유 타일 중심을 향해 발사
        if (target.TryGetComponent(
                out FactoryCore _) &&
            target.TryGetComponent(
                out TileObjectPlacement factoryPlacement) &&
            coordinateManager != null)
        {
            Vector3 nearestPosition =
                target.transform.position;

            float nearestDistanceSqr =
                float.MaxValue;

            foreach (Vector3Int cell
                    in factoryPlacement.OccupiedCells)
            {
                Vector3 position =
                    coordinateManager.CellToWorldCenter(
                        cell);

                float distanceSqr =
                    (position - transform.position)
                    .sqrMagnitude;

                if (distanceSqr >=
                    nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr =
                    distanceSqr;

                nearestPosition =
                    position;
            }

            return nearestPosition;
        }

        return target.transform.position;
    }

    private bool IsCellInsideRange(
        Vector3Int attackerCell,
        Vector3Int targetCell,
        float range)
    {
        if (range < 0f)
        {
            return false;
        }

        Vector3Int offset =
            attackerCell - targetCell;

        float x =
            Mathf.Max(
                Mathf.Abs(offset.x) - 0.5f,
                0f);

        float y =
            Mathf.Max(
                Mathf.Abs(offset.y) - 0.5f,
                0f);

        float minDistanceSqr =
            x * x + y * y;

        float rangeSqr =
            range * range;

        return minDistanceSqr <= rangeSqr;
    }

    private Vector3Int GetCurrentUnitCell()
    {
        if (movement != null &&
            movement.IsMoving)
        {
            return coordinateManager.WorldToCell(
                transform.position);
        }

        return placement.AnchorCell;
    }
}