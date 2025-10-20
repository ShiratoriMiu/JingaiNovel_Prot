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

    void Start()
    {
        LoadScenario(startingScenarioName);
    }

    void Update()
    {
        if (!isChoiceMade) return;
        if (Input.GetMouseButtonDown(0))
        {
            GoToNextLine();
        }
    }

    public void LoadScenario(string scenarioName)
    {
        var scenarioFile = Resources.Load<TextAsset>($"Data/{scenarioName}");
        if (scenarioFile == null) { Debug.LogError($"Scenario file not found: Data/{scenarioName}"); return; }
        scenario = CSVParser.Parse(scenarioFile);
        currentLine = 0;
        isChoiceMade = true;
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
            Debug.Log("End of scenario.");
        }
    }

    private void ShowLine()
    {
        ScenarioData data = scenario[currentLine];
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
            LoadScenario(jumpTarget.Replace(".csv", ""));
        }
    }
}