using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System; // For Action
using System.Collections;
using System.Text;

public class UIController : MonoBehaviour
{
    public event Action OnDialoguePanelClicked;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button dialoguePanelButton; // The background panel for the dialogue
    [SerializeField] private Image characterImage;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private GameObject choiceButtonsContainer;
    [SerializeField] private Button choiceButtonTemplate;

    [Header("Typing Effect")]
    [SerializeField] private float charsPerSecond = 10f;
    public bool IsTyping { get; private set; }

    private Coroutine typingCoroutine;
    private string fullDialogueText;

    private void Awake()
    {
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.AddListener(() => OnDialoguePanelClicked?.Invoke());
        }
    }

    public void ShowDialogue(string characterName, string dialogue, CharacterData characterData)
    {
        nameText.text = characterName;
        fullDialogueText = dialogue;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(dialogue));
    }

    public void SkipTyping()
    {
        if (IsTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = fullDialogueText;
            IsTyping = false;
            typingCoroutine = null;
        }
    }

    private IEnumerator TypeText(string text)
    {
        IsTyping = true;
        dialogueText.text = "";
        StringBuilder stringBuilder = new StringBuilder();
        bool isTag = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '<')
            {
                isTag = true;
            }

            stringBuilder.Append(c);

            if (c == '>')
            {
                isTag = false;
            }

            dialogueText.text = stringBuilder.ToString();

            if (!isTag && c != ' ')
            {
                yield return new WaitForSeconds(1f / charsPerSecond);
            }
        }

        IsTyping = false;
        typingCoroutine = null;
    }


    public void ShowCharacter(Sprite sprite)
    {
        if (characterImage == null) return;

        if (sprite != null)
        {
            characterImage.sprite = sprite;
            characterImage.enabled = true; // Imageコンポーネントを有効化
        }
        else
        {
            characterImage.enabled = false; // Imageコンポーネントを無効化
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
