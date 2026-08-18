using System;
using UnityEngine;

[Serializable]
public sealed class ResourceCost
{
    [SerializeField]
    private ResourceType resourceType;

    [SerializeField]
    [Min(1)]
    private int amount = 1;

    public ResourceType ResourceType => resourceType;
    public int Amount => amount;
}