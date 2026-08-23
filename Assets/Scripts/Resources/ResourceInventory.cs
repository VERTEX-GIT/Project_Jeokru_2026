using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    /* =< 변수 >============================================================================================== */

    // 싱글톤
    public static ResourceInventory Inventory { get; private set; }

    // 자원 보유량
    private readonly Dictionary<ResourceType, int> resourceAmounts = new();
    // 이벤트: 자원 보유량 변경 시 호출
    public event Action<ResourceType, int> ResourceAmountChanged;

    /* =< 기본 메서드 >======================================================================================== */

    private void Awake()
    {
        if (Inventory != null && Inventory != this)
        {
            Destroy(gameObject);
            return;
        }

        Inventory = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Inventory == this)
        {
            Inventory = null;
        }
    }

    /* =< 자원 메서드 >========================================================================================= */

    // 자원 보유량 리셋
    public void ResetResourceAmounts()
    {
        foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
        {
            resourceAmounts[resourceType] = 0;
        }
    }

    // 특정 자원 보유량 반환
    public int GetResourceAmount(ResourceType resourceType)
    {
        return resourceAmounts.TryGetValue(resourceType, out int amount) ? amount : 0;
    }

    // 특정 단일 자원 추가
    public bool Add(ResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        resourceAmounts[resourceType] = GetResourceAmount(resourceType) + amount;

        ResourceAmountChanged?.Invoke(resourceType, GetResourceAmount(resourceType));


        return true;
    }

    // 여러 자원 추가
    public bool Add(IReadOnlyList<ResourceCost> resources)
    {
        if (resources == null || resources.Count == 0)
        {
            return false;
        }

        Dictionary<ResourceType, int> totals = CalculateTotals(resources);

        if (totals.Count == 0)
        {
            return false;
        }

        // 여러 자원 수 만큼 단일 자원 추가 메서드 호출
        foreach (KeyValuePair<ResourceType, int> resource in totals)
        {
            Add(resource.Key, resource.Value);
        }

        return true;
    }

    // 특정 단일 자원 사용
    public bool Spend(ResourceType resourceType, int amount)
    {
        if (amount <= 0 || GetResourceAmount(resourceType) < amount)
        {
            return false;
        }

        resourceAmounts[resourceType] = GetResourceAmount(resourceType) - amount;

        ResourceAmountChanged?.Invoke(resourceType, GetResourceAmount(resourceType));

        return true;
    }

    // 여러 자원 사용
    public bool Spend(IReadOnlyList<ResourceCost> costs, bool logFailure = true)
    {
        if (!CanAfford(costs, logFailure))
        {
            return false;
        }

        Dictionary<ResourceType, int> totals = CalculateTotals(costs);

        // 여러 자원 수 만큼 단일 자원 사용 메서드 호출
        foreach (KeyValuePair<ResourceType, int> cost in totals)
        {
            Spend(cost.Key, cost.Value);
        }

        return true;
    }

    // Spend 할 자원이 충분한지 확인
    public bool CanAfford(IReadOnlyList<ResourceCost> costs, bool logFailure = true)
    {
        if (costs == null || costs.Count == 0)
        {
            return false;
        }

        Dictionary<ResourceType, int> totals = CalculateTotals(costs);

        if (totals.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<ResourceType, int> cost in totals)
        {
            if (GetResourceAmount(cost.Key) < cost.Value)
            {
                if (logFailure)
                {
                    Debug.Log("" + cost.Key + " 자원이 부족합니다. 필요량: " + cost.Value + ", 현재량: " + GetResourceAmount(cost.Key));
                }

                return false;
            }
        }

        return true;
    }

    /*
    -< CalculateTotals() >--------------------------------------------------
    Add 또는 Spend 시 같은 자원이 여러 번 등장할 경우, 자원별 총합 계산

    ex:
    | Iron: 3 |
    | Wood: 5 |
    | Iron: 2 | 일 경우,

    | Iron: 5 |
    | Wood: 5 | 로 반환.
    ------------------------------------------------------------------------
    */
    private static Dictionary<ResourceType, int> CalculateTotals(IReadOnlyList<ResourceCost> resources)
    {
        Dictionary<ResourceType, int> totals = new();

        foreach (ResourceCost resource in resources)
        {
            if (resource == null || resource.Amount <= 0)
            {
                continue;
            }

            if (totals.ContainsKey(resource.ResourceType))
            {
                totals[resource.ResourceType] += resource.Amount;
            }
            else
            {
                totals.Add(resource.ResourceType, resource.Amount);
            }
        }

        return totals;
    }
}
