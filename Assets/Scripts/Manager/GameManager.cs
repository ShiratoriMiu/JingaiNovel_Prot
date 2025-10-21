using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Scenario Settings")]
    [SerializeField] private string startingScenarioName = "scenario_01";
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("Dependencies")]
    [SerializeField] private UIController uiController;

    private List<ScenarioData> scenario;
    private int currentLine = 0;
    private bool isChoiceMade = true;
    private bool isScenarioPlaying = false;

    // --- State for Save/Load ---
    private string currentScenarioName;
    private GameData currentGameState = new GameData();
    private SaveLoadUI saveLoadUIInstance;

    void Start()
    {
        // Instantiate managers and UI if they don't exist
        if (SaveLoadManager.Instance == null)
        {
            GameObject managerObj = new GameObject("SaveLoadManager");
            managerObj.AddComponent<SaveLoadManager>();
        }

        InstantiateSaveLoadUI();
        CreateSaveButton();

        // Check if we should load a game or start a new one
        if (SaveLoadManager.Instance.DataToLoad != null)
        {
            ApplyGameData(SaveLoadManager.Instance.DataToLoad);
        }
        else
        {
            LoadScenario(startingScenarioName, 0);
        }

        // Subscribe to the UI click event
        if (uiController != null)
        {
            uiController.OnDialoguePanelClicked += HandleDialogueClick;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (uiController != null)
        {
            uiController.OnDialoguePanelClicked -= HandleDialogueClick;
        }
    }

    private void HandleDialogueClick()
    {
        // Do not process clicks if the Save/Load UI is active or if it's choice time
        if (saveLoadUIInstance != null && saveLoadUIInstance.IsVisible) return;
        if (!isScenarioPlaying || !isChoiceMade) return;

        if (uiController.IsTyping)
        {
            uiController.SkipTyping();
        }
        else
        {
            GoToNextLine();
        }
    }

    void Update()
    {
        // Input handling is now managed by UIController's event
    }

    public void LoadScenario(string scenarioName, int startLine)
    {
        currentScenarioName = scenarioName;
        var scenarioFile = Resources.Load<TextAsset>($"Data/{scenarioName}");
        if (scenarioFile == null) { Debug.LogError($"Scenario file not found: Data/{scenarioName}"); return; }

        scenario = CSVParser.Parse(scenarioFile);
        currentLine = Mathf.Clamp(startLine, 0, scenario.Count - 1);
        isChoiceMade = true;
        isScenarioPlaying = true;
        uiController.HideChoices();
        ShowLine();
    }

    public void GoToNextLine()
    {
        if (currentLine < scenario.Count - 1)
        {
            currentLine++;
            ShowLine();
        }
        else
        {
            isScenarioPlaying = false;
            Debug.Log("End of scenario.");
        }
    }

    private void ShowLine()
    {
        ScenarioData data = scenario[currentLine];

        // Update current game state
        currentGameState.scenarioName = currentScenarioName;
        currentGameState.currentLineIndex = currentLine;
        currentGameState.characterID = data.CharacterID;
        currentGameState.expression = data.Expression;
        if (!string.IsNullOrEmpty(data.BackgroundImage))
        {
            currentGameState.backgroundImageName = data.BackgroundImage;
        }

        switch (data.EventType)
        {
            case "dialogue": HandleDialogue(data); break;
            case "choice": HandleChoice(data); break;
            default: Debug.LogWarning($"Unknown event type: {data.EventType}"); break;
        }
    }

    private void HandleDialogue(ScenarioData data)
    {
        CharacterData character = characterDatabase.GetCharacterData(data.CharacterID);
        string characterName = (character != null) ? character.characterName : data.CharacterID;
        Sprite expressionSprite = (character != null) ? character.expressions.Find(e => e.name == data.Expression)?.sprite : null;

        uiController.ShowDialogue(characterName, data.Dialogue, character);
        uiController.ShowCharacter(expressionSprite);

        if (!string.IsNullOrEmpty(data.BackgroundImage))
        {
            var bgTexture = Resources.Load<Texture>($"Images/Backgrounds/{data.BackgroundImage.Replace(".png", "")}");
            uiController.ChangeBackground(bgTexture);
        }
    }

    // --- Save & Load Specific Methods ---

    public GameData GetCurrentGameData()
    {
        return currentGameState;
    }

    private void ApplyGameData(GameData data)
    {
        currentGameState = data;

        // Restore background
        if (!string.IsNullOrEmpty(data.backgroundImageName))
        {
            var bgTexture = Resources.Load<Texture>($"Images/Backgrounds/{data.backgroundImageName.Replace(".png", "")}");
            uiController.ChangeBackground(bgTexture);
        }

        // Restore character
        CharacterData character = characterDatabase.GetCharacterData(data.characterID);
        Sprite expressionSprite = (character != null) ? character.expressions.Find(e => e.name == data.expression)?.sprite : null;
        uiController.ShowCharacter(expressionSprite);

        // Load the scenario at the correct line
        LoadScenario(data.scenarioName, data.currentLineIndex);
    }

    private void InstantiateSaveLoadUI()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/SaveLoadUI");
        if (prefab == null)
        {
            Debug.LogError("SaveLoadUI prefab not found in Resources/Prefabs folder.");
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("GameManager: No Canvas found in the scene to instantiate the SaveLoadUI.");
            return;
        }

        GameObject uiObj = Instantiate(prefab, canvas.transform);
        saveLoadUIInstance = uiObj.GetComponent<SaveLoadUI>();
    }

    private void CreateSaveButton()
    {
        // Find the main canvas in the scene to add the button to
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene to add the save button.");
            return;
        }

        GameObject buttonObj = new GameObject("SaveButton");
        buttonObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image image = buttonObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(() => saveLoadUIInstance.Show(true));

        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-20, -20);
        rectTransform.sizeDelta = new Vector2(120, 50);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Save";
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
    }

    private void HandleChoice(ScenarioData data)
    {
        isChoiceMade = false;
        uiController.ShowDialogue("System", data.Dialogue, null);
        var choices = scenario.Skip(currentLine + 1).TakeWhile(d => d.EventType == "option").ToList();
        currentLine += choices.Count;
        uiController.ShowChoices(choices, OnChoiceSelected);
    }

    private void OnChoiceSelected(string jumpTarget)
    {
        isChoiceMade = true;
        uiController.HideChoices();
        if (jumpTarget.ToLower() == "quit")
        {
            Debug.Log("Quitting game.");
        }
        else
        {
            LoadScenario(jumpTarget.Replace(".csv", ""), 0);
        }
    }
}