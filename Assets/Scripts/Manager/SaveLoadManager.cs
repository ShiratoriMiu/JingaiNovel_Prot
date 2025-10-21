using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int saveSlotCount = 5;

    // This will hold the data that is loaded from a file, ready for the GameManager to use.
    public GameData DataToLoad { get; private set; }
    private string saveFileNamePrefix = "save_";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"{saveFileNamePrefix}{slotIndex}.json");
    }

    public void SaveGame(int slotIndex, GameData data)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        string filePath = GetSaveFilePath(slotIndex);
        File.WriteAllText(filePath, json);
        Debug.Log($"Game saved to slot {slotIndex} at {filePath}");
    }

    public GameData LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}");
            return null;
        }

        string filePath = GetSaveFilePath(slotIndex);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            GameData data = JsonUtility.FromJson<GameData>(json);
            Debug.Log($"Game loaded from slot {slotIndex}");
            return data;
        }

        Debug.LogWarning($"No save file found for slot {slotIndex}");
        return null;
    }

    // Call this when a "Load" button is clicked. It stores the data and loads the game scene.
    public void LoadGameAndSwitchScene(int slotIndex)
    {
        DataToLoad = LoadGame(slotIndex);
        if (DataToLoad != null)
        {
            // The GameManager will be responsible for checking this DataToLoad upon scene start.
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }

    // A helper to get all save data info for the Load/Save UI
    public List<GameData> GetAllSaveDataInfo()
    {
        List<GameData> allData = new List<GameData>();
        for (int i = 0; i < saveSlotCount; i++)
        {
            allData.Add(LoadGame(i)); // This will add null if the slot is empty
        }
        return allData;
    }
}
