using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
public sealed class UnitTargeting : MonoBehaviour
{
    private const float EmptySearchInterval = 0.2f;

    private UnitCore unitCore;
    private float nextSearchTime;

    private void Awake()
    {
        unitCore = GetComponent<UnitCore>();
    }

    private void Start()
    {
        if (CanAutoTarget())
        {
            TryAcquireTarget();
        }
    }

    private void Update()
    {
        if (!CanAutoTarget())
        {
            return;
        }

        // 현재 타겟이 아직 유효하면 유지
        if (IsCurrentTargetValid())
        {
            return;
        }

        // 타겟이 없을 때 매 프레임 전체 유닛을 검색하지 않도록 제한
        if (Time.time < nextSearchTime)
        {
            return;
        }

        bool acquired = TryAcquireTarget();

        if (!acquired)
        {
            nextSearchTime =
                Time.time + EmptySearchInterval;
        }
    }

    private bool CanAutoTarget()
    {
        if (unitCore == null ||
            !unitCore.IsActive ||
            unitCore.Data == null)
        {
            return false;
        }

        // 적군은 항상 자동전투
        if (unitCore.Data.Team == UnitTeam.Enemy)
        {
            return true;
        }

        // 아군은 자동전투 상태일 때만
        return unitCore.Data.Team == UnitTeam.Ally &&
               unitCore.IsAutoCombat;
    }

    private bool IsCurrentTargetValid()
    {
        GameObject target =
            unitCore.CurrentTarget;

        if (target == null)
        {
            return false;
        }

        // 유닛
        if (target.TryGetComponent(
                out UnitCore targetUnit))
        {
            return targetUnit.IsActive &&
                   targetUnit.Data != null &&
                   targetUnit.Data.Team !=
                       unitCore.Data.Team;
        }

        // 공장은 적군만 타겟 가능
        if (unitCore.Data.Team == UnitTeam.Enemy &&
            target.TryGetComponent(
                out FactoryHealth factoryHealth))
        {
            return factoryHealth.IsAlive;
        }

        return false;
    }

    public bool TryAcquireTarget()
    {
        if (!CanAutoTarget())
        {
            return false;
        }

        List<GameObject> nearestTargets =
            new();

        float nearestDistanceSqr =
            float.MaxValue;

        FindNearestEnemyUnits(
            nearestTargets,
            ref nearestDistanceSqr);

        // 공장은 적군만 공격 가능
        if (unitCore.Data.Team ==
            UnitTeam.Enemy)
        {
            FindNearestFactories(
                nearestTargets,
                ref nearestDistanceSqr);
        }

        if (nearestTargets.Count == 0)
        {
            unitCore.ClearTarget();
            return false;
        }

        GameObject selectedTarget =
            nearestTargets[
                Random.Range(
                    0,
                    nearestTargets.Count)];

        unitCore.SetTarget(selectedTarget);

        nextSearchTime = 0f;

        return true;
    }

    private void FindNearestEnemyUnits(
        List<GameObject> nearestTargets,
        ref float nearestDistanceSqr)
    {
        UnitCore[] units =
            FindObjectsByType<UnitCore>(
                FindObjectsSortMode.None);

        foreach (UnitCore candidate in units)
        {
            if (candidate == null ||
                candidate == unitCore ||
                !candidate.IsActive ||
                candidate.Data == null ||
                candidate.Data.Team ==
                    unitCore.Data.Team)
            {
                continue;
            }

            ConsiderTarget(
                candidate.gameObject,
                nearestTargets,
                ref nearestDistanceSqr);
        }
    }

    private void FindNearestFactories(
        List<GameObject> nearestTargets,
        ref float nearestDistanceSqr)
    {
        FactoryCore[] factories =
            FindObjectsByType<FactoryCore>(
                FindObjectsSortMode.None);

        foreach (FactoryCore factory in factories)
        {
            if (factory == null ||
                !factory.TryGetComponent(
                    out FactoryHealth health) ||
                !health.IsAlive)
            {
                continue;
            }

            ConsiderTarget(
                factory.gameObject,
                nearestTargets,
                ref nearestDistanceSqr);
        }
    }

    private void ConsiderTarget(
        GameObject candidate,
        List<GameObject> nearestTargets,
        ref float nearestDistanceSqr)
    {
        float distanceSqr =
            (candidate.transform.position -
             transform.position)
            .sqrMagnitude;

        if (distanceSqr <
            nearestDistanceSqr)
        {
            nearestDistanceSqr =
                distanceSqr;

            nearestTargets.Clear();
            nearestTargets.Add(candidate);

            return;
        }

        if (Mathf.Approximately(
                distanceSqr,
                nearestDistanceSqr))
        {
            nearestTargets.Add(candidate);
        }
    }
}