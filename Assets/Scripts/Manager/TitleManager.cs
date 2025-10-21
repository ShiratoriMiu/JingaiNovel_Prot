using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    private SaveLoadUI saveLoadUIInstance;

    private void Start()
    {
        // Ensure the SaveLoadManager exists
        if (SaveLoadManager.Instance == null)
        {
            GameObject managerObj = new GameObject("SaveLoadManager");
            managerObj.AddComponent<SaveLoadManager>();
        }

        // Instantiate the Save/Load UI from Resources
        InstantiateSaveLoadUI();
    }

    // This method will be called by the "Start Game" button
    public void StartGame()
    {
        // Ensure no data is loaded when starting a new game
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.ClearDataToLoad();
        }
        SceneManager.LoadScene("GameScene");
    }

    // This method will be called by the "Continue" button
    public void ShowLoadScreen()
    {
        if (saveLoadUIInstance != null)
        {
            saveLoadUIInstance.Show(false); // false for Load mode
        }
        else
        {
            Debug.LogError("SaveLoadUI instance is not found.");
        }
    }

    private void InstantiateSaveLoadUI()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/SaveLoadUI");
        if (prefab != null)
        {
            GameObject uiObj = Instantiate(prefab);
            saveLoadUIInstance = uiObj.GetComponent<SaveLoadUI>();
        }
        else
        {
            Debug.LogError("SaveLoadUI prefab not found in Resources/Prefabs folder.");
        }
    }
}
