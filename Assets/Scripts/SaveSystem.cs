using System;
using System.IO;
using UnityEngine;

public interface ISaveable
{
    void OnSave(SaveData data);
    void OnLoad(SaveData data);
}

[System.Serializable]
public class SaveData
{
    /*
    public string playerName;
    public int days;

    To use, do this:

    public class scriptname : MonoBehaviour, ISaveable
    ...
    public void OnSave(SaveData data)
    {
        data.scriptPlayername = playerName;
        data.scriptDays = days;
    }

    public void OnLoad(SaveData data)
    {
        playerName = data.scriptPlayername;
        days = data.scriptDays;
    }
    ... and so on for each variable you want to save. Important: Make sure to add the variable to the SaveData class as well.
    */

    public string playerName;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private SaveData currentSaveData;
    public SaveData GetSaveData() => currentSaveData == null ? null : JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(currentSaveData));
    public event Action<SaveData> OnSave; 
    public event Action<SaveData> OnLoad;
    private string savePath;
    private const string SAVE_FILENAME = "BorderlineHumanSaveData.json";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        savePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
    }

    public void SaveGame(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"Game saved to {savePath}");
            OnSave?.Invoke(GetSaveData());
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }

    public SaveData LoadGame()
    {
        try
        {
            if (!File.Exists(savePath))
                return null;

            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game loaded successfully");
            OnLoad?.Invoke(GetSaveData());
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}");
            return null;
        }
    }
}