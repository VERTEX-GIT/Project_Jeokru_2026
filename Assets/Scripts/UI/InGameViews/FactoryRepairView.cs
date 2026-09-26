using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FactoryRepairView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private FactoryRepair repair;
    private FactoryHealth health;

    private void Awake()
    {
        repair = GetComponentInParent<FactoryRepair>();
        health = GetComponentInParent<FactoryHealth>();
        button.onClick.AddListener(Repair);
    }

    private void OnDestroy() => button.onClick.RemoveListener(Repair);

    private void LateUpdate()
    {
        if (repair == null || health == null) return;

        IReadOnlyList<ResourceCost> costs = repair.GetRepairCosts();
        int iron = 0;
        foreach (ResourceCost cost in costs)
            if (cost.ResourceType == ResourceType.Iron) iron += cost.Amount;

        bool full = health.CurrentHp >= health.MaxHp;
        label.text = full ? "수리 완료" : $"수리하기: 철 {iron}개";
        ResourceInventory inventory = ResourceInventory.Inventory;
        button.interactable = !full &&
            !GameplayPauseController.HasReason(GameplayPauseController.PauseReason.GameOver) &&
            inventory != null && inventory.CanAfford(costs, false);
    }

    private void Repair()
    {
        repair?.TryRepair();
    }
}
