using System;
using System.IO;
using UnityEngine;

public sealed class JsonFileSaveStorage : ISaveStorage
{
    private readonly string savePath;

    public JsonFileSaveStorage(string saveFileName)
    {
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
    }

    public bool Exists()
    {
        return File.Exists(savePath);
    }

    public SaveData Load()
    {
        if (!Exists())
        {
            return new SaveData();
        }

        try
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveStorage] Failed to load save file. Starting fresh. {ex.Message}");
            return new SaveData();
        }
    }

    public void Save(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(savePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveStorage] Failed to save file. {ex.Message}");
        }
    }

    public void Delete()
    {
        try
        {
            if (Exists())
            {
                File.Delete(savePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveStorage] Failed to delete save file. {ex.Message}");
        }
    }
}
