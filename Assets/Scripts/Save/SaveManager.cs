using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const int CurrentSaveVersion = 1;
    private const string SaveFileName = "save.json";

    public static string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            SaveFileName);

    public static bool HasSave =>
        File.Exists(
            SavePath);

    public static bool Save(
        GameTimeManager gameTimeManager)
    {
        if (gameTimeManager == null)
        {
            Debug.LogError(
                "SaveManager: GameTimeManager가 없습니다.");

            return false;
        }

        ResourceInventory inventory =
            ResourceInventory.Inventory;

        if (inventory == null)
        {
            Debug.LogError(
                "SaveManager: ResourceInventory가 없습니다.");

            return false;
        }

        GameSaveData saveData =
            CreateSaveData(
                gameTimeManager,
                inventory);

        try
        {
            string json =
                JsonUtility.ToJson(
                    saveData,
                    true);

            File.WriteAllText(
                SavePath,
                json);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "SaveManager: 저장 파일을 작성하지 못했습니다.\n" +
                exception);

            return false;
        }
    }

    public static bool TryLoad(
        GameTimeManager gameTimeManager)
    {
        return TryLoad(
            gameTimeManager,
            null,
            null);
    }

    public static bool TryLoad(
        GameTimeManager gameTimeManager,
        PlacementObjectProvider objectProvider)
    {
        return TryLoad(
            gameTimeManager,
            objectProvider,
            null);
    }

    public static bool TryLoad(
        GameTimeManager gameTimeManager,
        PlacementObjectProvider objectProvider,
        IReadOnlyList<UnitData> unitDataCatalog)
    {
        if (gameTimeManager == null)
        {
            Debug.LogError(
                "SaveManager: GameTimeManager가 없습니다.");

            return false;
        }

        ResourceInventory inventory =
            ResourceInventory.Inventory;

        if (inventory == null)
        {
            Debug.LogError(
                "SaveManager: ResourceInventory가 없습니다.");

            return false;
        }

        if (!TryReadSaveData(
                out GameSaveData saveData))
        {
            return false;
        }

        if (saveData.gameTime == null)
        {
            Debug.LogError(
                "SaveManager: 저장 파일에 GameTime 데이터가 없습니다.");

            return false;
        }

        if (saveData.resources == null)
        {
            Debug.LogError(
                "SaveManager: 저장 파일에 Resource 데이터가 없습니다.");

            return false;
        }

        inventory.RestoreResourceAmounts(
            saveData.resources);

        if (objectProvider != null &&
            saveData.factories != null)
        {
            if (!RestoreFactories(
                    saveData.factories,
                    objectProvider))
            {
                return false;
            }
        }

        if (unitDataCatalog != null &&
            saveData.units != null)
        {
            if (!RestoreUnits(
                    saveData.units,
                    unitDataCatalog))
            {
                return false;
            }
        }

        gameTimeManager.RestoreTime(
            saveData.gameTime.day,
            saveData.gameTime.time);

        return true;
    }

    public static bool TryReadSaveData(
        out GameSaveData saveData)
    {
        saveData = null;

        if (!HasSave)
        {
            return false;
        }

        try
        {
            string json =
                File.ReadAllText(
                    SavePath);

            if (string.IsNullOrWhiteSpace(
                    json))
            {
                Debug.LogError(
                    "SaveManager: 저장 파일이 비어 있습니다.");

                return false;
            }

            saveData =
                JsonUtility.FromJson<
                    GameSaveData>(
                    json);

            if (saveData == null)
            {
                Debug.LogError(
                    "SaveManager: 저장 파일을 읽지 못했습니다.");

                return false;
            }

            if (saveData.version !=
                CurrentSaveVersion)
            {
                Debug.LogError(
                    $"SaveManager: 지원하지 않는 저장 버전입니다. " +
                    $"파일 {saveData.version}, 현재 {CurrentSaveVersion}");

                saveData = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "SaveManager: 저장 파일을 불러오지 못했습니다.\n" +
                exception);

            saveData = null;
            return false;
        }
    }

    public static bool DeleteSave()
    {
        if (!HasSave)
        {
            return true;
        }

        try
        {
            File.Delete(
                SavePath);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "SaveManager: 저장 파일을 삭제하지 못했습니다.\n" +
                exception);

            return false;
        }
    }

    private static GameSaveData CreateSaveData(
        GameTimeManager gameTimeManager,
        ResourceInventory inventory)
    {
        GameSaveData saveData =
            new()
            {
                version =
                    CurrentSaveVersion,

                savedAtUtc =
                    DateTime.UtcNow
                        .ToString("O"),

                gameTime =
                    new GameTimeSaveData
                    {
                        day =
                            gameTimeManager
                                .CurrentDay,

                        time =
                            gameTimeManager
                                .CurrentTime
                    }
            };

        foreach (ResourceType resourceType
                 in Enum.GetValues(
                     typeof(ResourceType)))
        {
            saveData.resources.Add(
                new ResourceAmountSaveData
                {
                    resourceType =
                        resourceType,

                    amount =
                        inventory
                            .GetResourceAmount(
                                resourceType)
                });
        }

        CaptureFactories(
            saveData);

        CaptureUnits(
            saveData);

        return saveData;
    }

    private static void CaptureFactories(
        GameSaveData saveData)
    {
        FactoryCore[] factories =
            UnityEngine.Object
                .FindObjectsByType<
                    FactoryCore>(
                    FindObjectsSortMode.None);

        foreach (FactoryCore factory
                 in factories)
        {
            if (factory == null ||
                factory.Definition == null ||
                !factory.TryGetComponent(
                    out TileObjectPlacement
                        placement) ||
                !placement.IsPlaced ||
                !factory.TryGetComponent(
                    out FactoryHealth health))
            {
                continue;
            }

            factory.TryGetComponent(
                out FactoryProduction
                    production);

            saveData.factories.Add(
                new FactorySaveData
                {
                    factoryType =
                        factory.Definition
                            .FactoryType,

                    anchorCell =
                        ToSaveCell(
                            placement
                                .AnchorCell),

                    currentHp =
                        health.CurrentHp,

                    isDestroyed =
                        health.IsDestroyed,

                    productionProgressRate =
                        production != null
                            ? production
                                .ProductionProgressRate
                            : 0f,

                    isProductionCycleActive =
                        production != null &&
                        production
                            .IsProductionCycleActive
                });
        }
    }

    private static void CaptureUnits(
        GameSaveData saveData)
    {
        UnitCore[] units =
            UnityEngine.Object
                .FindObjectsByType<
                    UnitCore>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (UnitCore unit
                 in units)
        {
            if (unit == null ||
                unit.Data == null ||
                unit.Data.Team !=
                    UnitTeam.Ally ||
                string.IsNullOrWhiteSpace(
                    unit.Data.SaveId) ||
                !unit.TryGetComponent(
                    out UnitHealth health) ||
                !unit.TryGetComponent(
                    out TileObjectPlacement
                        placement))
            {
                continue;
            }

            unit.TryGetComponent(
                out UnitMovement movement);

            unit.TryGetComponent(
                out UnitStress stress);

            unit.TryGetComponent(
                out UnitCounseling counseling);

            Vector3Int unitCell;

            if (counseling != null &&
                counseling.IsCounseling &&
                counseling.HasReturnCell)
            {
                unitCell =
                    counseling.ReturnCell;
            }
            else if (movement != null &&
                     movement.IsMoving)
            {
                unitCell =
                    movement.DestinationCell;
            }
            else if (placement.IsPlaced)
            {
                unitCell =
                    placement.AnchorCell;
            }
            else
            {
                continue;
            }

            UnitSaveData unitSave =
                new()
                {
                    unitId =
                        unit.Data.SaveId,

                    cell =
                        ToSaveCell(
                            unitCell),

                    isActive =
                        unit.IsActive,

                    currentHp =
                        health.CurrentHp,

                    currentStress =
                        stress != null
                            ? stress.CurrentStress
                            : 0f,

                    attackPower =
                        unit.AttackPower,

                    defense =
                        unit.Defense,

                    attackCooldown =
                        unit.AttackCooldown
                };

            CaptureFactoryAssignment(
                unit,
                unitSave);

            if (counseling != null &&
                counseling.IsCounseling)
            {
                unitSave.isCounseling =
                    true;

                unitSave.hasCounselingReturnCell =
                    counseling.HasReturnCell;

                unitSave.counselingReturnCell =
                    ToSaveCell(
                        counseling.ReturnCell);

                unitSave.counselingRecoveryTimer =
                    counseling.RecoveryTimer;
            }

            saveData.units.Add(
                unitSave);
        }
    }

    private static void CaptureFactoryAssignment(
        UnitCore unit,
        UnitSaveData unitSave)
    {
        if (unit.CurrentTarget == null ||
            !unit.CurrentTarget.TryGetComponent(
                out FactoryCore factory) ||
            !factory.TryGetComponent(
                out TileObjectPlacement
                    factoryPlacement) ||
            !factoryPlacement.IsPlaced)
        {
            return;
        }

        unitSave.hasFactoryAssignment =
            true;

        unitSave.assignedFactoryCell =
            ToSaveCell(
                factoryPlacement
                    .AnchorCell);
    }

    private static bool RestoreFactories(
        IReadOnlyList<FactorySaveData>
            savedFactories,
        PlacementObjectProvider objectProvider)
    {
        FactoryCore[] existingFactories =
            UnityEngine.Object
                .FindObjectsByType<
                    FactoryCore>(
                    FindObjectsSortMode.None);

        foreach (FactoryCore factory
                 in existingFactories)
        {
            if (factory == null)
            {
                continue;
            }

            if (factory.TryGetComponent(
                    out TileObjectPlacement
                        placement) &&
                placement.IsPlaced)
            {
                placement.RemoveFromTiles();
            }

            factory.gameObject
                .SetActive(false);

            UnityEngine.Object.Destroy(
                factory.gameObject);
        }

        foreach (FactorySaveData savedFactory
                 in savedFactories)
        {
            if (savedFactory == null ||
                savedFactory.anchorCell == null)
            {
                continue;
            }

            TileObjectPlacement placement =
                objectProvider.CreateFactory(
                    savedFactory.factoryType);

            if (placement == null)
            {
                return false;
            }

            SceneTilePlacementInitializer
                initializer =
                    placement.GetComponent<
                        SceneTilePlacementInitializer>();

            if (initializer != null)
            {
                initializer.enabled =
                    false;
            }

            Vector3Int anchorCell =
                ToVector3Int(
                    savedFactory.anchorCell);

            if (!placement.TryPlace(
                    anchorCell))
            {
                Debug.LogError(
                    $"SaveManager: " +
                    $"{savedFactory.factoryType} 공장을 " +
                    $"{anchorCell}에 복원하지 못했습니다.",
                    placement);

                UnityEngine.Object.Destroy(
                    placement.gameObject);

                return false;
            }

            if (placement.TryGetComponent(
                    out FactoryHealth health))
            {
                health.RestoreState(
                    savedFactory.currentHp,
                    savedFactory.isDestroyed);
            }

            if (placement.TryGetComponent(
                    out FactoryProduction
                        production))
            {
                production.RestoreState(
                    savedFactory
                        .productionProgressRate,
                    savedFactory
                        .isProductionCycleActive);
            }
        }

        return true;
    }

    private static bool RestoreUnits(
        IReadOnlyList<UnitSaveData>
            savedUnits,
        IReadOnlyList<UnitData>
            unitDataCatalog)
    {
        UnitCore[] existingUnits =
            UnityEngine.Object
                .FindObjectsByType<
                    UnitCore>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (UnitCore unit
                 in existingUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (unit.TryGetComponent(
                    out TileObjectPlacement
                        placement) &&
                placement.IsPlaced)
            {
                placement.RemoveFromTiles();
            }

            unit.gameObject.SetActive(
                false);

            UnityEngine.Object.Destroy(
                unit.gameObject);
        }

        List<(UnitCore unit, UnitSaveData data)>
            restoredUnits =
                new();

        foreach (UnitSaveData savedUnit
                 in savedUnits)
        {
            if (savedUnit == null ||
                string.IsNullOrWhiteSpace(
                    savedUnit.unitId) ||
                savedUnit.cell == null)
            {
                continue;
            }

            UnitData unitData =
                FindUnitData(
                    unitDataCatalog,
                    savedUnit.unitId);

            if (unitData == null ||
                unitData.UnitPrefab == null)
            {
                Debug.LogError(
                    $"SaveManager: UnitData '{savedUnit.unitId}'를 찾을 수 없습니다.");

                return false;
            }

            GameObject unitObject =
                UnityEngine.Object.Instantiate(
                    unitData.UnitPrefab);

            UnitCore unit =
                unitObject.GetComponent<
                    UnitCore>();

            TileObjectPlacement placement =
                unitObject.GetComponent<
                    TileObjectPlacement>();

            if (unit == null ||
                placement == null)
            {
                Debug.LogError(
                    $"{unitObject.name}: " +
                    "유닛 복원에 필요한 컴포넌트가 없습니다.",
                    unitObject);

                UnityEngine.Object.Destroy(
                    unitObject);

                return false;
            }

            SceneTilePlacementInitializer
                initializer =
                    unitObject.GetComponent<
                        SceneTilePlacementInitializer>();

            if (initializer != null)
            {
                initializer.enabled =
                    false;
            }

            unit.SetData(
                unitData);

            unit.SetCombatStats(
                savedUnit.attackPower,
                savedUnit.defense,
                savedUnit.attackCooldown);

            if (unit.TryGetComponent(
                    out UnitHealth health))
            {
                health.RestoreState(
                    savedUnit.currentHp);
            }

            if (unit.TryGetComponent(
                    out UnitStress stress))
            {
                stress.SetStress(
                    savedUnit.currentStress);
            }

            Vector3Int savedCell =
                ToVector3Int(
                    savedUnit.cell);

            if (!savedUnit.isCounseling)
            {
                if (!placement.TryPlace(
                        savedCell))
                {
                    Debug.LogError(
                        $"SaveManager: 유닛 '{savedUnit.unitId}'를 " +
                        $"{savedCell}에 복원하지 못했습니다.",
                        unitObject);

                    UnityEngine.Object.Destroy(
                        unitObject);

                    return false;
                }

                unit.SetUnitActive(
                    savedUnit.isActive);
            }

            unit.SetAutoCombat(
                false);

            unit.SetPlayerMoveCommandActive(
                false);

            unit.ClearTarget();

            if (unit.TryGetComponent(
                    out UnitCounseling
                        counseling) &&
                savedUnit.isCounseling)
            {
                Vector3Int returnCell =
                    savedUnit
                            .counselingReturnCell !=
                        null
                        ? ToVector3Int(
                            savedUnit
                                .counselingReturnCell)
                        : savedCell;

                if (!counseling.RestoreState(
                        true,
                        savedUnit
                            .hasCounselingReturnCell,
                        returnCell,
                        savedUnit
                            .counselingRecoveryTimer))
                {
                    Debug.LogError(
                        $"SaveManager: 유닛 '{savedUnit.unitId}'의 " +
                        "상담 상태를 복원하지 못했습니다.",
                        unitObject);

                    UnityEngine.Object.Destroy(
                        unitObject);

                    return false;
                }
            }

            restoredUnits.Add(
                (unit, savedUnit));
        }

        RestoreFactoryAssignments(
            restoredUnits);

        return true;
    }

    private static void RestoreFactoryAssignments(
        IReadOnlyList<
            (UnitCore unit, UnitSaveData data)>
                restoredUnits)
    {
        foreach (var restored in
                 restoredUnits)
        {
            if (restored.unit == null ||
                restored.data == null ||
                restored.data.isCounseling ||
                !restored.data.hasFactoryAssignment ||
                restored.data.assignedFactoryCell ==
                    null)
            {
                continue;
            }

            Vector3Int factoryCell =
                ToVector3Int(
                    restored.data
                        .assignedFactoryCell);

            FactoryCore factory =
                FindFactoryAt(
                    factoryCell);

            if (factory == null)
            {
                continue;
            }

            restored.unit.SetAutoCombat(
                false);

            restored.unit.SetTarget(
                factory.gameObject);
        }
    }

    private static FactoryCore FindFactoryAt(
        Vector3Int anchorCell)
    {
        FactoryCore[] factories =
            UnityEngine.Object
                .FindObjectsByType<
                    FactoryCore>(
                    FindObjectsSortMode.None);

        foreach (FactoryCore factory
                 in factories)
        {
            if (factory == null ||
                !factory.TryGetComponent(
                    out TileObjectPlacement
                        placement) ||
                !placement.IsPlaced)
            {
                continue;
            }

            if (placement.AnchorCell ==
                anchorCell)
            {
                return factory;
            }
        }

        return null;
    }

    private static UnitData FindUnitData(
        IReadOnlyList<UnitData>
            unitDataCatalog,
        string saveId)
    {
        if (unitDataCatalog == null)
        {
            return null;
        }

        foreach (UnitData unitData
                 in unitDataCatalog)
        {
            if (unitData == null ||
                string.IsNullOrWhiteSpace(
                    unitData.SaveId))
            {
                continue;
            }

            if (string.Equals(
                    unitData.SaveId,
                    saveId,
                    StringComparison.Ordinal))
            {
                return unitData;
            }
        }

        return null;
    }

    private static CellSaveData ToSaveCell(
        Vector3Int cell)
    {
        return new CellSaveData(
            cell.x,
            cell.y,
            cell.z);
    }

    private static Vector3Int ToVector3Int(
        CellSaveData cell)
    {
        return new Vector3Int(
            cell.x,
            cell.y,
            cell.z);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem(
        "Project Jeokru/Save/Open Save Folder")]
    private static void OpenSaveFolder()
    {
        UnityEditor.EditorUtility
            .RevealInFinder(
                Application
                    .persistentDataPath);
    }
#endif
}
