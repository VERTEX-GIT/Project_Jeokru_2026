using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactoryDefinition", menuName = "Project Jeokru/Factory Definition")]

public class FactoryDefinition : ScriptableObject
{
    // -------------------------------------------------------
    [Header("Identity")]
    [SerializeField]
    private FactoryType factoryType;

    [SerializeField]
    private string displayName;

    // -------------------------------------------------------
    [Header("Production")]

    [SerializeField]
    private ResourceType productionType;

    [SerializeField]
    [Min(0.1f)]
    private float baseProductionTime = 60f;

    [SerializeField]
    [Min(1)]
    private int productionAmount = 1;

    [SerializeField]
    private List<ResourceCost> productionCosts = new();

    // -------------------------------------------------------
    [Header("Durability")]
    [SerializeField]
    [Min(1)]
    private int maxHealth = 100;

    [SerializeField]
    [Min(0)]
    private int defense;

    // -------------------------------------------------------
    [Header("Construction")]
    [SerializeField]
    private List<ResourceCost> installationCosts = new();

    /* ---------------------------------------------------------------------- */

    public FactoryType FactoryType => factoryType;
    public string DisplayName => displayName;

    public ResourceType ProductionType => productionType;
    public float BaseProductionTime => baseProductionTime;
    public int ProductionAmount => productionAmount;
    public IReadOnlyList<ResourceCost> ProductionCosts => productionCosts;

    public int MaxHealth => maxHealth;
    public int Defense => defense;

    public IReadOnlyList<ResourceCost> InstallationCosts => installationCosts;
}
