using System;
using System.Collections.Generic;
using UnityEngine;

// 공장 수리 결과를 나타내는 열거형
public enum FactoryRepairResult
{
    Success,            // 성공
    AlreadyFull,        // 이미 최대 체력
    NotEnoughResources, // 자원 부족
    Unavailable         // 사용 불가
}

[DisallowMultipleComponent]
[RequireComponent(typeof(FactoryCore))]
[RequireComponent(typeof(FactoryHealth))]

public sealed class FactoryRepair : MonoBehaviour
{
    /* =< 변수 >==================================================================================================== */

    private FactoryCore factoryCore;
    private FactoryHealth factoryHealth;
    private ResourceInventory resourceInventory;


    /* =< 기본 메서드 >============================================================================================= */

    private void Awake()
    {
        factoryCore = GetComponent<FactoryCore>();
        factoryHealth = GetComponent<FactoryHealth>();
        resourceInventory = ResourceInventory.Inventory;
    }

    /* =< 수리 관련 메서드 >========================================================================================= */

    // 현재 체력 비율(0~1)
    public float CurHPRate()
    {
        if (factoryHealth == null || factoryHealth.MaxHp <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(factoryHealth.CurrentHp / factoryHealth.MaxHp);
    }

    // 현재 HP 구간에 해당하는 수리 비용 반환
    public IReadOnlyList<ResourceCost> GetRepairCosts()
    {
        if (factoryCore == null || factoryCore.Definition == null)
        {
            return Array.Empty<ResourceCost>(); // 빈 배열 반환
        }

        return factoryCore.Definition.GetRepairCosts(CurHPRate());
    }

    // 수리 비용을 소비하고 공장 HP를 최대치까지 회복
    public FactoryRepairResult TryRepair()
    {   
        // 사용 불가
        if (factoryCore == null || factoryCore.Definition == null || factoryHealth == null)
        {
            return FactoryRepairResult.Unavailable;
        }

        // 이미 최대 체력
        if (factoryHealth.CurrentHp >= factoryHealth.MaxHp)
        {
            return FactoryRepairResult.AlreadyFull;
        }

        if (resourceInventory == null)
        {
            resourceInventory = ResourceInventory.Inventory;
        }

        IReadOnlyList<ResourceCost> costs = GetRepairCosts();

        // 자원 부족 또는 수리 비용이 없는 경우
        if (resourceInventory == null || costs.Count == 0 || !resourceInventory.Spend(costs, false))
        {
            return resourceInventory == null || costs.Count == 0
                ? FactoryRepairResult.Unavailable
                : FactoryRepairResult.NotEnoughResources;
        }

        // 수리 시도
        return factoryHealth.Repair()
            ? FactoryRepairResult.Success
            : FactoryRepairResult.Unavailable;
    }
}