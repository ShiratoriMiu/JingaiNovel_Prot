using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System; // For Action

public class UIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image characterImage;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private GameObject choiceButtonsContainer;
    [SerializeField] private Button choiceButtonTemplate;

    public void ShowDialogue(string characterName, string dialogue, CharacterData characterData)
    {
        nameText.text = characterName;
        // Example of using character-specific data. Add a 'nameColor' to CharacterData to use this.
        // nameText.color = (characterData != null) ? characterData.nameColor : Color.white;

        dialogueText.text = dialogue;

        // TODO: Implement text reveal effect (typewriter) in a future step
    }

    public void ShowCharacter(Sprite sprite)
    {
        if (sprite != null)
        {
            characterImage.sprite = sprite;
            characterImage.color = Color.white;
        }
        else
        {
            // Hide character if sprite is null
            characterImage.color = new Color(1, 1, 1, 0);
        }
    }

    public void ChangeBackground(Texture texture)
    {
        if (texture != null)
        {
            backgroundImage.texture = texture;
        }
    }

    public void ShowChoices(List<ScenarioData> choices, Action<string> onChoiceSelected)
    {
        choiceButtonsContainer.SetActive(true);

        // Clear previous buttons, keeping the template intact
        for (int i = choiceButtonsContainer.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = choiceButtonsContainer.transform.GetChild(i).gameObject;
            if (child != choiceButtonTemplate.gameObject)
            {
                Destroy(child);
            }
        }

        // Create a button for each choice
        foreach (var choice in choices)
        {
            Button newButton = Instantiate(choiceButtonTemplate, choiceButtonsContainer.transform);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = choice.Dialogue;
            newButton.gameObject.SetActive(true);

            // Pass the jump target (e.g., "scenario_02.csv") to the callback
            newButton.onClick.AddListener(() => onChoiceSelected(choice.EventValue));
        }

        // Hide the template
        choiceButtonTemplate.gameObject.SetActive(false);
    }

    public void HideChoices()
    {
        choiceButtonsContainer.SetActive(false);
    }
}
