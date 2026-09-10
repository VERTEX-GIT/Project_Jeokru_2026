using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// InGame 플레이 모드에서 실행. 테스트로 만든 공장과 변경한 재고는 즉시 복구한다.
public static class FactoryPlacementCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Project Jeokru/Check Factory Placement (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying && !PauseMenu.IsPaused, "Run in unpaused InGame play mode");
        var controller = UnityEngine.Object.FindAnyObjectByType<ObjectPlacementController>();
        var validator = UnityEngine.Object.FindAnyObjectByType<PlacementValidator>();
        var occupancy = TileOccupancyManager.Instance;
        var inventory = ResourceInventory.Inventory;
        Require(controller != null && inventory != null && occupancy != null, "Missing scene systems");
        Require(controller.CurrentMode == PlacementMode.None, "Cancel existing placement first");
        var provider = controller.GetComponent<PlacementObjectProvider>();
        var prefabField = typeof(PlacementObjectProvider).GetField("factoryPrefab", Private);
        var previousPrefab = prefabField.GetValue(provider);
        var amounts = (Dictionary<ResourceType, int>)typeof(ResourceInventory)
            .GetField("resourceAmounts", Private).GetValue(inventory);
        var savedAmounts = new Dictionary<ResourceType, int>(amounts);
        var originalFactories = new HashSet<FactoryCore>(UnityEngine.Object.FindObjectsByType<FactoryCore>());
        var place = typeof(ObjectPlacementController).GetMethod("TryPlaceAt", Private);
        var names = new[] { "Mine", "RedMedFac", "BlueMedFac", "PurpleMedFac", "GreenMedFac" };
        var paths = new[] { "Mine", "RedMedFactory", "BlueMedFac", "PurpMedFac", "GrnMedFac" };
        var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        var bounds = occupancy.CoordinateManager.GetComponent<Tilemap>().cellBounds;
        Vector3Int? availableCell = null;
        foreach (var candidate in bounds.allPositionsWithin)
        {
            if (!validator.CanPlaceFactory(candidate)) continue;
            availableCell = candidate;
            break;
        }
        Require(availableCell.HasValue, "No available factory area");
        var cell = availableCell.Value;
        bool Place(Vector3Int position) => (bool)place.Invoke(controller, new object[] { position });

        void RemoveNewFactories()
        {
            foreach (var factory in UnityEngine.Object.FindObjectsByType<FactoryCore>(FindObjectsInactive.Include))
            {
                if (originalFactories.Contains(factory)) continue;
                factory.GetComponent<TileObjectPlacement>().RemoveFromTiles();
                UnityEngine.Object.DestroyImmediate(factory.gameObject);
            }
        }

        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefab/Facility/" + paths[i] + ".prefab").GetComponent<TileObjectPlacement>();
                var definition = prefab.GetComponent<FactoryCore>().Definition;
                var button = buttons.Single(b => b.name == names[i]);
                Require(button.onClick.GetPersistentEventCount() == 1 &&
                    button.onClick.GetPersistentTarget(0) == controller &&
                    button.onClick.GetPersistentMethodName(0) == "BeginFactoryPlacement", "Button wiring " + names[i]);

                amounts.Clear();
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType))) amounts[type] = 10000;
                button.onClick.Invoke();
                Require(provider.FactoryDefinition == definition, "Selected prefab " + names[i]);
                Require(controller.BlocksWorldInput, "Selection must block unit input");
                var before = new Dictionary<ResourceType, int>(amounts);
                Require(!Place(new Vector3Int(10000, 10000)), "Reject outside map");
                Require(before.All(p => inventory.GetResourceAmount(p.Key) == p.Value), "Invalid placement spent resources");
                Require(Place(cell), "Valid placement " + names[i]);
                Require(controller.CurrentMode == PlacementMode.None && controller.InputConsumedThisFrame, "Finish input guard");
                Require(occupancy.TryGetOccupant(cell, out var placed) &&
                    placed.GetComponent<FactoryCore>().Definition == definition, "Wrong placed factory");
                foreach (var pair in before)
                {
                    int cost = definition.InstallationCosts.Where(c => c != null && c.ResourceType == pair.Key)
                        .Sum(c => c.Amount);
                    Require(inventory.GetResourceAmount(pair.Key) == pair.Value - cost, "Installation cost");
                }
                controller.BeginFactoryPlacement(prefab);
                int iron = inventory.GetResourceAmount(ResourceType.Iron);
                Require(!Place(cell), "Reject occupied cell");
                Require(inventory.GetResourceAmount(ResourceType.Iron) == iron, "Occupied cell spent resources");
                Require(controller.CancelPlacement() && !controller.CancelPlacement(), "Cancel/idempotency");
                Require(inventory.GetResourceAmount(ResourceType.Iron) == iron, "Cancel spent resources");
                RemoveNewFactories();
                Require(validator.CanPlaceFactory(cell), "Occupancy/work area cleanup");

                if (definition.InstallationCosts.Count > 0)
                {
                    amounts.Clear();
                    controller.BeginFactoryPlacement(prefab);
                    Require(!Place(cell) && validator.CanPlaceFactory(cell), "Reject insufficient resources");
                    Require(amounts.Count == 0, "Insufficient resources mutated inventory");
                    controller.CancelPlacement();
                }
            }
            var pauseTest = new GameObject("FactoryPlacementPauseCheck");
            try
            {
                var pause = pauseTest.AddComponent<PauseMenu>();
                var escape = typeof(PauseMenu).GetMethod("HandleEscapePressed", Private);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefab/Facility/Mine.prefab").GetComponent<TileObjectPlacement>();
                controller.BeginFactoryPlacement(prefab);
                escape.Invoke(pause, null);
                Require(!PauseMenu.IsPaused && controller.CurrentMode == PlacementMode.None, "Escape pause-first order");
                controller.BeginFactoryPlacement(prefab);
                controller.CancelPlacement();
                escape.Invoke(pause, null);
                Require(!PauseMenu.IsPaused, "Escape placement-first order");
            }
            finally { UnityEngine.Object.DestroyImmediate(pauseTest); }
            Debug.Log("FACTORY_CHECK PASS: five buttons/prefabs, exact costs, occupied/outside/insufficient rejection, cancellation, Escape ordering and cleanup");
        }
        finally
        {
            controller.CancelPlacement();
            RemoveNewFactories();
            amounts.Clear();
            foreach (var pair in savedAmounts) amounts[pair.Key] = pair.Value;
            prefabField.SetValue(provider, previousPrefab);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("FACTORY_CHECK: " + message);
    }
}
