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
    private InGameMenuUI inGameMenuUIInstance;

    // --- Affection System State ---
    private Dictionary<string, int> characterAffections = new Dictionary<string, int>();


    void Start()
    {
        if (SaveLoadManager.Instance == null)
        {
            GameObject managerObj = new GameObject("SaveLoadManager");
            managerObj.AddComponent<SaveLoadManager>();
        }

        InstantiateSaveLoadUI();
        InstantiateInGameMenuUI();
        CreateSaveButton();
        CreateMenuButton();

        if (SaveLoadManager.Instance.DataToLoad != null)
        {
            ApplyGameData(SaveLoadManager.Instance.DataToLoad);
        }
        else
        {
            characterAffections.Clear(); // New game starts with 0 affection
            LoadScenario(startingScenarioName, 0);
        }

        if (uiController != null)
        {
            uiController.OnDialoguePanelClicked += HandleDialogueClick;
        }
    }

    private void OnDestroy()
    {
        if (uiController != null)
        {
            uiController.OnDialoguePanelClicked -= HandleDialogueClick;
        }
    }

    private void HandleDialogueClick()
    {
        if (saveLoadUIInstance != null && saveLoadUIInstance.IsVisible) return;
        if (inGameMenuUIInstance != null && inGameMenuUIInstance.IsVisible) return;
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
        // Stop processing if we're past the end of the scenario
        if (currentLine >= scenario.Count)
        {
            isScenarioPlaying = false;
            Debug.Log("End of scenario reached.");
            return;
        }

        ScenarioData data = scenario[currentLine];

        // Check for branch conditions before processing the line
        if (!CheckBranchCondition(data.BranchCondition))
        {
            GoToNextLine(); // Skip this line and move to the next
            return;
        }

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
            case "dialogue":
                HandleDialogue(data);
                break;
            case "choice":
                HandleChoice(data);
                return; // Stop further processing after initiating a choice
            case "jump":
                HandleJump(data);
                return; // Stop further processing after a jump
            // Note: "option" is handled by HandleChoice, not here.
            default:
                if (data.CharacterID != "option" && !string.IsNullOrEmpty(data.EventType))
                {
                     Debug.LogWarning($"Unknown event type: '{data.EventType}' on line {currentLine}");
                }
                break;
        }
    }

    private void HandleDialogue(ScenarioData data)
    {
        CharacterData character = characterDatabase.GetCharacterData(data.CharacterID);
        string characterName = (character != null) ? character.characterName : data.CharacterID;

        Sprite expressionSprite = null;

        if (data.Expression != "none" && !string.IsNullOrEmpty(data.Expression))
        {
            if (character != null)
            {
                expressionSprite = character.expressions.Find(e => e.name == data.Expression)?.sprite;
            }
        }

        uiController.ShowDialogue(characterName, data.Dialogue, character);
        uiController.ShowCharacter(expressionSprite);

        if (!string.IsNullOrEmpty(data.BackgroundImage))
        {
            var bgTexture = Resources.Load<Texture>($"Images/Backgrounds/{data.BackgroundImage.Replace(".png", "")}");
            uiController.ChangeBackground(bgTexture);
        }
    }

    private void HandleChoice(ScenarioData data)
    {
        isChoiceMade = false;
        uiController.ShowCharacter(null);
        uiController.ShowDialogue("System", data.Dialogue, null);
        var choices = scenario.Skip(currentLine + 1).TakeWhile(d => d.CharacterID == "option").ToList();
        currentLine += choices.Count;
        uiController.ShowChoices(choices, OnChoiceSelected);
    }

    private void HandleJump(ScenarioData data)
    {
        Debug.Log($"Jumping to scenario: {data.EventValue}");
        LoadScenario(data.EventValue.Replace(".csv", ""), 0);
    }

    private void OnChoiceSelected(ScenarioData choiceData)
    {
        isChoiceMade = true;
        uiController.HideChoices();

        // Apply affection changes from the chosen option
        ApplyAffectionChange(choiceData.AffectionChange);

        // Jump to the next scenario
        string jumpTarget = choiceData.EventValue;
        if (jumpTarget.ToLower() == "quit")
        {
            Debug.Log("Quitting game.");
            // Application.Quit(); // Uncomment for build
        }
        else
        {
            LoadScenario(jumpTarget.Replace(".csv", ""), 0);
        }
    }

    // --- Affection System Methods ---

    private void ApplyAffectionChange(string affectionString)
    {
        if (string.IsNullOrEmpty(affectionString))
        {
            return;
        }

        // Multiple changes can be comma-separated, e.g., "charA:+10,charB:-5"
        var changes = affectionString.Split(',');
        foreach (var change in changes)
        {
            var parts = change.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogWarning($"Invalid affection change format: '{change}'");
                continue;
            }

            string characterID = parts[0].Trim(); // Trim whitespace
            if (!int.TryParse(parts[1].Trim(), out int valueChange)) // Trim whitespace
            {
                Debug.LogWarning($"Invalid value in affection change: '{change}'");
                continue;
            }

            int currentAffection = GetAffection(characterID);
            characterAffections[characterID] = currentAffection + valueChange;
            Debug.Log($"Affection for {characterID} changed to {characterAffections[characterID]}");
        }
    }

    private int GetAffection(string characterID)
    {
        characterAffections.TryGetValue(characterID, out int value);
        return value;
    }

    private bool CheckBranchCondition(string conditionString)
    {
        if (string.IsNullOrEmpty(conditionString))
        {
            return true; // No condition always passes
        }

        var parts = conditionString.Split(':');
        if (parts.Length != 3)
        {
            Debug.LogWarning($"Invalid branch condition format: '{conditionString}'");
            return true; // Treat invalid formats as true to prevent game from stopping
        }

        string characterID = parts[0].Trim(); // Trim whitespace
        string op = parts[1].Trim(); // Trim whitespace
        if (!int.TryParse(parts[2].Trim(), out int requiredValue)) // Trim whitespace
        {
            Debug.LogWarning($"Invalid value in branch condition: '{conditionString}'");
            return true;
        }

        int currentAffection = GetAffection(characterID);

        switch (op)
        {
            case "==": return currentAffection == requiredValue;
            case "!=": return currentAffection != requiredValue;
            case ">": return currentAffection > requiredValue;
            case ">=": return currentAffection >= requiredValue;
            case "<": return currentAffection < requiredValue;
            case "<=": return currentAffection <= requiredValue;
            default:
                Debug.LogWarning($"Unsupported operator in branch condition: '{op}'");
                return true;
        }
    }

    // --- Save & Load Specific Methods ---

    public GameData GetCurrentGameData()
    {
        // Make sure to copy the dictionary so the save data is a snapshot
        currentGameState.characterAffections = new Dictionary<string, int>(this.characterAffections);
        return currentGameState;
    }

    private void ApplyGameData(GameData data)
    {
        currentGameState = data;
        // Restore the affection dictionary
        this.characterAffections = new Dictionary<string, int>(data.characterAffections);

        if (!string.IsNullOrEmpty(data.backgroundImageName))
        {
            var bgTexture = Resources.Load<Texture>($"Images/Backgrounds/{data.backgroundImageName.Replace(".png", "")}");
            uiController.ChangeBackground(bgTexture);
        }

        CharacterData character = characterDatabase.GetCharacterData(data.characterID);
        Sprite expressionSprite = (character != null) ? character.expressions.Find(e => e.name == data.expression)?.sprite : null;
        uiController.ShowCharacter(expressionSprite);

        LoadScenario(data.scenarioName, data.currentLineIndex);
    }

    #region UI Instantiation (Boilerplate)
    private void InstantiateSaveLoadUI()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/SaveLoadUI");
        if (prefab == null) { Debug.LogError("SaveLoadUI prefab not found."); return; }
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("No Canvas found in scene."); return; }
        GameObject uiObj = Instantiate(prefab, canvas.transform);
        saveLoadUIInstance = uiObj.GetComponent<SaveLoadUI>();
    }

    private void CreateSaveButton()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject buttonObj = new GameObject("SaveButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        buttonObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
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

    private void InstantiateInGameMenuUI()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/InGameMenuUI");
        if (prefab == null) { Debug.LogError("InGameMenuUI prefab not found."); return; }
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject uiObj = Instantiate(prefab, canvas.transform);
        inGameMenuUIInstance = uiObj.GetComponent<InGameMenuUI>();
    }

    private void CreateMenuButton()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject buttonObj = new GameObject("MenuButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        buttonObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(() => inGameMenuUIInstance.Show());
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(20, -20);
        rectTransform.sizeDelta = new Vector2(120, 50);
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Menu";
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
    }
    #endregion
}
