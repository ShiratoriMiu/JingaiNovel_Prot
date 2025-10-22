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

    // --- Timed Choice State ---
    private bool isTimerActive = false;


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
            characterAffections.Clear();
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
        if (isTimerActive) return; // Don't advance dialogue if timer is running

        if (uiController.IsTyping)
        {
            uiController.SkipTyping();
        }
        else
        {
            GoToNextLine();
        }
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
        if (currentLine >= scenario.Count)
        {
            isScenarioPlaying = false;
            Debug.Log("End of scenario reached.");
            return;
        }

        ScenarioData data = scenario[currentLine];

        if (!CheckBranchCondition(data.BranchCondition))
        {
            GoToNextLine();
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
            case "dialogue": HandleDialogue(data); break;
            case "choice": HandleChoice(data); return;
            case "jump": HandleJump(data); return;
            default:
                if (data.CharacterID != "option" && data.CharacterID != "timeout" && !string.IsNullOrEmpty(data.EventType))
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
        if (data.Expression != "none" && !string.IsNullOrEmpty(data.Expression) && character != null)
        {
            expressionSprite = character.expressions.Find(e => e.name == data.Expression)?.sprite;
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

        uiController.ShowChoices(choices, OnChoiceSelected);

        // Timed choice logic
        if (float.TryParse(data.EventValue, out float duration))
        {
            isTimerActive = true;
            uiController.StartTimer(duration, OnChoiceTimeout);
        }
    }

    private void OnChoiceTimeout()
    {
        if (!isTimerActive) return; // Already handled
        isTimerActive = false;
        isChoiceMade = true; // Allow progression
        uiController.HideChoices();

        // Find the timeout jump target
        var timeoutData = scenario.Skip(currentLine + 1).FirstOrDefault(d => d.CharacterID == "timeout");
        if (timeoutData != null)
        {
            Debug.Log("Choice timed out. Jumping to timeout scenario.");
            LoadScenario(timeoutData.EventValue.Replace(".csv",""), 0);
        }
        else
        {
            Debug.LogWarning("Choice timed out, but no 'timeout' event was found. Proceeding to next line.");
            GoToNextLine();
        }
    }


    private void HandleJump(ScenarioData data)
    {
        Debug.Log($"Jumping to scenario: {data.EventValue}");
        LoadScenario(data.EventValue.Replace(".csv", ""), 0);
    }

    private void OnChoiceSelected(ScenarioData choiceData)
    {
        if(isTimerActive)
        {
            uiController.StopTimer();
            isTimerActive = false;
        }

        isChoiceMade = true;
        uiController.HideChoices();
        ApplyAffectionChange(choiceData.AffectionChange);

        string jumpTarget = choiceData.EventValue;
        if (!string.IsNullOrEmpty(jumpTarget))
        {
            if (jumpTarget.ToLower() == "quit")
            {
                Debug.Log("Quitting game.");
            }
            else
            {
                LoadScenario(jumpTarget.Replace(".csv", ""), 0);
            }
        }
        else
        {
            GoToNextLine();
        }
    }

    #region Affection System
    private void ApplyAffectionChange(string affectionString)
    {
        if (string.IsNullOrEmpty(affectionString)) return;
        var changes = affectionString.Split(',');
        foreach (var change in changes)
        {
            var parts = change.Split(':');
            if (parts.Length != 2) continue;
            string characterID = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out int valueChange)) continue;
            int currentAffection = GetAffection(characterID);
            characterAffections[characterID] = currentAffection + valueChange;
        }
    }

    private int GetAffection(string characterID)
    {
        characterAffections.TryGetValue(characterID, out int value);
        return value;
    }

    private bool CheckBranchCondition(string conditionString)
    {
        if (string.IsNullOrEmpty(conditionString)) return true;
        var parts = conditionString.Split(':');
        if (parts.Length != 3) return true;
        string characterID = parts[0].Trim();
        string op = parts[1].Trim();
        if (!int.TryParse(parts[2].Trim(), out int requiredValue)) return true;
        int currentAffection = GetAffection(characterID);
        switch (op)
        {
            case "==": return currentAffection == requiredValue;
            case "!=": return currentAffection != requiredValue;
            case ">": return currentAffection > requiredValue;
            case ">=": return currentAffection >= requiredValue;
            case "<": return currentAffection < requiredValue;
            case "<=": return currentAffection <= requiredValue;
            default: return true;
        }
    }
    #endregion

    #region Save/Load
    public GameData GetCurrentGameData()
    {
        currentGameState.characterAffections = new Dictionary<string, int>(this.characterAffections);
        return currentGameState;
    }

    private void ApplyGameData(GameData data)
    {
        currentGameState = data;
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
    #endregion

    #region UI Instantiation
    private void InstantiateSaveLoadUI(){/*...boilerplate...*/}
    private void CreateSaveButton(){/*...boilerplate...*/}
    private void InstantiateInGameMenuUI(){/*...boilerplate...*/}
    private void CreateMenuButton(){/*...boilerplate...*/}
    #endregion
}
