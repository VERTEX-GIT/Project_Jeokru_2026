using System;
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
            null);
    }

    public static bool TryLoad(
        GameTimeManager gameTimeManager,
        PlacementObjectProvider objectProvider)
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

    private static bool RestoreFactories(
        System.Collections.Generic
            .IReadOnlyList<FactorySaveData>
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
