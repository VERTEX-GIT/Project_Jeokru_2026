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

        return saveData;
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
