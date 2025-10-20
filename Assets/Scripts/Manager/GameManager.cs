using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For Linq queries like Where

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
        // If a choice is displayed, don't advance dialogue on click.
        if (!isChoiceMade) return;

        // On mouse click, go to the next line.
        if (Input.GetMouseButtonDown(0))
        {
            GoToNextLine();
        }
    }

    public void LoadScenario(string scenarioName)
    {
        var scenarioFile = Resources.Load<TextAsset>($"Data/{scenarioName}");
        if (scenarioFile == null)
        {
            Debug.LogError($"Scenario file not found: Data/{scenarioName}");
            return;
        }
        scenario = CSVParser.Parse(scenarioFile);
        currentLine = 0;
        isChoiceMade = true; // Reset choice state
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
            // Optionally, return to title screen or show an end message.
        }
    }

    private void ShowLine()
    {
        ScenarioData data = scenario[currentLine];

        switch (data.EventType)
        {
            case "dialogue":
                HandleDialogue(data);
                break;
            case "choice":
                HandleChoice(data);
                break;
            // Add other event types like 'playSound', 'changeScene' etc. here
            default:
                Debug.LogWarning($"Unknown event type: {data.EventType}");
                break;
        }
    }

    private void HandleDialogue(ScenarioData data)
    {
        CharacterData character = characterDatabase.GetCharacterData(data.CharacterID);
        string characterName = (character != null) ? character.characterName : data.CharacterID;
        Sprite expressionSprite = (character != null) ?
            character.expressions.Find(e => e.name == data.Expression)?.sprite : null;

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

        // Show the choice prompt
        uiController.ShowDialogue("System", data.Dialogue, null);

        // Find subsequent "option" lines
        var choices = new List<ScenarioData>();
        for (int i = currentLine + 1; i < scenario.Count; i++)
        {
            if (scenario[i].EventType == "option")
            {
                choices.Add(scenario[i]);
            }
            else
            {
                // Stop when the event type is no longer "option"
                break;
            }
        }

        currentLine += choices.Count; // Skip the option lines in dialogue progression
        uiController.ShowChoices(choices, OnChoiceSelected);
    }

    private void OnChoiceSelected(string jumpTarget)
    {
        isChoiceMade = true;
        uiController.HideChoices();

        if (jumpTarget.ToLower() == "quit")
        {
            Debug.Log("Quitting game.");
            // Application.Quit(); // This would work in a real build
        }
        else
        {
            LoadScenario(jumpTarget.Replace(".csv", ""));
        }
    }
}
