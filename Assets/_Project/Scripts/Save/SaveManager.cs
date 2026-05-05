using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SaveManager : MonoBehaviour
{
    [SerializeField] private string saveFileName = "save.json";
    [SerializeField] private float autoSaveInterval = 10f;
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnApplicationQuit = true;

    private readonly List<ISaveable> saveables = new();
    private ISaveStorage storage;
    private float autoSaveTimer;
    private bool isConstructed;
    private bool hasLoaded;

    public void Construct(ISaveStorage saveStorage, IEnumerable<ISaveable> saveTargets)
    {
        storage = saveStorage;
        saveables.Clear();

        if (saveTargets != null)
        {
            foreach (var saveable in saveTargets)
            {
                if (saveable != null && !saveables.Contains(saveable))
                {
                    saveables.Add(saveable);
                }
            }
        }

        isConstructed = true;

        if (loadOnStart && !hasLoaded)
        {
            Load();
        }
    }

    private void Awake()
    {
        DIContainer.Global.Register(this);
    }

    private void Start()
    {
        EnsureConstructed();

        if (loadOnStart && !hasLoaded)
        {
            Load();
        }
    }

    private void OnEnable()
    {
        SaveEvents.SaveRequested += Save;
    }

    private void OnDisable()
    {
        SaveEvents.SaveRequested -= Save;
    }

    private void Update()
    {
        if (autoSaveInterval <= 0f)
        {
            return;
        }

        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            Save();
        }
    }

    public void Save()
    {
        EnsureConstructed();

        if (storage == null)
        {
            return;
        }

        var saveData = new SaveData
        {
            lastSaveUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        for (int i = 0; i < saveables.Count; i++)
        {
            saveables[i]?.CaptureSaveData(saveData);
        }

        storage.Save(saveData);
    }

    public void Load()
    {
        EnsureConstructed();

        if (storage == null)
        {
            return;
        }

        SaveData saveData = storage.Load();
        for (int i = 0; i < saveables.Count; i++)
        {
            saveables[i]?.RestoreSaveData(saveData);
        }

        hasLoaded = true;
    }

    [ContextMenu("Save Now")]
    private void SaveNowFromInspector()
    {
        Save();
    }

    [ContextMenu("Delete Save File")]
    private void DeleteSaveFileFromInspector()
    {
        EnsureConstructed();
        storage?.Delete();
        hasLoaded = false;
        Debug.Log("[SaveManager] Save file deleted.");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && saveOnApplicationPause)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit)
        {
            Save();
        }
    }

    private void EnsureConstructed()
    {
        if (isConstructed)
        {
            return;
        }

        Construct(new JsonFileSaveStorage(saveFileName), FindSaveablesInScene());
    }

    private static IEnumerable<ISaveable> FindSaveablesInScene()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISaveable saveable)
            {
                yield return saveable;
            }
        }
    }
}
