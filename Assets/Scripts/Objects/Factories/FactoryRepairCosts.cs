using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public sealed class FactoryRepairCosts
{
    [SerializeField, InspectorName("HP 0~20%")]
    private List<ResourceCost> hp0To20 = new();

    [SerializeField, InspectorName("HP 20~40%")]
    private List<ResourceCost> hp20To40 = new();

    [SerializeField, InspectorName("HP 40~60%")]
    private List<ResourceCost> hp40To60 = new();

    [SerializeField, InspectorName("HP 60~80%")]
    private List<ResourceCost> hp60To80 = new();

    [SerializeField, InspectorName("HP 80~100%")]
    private List<ResourceCost> hp80To100 = new();

    // 공장 수리 비용 반환 (HP 비율에 따라 다르게 반환)
    public IReadOnlyList<ResourceCost> GetRepairCosts(float hpRate)
    {
        if (hpRate <= 0.2f) return hp0To20;
        if (hpRate <= 0.4f) return hp20To40;
        if (hpRate <= 0.6f) return hp40To60;
        if (hpRate <= 0.8f) return hp60To80;

        return hp80To100;
    }
}
