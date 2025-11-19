using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Text.RegularExpressions;

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

    // --- Animation State ---
    private bool isBlockingAnimationPlaying = false;
    private bool isTransitioning = false;
    private bool pendingAfterAnimation = false;
    private bool isWaitingForNameInput = false;


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
        if (!isScenarioPlaying) return;
        if (isTransitioning || isWaitingForNameInput) return;

        if (uiController.IsTyping)
        {
            uiController.SkipTyping();
            return;
        }

        if (!isChoiceMade || isTimerActive || isBlockingAnimationPlaying) return;

        if (pendingAfterAnimation)
        {
            PlayAfterAnimation();
        }
        else if (!uiController.IsDuringAnimationPlaying)
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
        isTransitioning = true;

        if (currentLine < scenario.Count - 1)
        {
            currentLine++;
            ShowLine();
        }
        else
        {
            isScenarioPlaying = false;
            isTransitioning = false;
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

        // Play "during" animations
        uiController.PlayAnimations(data.AnimationDuring);

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
            case "inputName": HandleNameInput(data); return;
            default:
                if (data.CharacterID != "option" && data.CharacterID != "timeout" && !string.IsNullOrEmpty(data.EventType))
                {
                     Debug.LogWarning($"Unknown event type: '{data.EventType}' on line {currentLine}");
                }
                // If the line has no dialogue, it might be purely for an "AnimationAfter" event.
                // Trigger the finish event immediately to process it.
                else if (string.IsNullOrEmpty(data.Dialogue))
                {
                    OnDialogueLineFinished();
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

        Debug.Log($"Replacing '[PLAYER_NAME]' in dialogue. Current Player Name: '{currentGameState.playerName}'. Original Dialogue: '{data.Dialogue}'");
        string processedDialogue = data.Dialogue.Replace("[PLAYER_NAME]", currentGameState.playerName);

        uiController.ShowDialogue(characterName, processedDialogue, character, data.AnimationDuring, OnDialogueLineFinished);

        uiController.ShowCharacter(expressionSprite);

        string bgNameToLoad = !string.IsNullOrEmpty(data.BackgroundImage)
            ? data.BackgroundImage
            : currentGameState.backgroundImageName;

        if (!string.IsNullOrEmpty(bgNameToLoad))
        {
            var bgTexture = Resources.Load<Texture>($"Images/Backgrounds/{bgNameToLoad.Replace(".png", "")}");
            uiController.ChangeBackground(bgTexture);
            currentGameState.backgroundImageName = bgNameToLoad; // Ensure consistency
        }
        isTransitioning = false;
    }

    private void OnDialogueLineFinished()
    {
        // This is called when typing finishes or is skipped.
        // We now check for an "AnimationAfter" event.
        ScenarioData data = scenario[currentLine];

        if (!string.IsNullOrEmpty(data.AnimationAfter))
        {
            pendingAfterAnimation = true;
        }
        // If there's no "after" animation, the game simply waits for the next player click to advance.
    }

    private void PlayAfterAnimation()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        pendingAfterAnimation = false;

        ScenarioData data = scenario[currentLine];
        if (string.IsNullOrEmpty(data.AnimationAfter))
        {
            isTransitioning = false;
            return;
        }

    var commandList = data.AnimationAfter.Split(',').Select(c => Regex.Replace(c, @"\s+", "")).ToList();
    bool autoProceed = commandList.RemoveAll(c => c.Equals("AutoProceed", StringComparison.OrdinalIgnoreCase)) > 0;
    string animationCommandsForUI = string.Join(",", commandList);

        isBlockingAnimationPlaying = true;
    uiController.PlayBlockingAnimation(animationCommandsForUI, () => {
            isBlockingAnimationPlaying = false;
            if (autoProceed)
            {
                GoToNextLine();
            }
            else
            {
                isTransitioning = false;
            }
        });
    }

    private void HandleChoice(ScenarioData data)
    {
        isChoiceMade = false;
        uiController.ShowCharacter(null);
        // Pass null for the Action, as we don't need a callback here.
        uiController.ShowDialogue("System", data.Dialogue, null, null, null);

        var choices = scenario.Skip(currentLine + 1).TakeWhile(d => d.CharacterID == "option").ToList();

        uiController.ShowChoices(choices, OnChoiceSelected);

        // Timed choice logic
        if (float.TryParse(data.EventValue, out float duration))
        {
            isTimerActive = uiController.StartTimer(duration, OnChoiceTimeout);
        }
        isTransitioning = false;
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
        // First, fully restore the game state from the saved data.
        currentGameState = data;
        this.characterAffections = new Dictionary<string, int>(data.characterAffections);

        // Now, load the scenario. ShowLine will be called inside, and it will
        // use the restored currentGameState to render the correct background.
        LoadScenario(data.scenarioName, data.currentLineIndex);
    }
    #endregion

    #region UI Instantiation
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

    #region Name Input
    private void HandleNameInput(ScenarioData data)
    {
        isWaitingForNameInput = true;
        isTransitioning = false; // Allow UI interaction
        uiController.ShowNameInput(OnNameConfirmed);
    }

    private void OnNameConfirmed(string newName)
    {
        currentGameState.playerName = newName;
        isWaitingForNameInput = false;
        GoToNextLine();
    }
    #endregion
}
