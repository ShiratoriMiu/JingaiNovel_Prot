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
    [SerializeField] private Button dialoguePanelButton;
    [SerializeField] private Image characterImage;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private GameObject choiceButtonsContainer;
    [SerializeField] private Button choiceButtonTemplate;

    [Header("Timed Choice Elements")]
    [SerializeField] private Slider timerSlider; // タイマーバー用のSlider

    [Header("Typing Effect")]
    [SerializeField] private float charsPerSecond = 10f;
    public bool IsTyping { get; private set; }

    private Coroutine typingCoroutine;
    private Coroutine timerCoroutine;
    private string fullDialogueText;

    private void Awake()
    {
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.AddListener(() => OnDialoguePanelClicked?.Invoke());
        }
        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(false); // 最初は非表示
        }
    }

    public void StartTimer(float duration, Action onTimeout)
    {
        if (timerSlider == null)
        {
            Debug.LogWarning("Timer Slider is not assigned in the UIController.");
            onTimeout?.Invoke();
            return;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(TimerCoroutine(duration, onTimeout));
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(false);
        }
    }

    private IEnumerator TimerCoroutine(float duration, Action onTimeout)
    {
        timerSlider.gameObject.SetActive(true);
        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            timerSlider.value = timer / duration; // 0から1の範囲で値を設定
            yield return null;
        }

        timerSlider.gameObject.SetActive(false);
        onTimeout?.Invoke();
        timerCoroutine = null;
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
            if (c == '<') isTag = true;
            stringBuilder.Append(c);
            if (c == '>') isTag = false;
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
            characterImage.enabled = true;
        }
        else
        {
            characterImage.enabled = false;
        }
    }

    public void ChangeBackground(Texture texture)
    {
        if (texture != null)
        {
            backgroundImage.texture = texture;
        }
    }

    public void ShowChoices(List<ScenarioData> choices, Action<ScenarioData> onChoiceSelected)
    {
        choiceButtonsContainer.SetActive(true);
        // Clear previous buttons
        for (int i = choiceButtonsContainer.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = choiceButtonsContainer.transform.GetChild(i).gameObject;
            if (child != choiceButtonTemplate.gameObject) Destroy(child);
        }

        // Create a button for each choice
        foreach (var choice in choices)
        {
            Button newButton = Instantiate(choiceButtonTemplate, choiceButtonsContainer.transform);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = choice.Dialogue;
            newButton.gameObject.SetActive(true);
            newButton.onClick.AddListener(() => onChoiceSelected(choice));
        }
        choiceButtonTemplate.gameObject.SetActive(false);
    }

    public void HideChoices()
    {
        // Disable all buttons to prevent clicking after a choice is made or timed out
        foreach (var button in choiceButtonsContainer.GetComponentsInChildren<Button>())
        {
            button.interactable = false;
        }
        choiceButtonsContainer.SetActive(false);

        // Re-enable template for next time
        choiceButtonTemplate.interactable = true;
    }
}
