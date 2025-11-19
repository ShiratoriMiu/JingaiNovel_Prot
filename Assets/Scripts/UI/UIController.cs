using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using System.Linq;

[Serializable]
public class AnimationTarget
{
    public string name;
    public Animator animator;
}

public class UIController : MonoBehaviour
{
    public event Action OnDialoguePanelClicked;

    [Header("UI Elements")]
    [SerializeField] private GameObject dialogueBoxContainer;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button dialoguePanelButton;
    [SerializeField] private Image characterImage;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private GameObject choiceButtonsContainer;
    [SerializeField] private Button choiceButtonTemplate;

    [Header("Name Input UI")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button nameInputSubmitButton;
    [SerializeField] private GameObject nameConfirmPopup;
    [SerializeField] private TextMeshProUGUI confirmNameText;
    [SerializeField] private Button confirmAcceptButton;
    [SerializeField] private Button confirmBackButton;
    [SerializeField] private GameObject ngWordWarningPopup;
    [SerializeField] private Button ngWordWarningOkButton;

    [Header("Animation Settings")]
    [SerializeField] private List<AnimationTarget> animationTargets = new List<AnimationTarget>();

    [Header("Timed Choice Elements")]
    [SerializeField] private Slider timerSlider;

    [Header("Typing Effect")]
    [SerializeField] private float charsPerSecond = 10f;
    public bool IsTyping { get; private set; }

    private Coroutine typingCoroutine;
    private Coroutine timerCoroutine;
    private Coroutine blockingAnimationCoroutine;
    private Coroutine duringAnimationCoroutine;
    private string fullDialogueText;
    private Action onTypingCompleted;
    private Action<string> onNameConfirmed;
    private List<string> ngWordList = new List<string>();

    public bool IsDuringAnimationPlaying { get; private set; }

    private void Awake()
    {
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.AddListener(() => OnDialoguePanelClicked?.Invoke());
        }

        // Initialize Name Input UI
        nameInputPanel?.SetActive(false);
        nameConfirmPopup?.SetActive(false);
        ngWordWarningPopup?.SetActive(false);

        nameInputSubmitButton?.onClick.AddListener(OnNameInputSubmit);
        confirmAcceptButton?.onClick.AddListener(OnConfirmAccept);
        confirmBackButton?.onClick.AddListener(OnConfirmBack);
        ngWordWarningOkButton?.onClick.AddListener(() => ngWordWarningPopup?.SetActive(false));

        LoadNGWordList();

        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(false);
        }
        IsDuringAnimationPlaying = false;
    }

    public void ShowDialogue(string characterName, string dialogue, CharacterData characterData, string animationDuringCommands, Action onTypingCompleted)
    {
        this.onTypingCompleted = onTypingCompleted;
        SetDialogueBoxVisible(true);
        nameText.text = characterName;
        fullDialogueText = dialogue;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(dialogue));

        if (duringAnimationCoroutine != null) StopCoroutine(duringAnimationCoroutine);
        if (!string.IsNullOrEmpty(animationDuringCommands))
        {
            duringAnimationCoroutine = StartCoroutine(DuringAnimationCoroutine(animationDuringCommands));
        }
    }

    public void SkipTyping()
    {
        if (IsTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = fullDialogueText;
            IsTyping = false;
            typingCoroutine = null;
            onTypingCompleted?.Invoke();
            onTypingCompleted = null;
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
        onTypingCompleted?.Invoke();
        onTypingCompleted = null;
    }

    #region Animation
    public List<Animator> PlayAnimations(string animationCommands)
    {
        var triggeredAnimators = new List<Animator>();
        if (string.IsNullOrEmpty(animationCommands)) return triggeredAnimators;

        var commands = animationCommands.Split(',');
        foreach (var command in commands)
        {
            if (string.IsNullOrEmpty(command)) continue;

            var parts = command.Split(':');
            if (parts.Length != 2) continue;

            string targetName = parts[0].Trim();
            string trigger = parts[1].Trim();

            AnimationTarget target = animationTargets.Find(t => t.name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (target != null && target.animator != null)
            {
                target.animator.SetTrigger(trigger);
                triggeredAnimators.Add(target.animator);
            }
            else
            {
                Debug.LogWarning($"Animation target '{targetName}' not found or animator is null.");
            }
        }
        return triggeredAnimators;
    }

    public void PlayBlockingAnimation(string animationCommands, Action onComplete)
    {
        if (blockingAnimationCoroutine != null) StopCoroutine(blockingAnimationCoroutine);
        blockingAnimationCoroutine = StartCoroutine(BlockingAnimationCoroutine(animationCommands, onComplete));
    }

    private IEnumerator BlockingAnimationCoroutine(string animationCommands, Action onComplete)
    {
        var triggeredAnimators = PlayAnimations(animationCommands);

        yield return null; // Wait one frame for animator states to update

        float waitTime = GetMaxAnimationLength(triggeredAnimators);

        if (waitTime <= 0 && triggeredAnimators.Any()) waitTime = 1f; // Default wait time if length is 0

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        onComplete?.Invoke();
        blockingAnimationCoroutine = null;
    }

    private IEnumerator DuringAnimationCoroutine(string animationCommands)
    {
        IsDuringAnimationPlaying = true;
        var triggeredAnimators = PlayAnimations(animationCommands);

        yield return null; // Wait one frame for animator states to update

        float waitTime = GetMaxAnimationLength(triggeredAnimators);

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        IsDuringAnimationPlaying = false;
        duringAnimationCoroutine = null;
    }

    private float GetMaxAnimationLength(List<Animator> animators)
    {
        float maxTime = 0f;
        foreach (var animator in animators)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                maxTime = Mathf.Max(maxTime, GetCurrentAnimatorClipLength(animator));
            }
        }
        return maxTime;
    }

    private float GetCurrentAnimatorClipLength(Animator animator)
    {
        if (animator.IsInTransition(0))
        {
            return animator.GetNextAnimatorStateInfo(0).length;
        }
        else
        {
            return animator.GetCurrentAnimatorStateInfo(0).length;
        }
    }

    public void SetDialogueBoxVisible(bool isVisible)
    {
        if (dialogueBoxContainer != null)
        {
            dialogueBoxContainer.SetActive(isVisible);
        }
    }
    #endregion

    #region Timed Choices
    public bool StartTimer(float duration, Action onTimeout)
    {
        if (timerSlider == null)
        {
            CreateTimerSlider();
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(TimerCoroutine(duration, onTimeout));
        return true;
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
            timerSlider.value = timer / duration;
            yield return null;
        }

        timerSlider.gameObject.SetActive(false);
        onTimeout?.Invoke();
        timerCoroutine = null;
    }

    private void CreateTimerSlider()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene. Cannot create Timer Slider.");
            return;
        }

        GameObject sliderObj = new GameObject("TimerSlider");
        sliderObj.transform.SetParent(canvas.transform, false);
        timerSlider = sliderObj.AddComponent<Slider>();

        RectTransform rectTransform = sliderObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1);
        rectTransform.anchorMax = new Vector2(0.5f, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = new Vector2(0, -20);
        rectTransform.sizeDelta = new Vector2(canvas.GetComponent<RectTransform>().sizeDelta.x * 0.8f, 20);

        // Background
        GameObject backgroundObj = new GameObject("Background", typeof(RectTransform));
        backgroundObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        StretchToParentSize(backgroundObj.GetComponent<RectTransform>());

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        StretchToParentSize(fillAreaObj.GetComponent<RectTransform>());

        // Fill
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.yellow;

        timerSlider.fillRect = fillObj.GetComponent<RectTransform>();
        timerSlider.fillRect.anchorMin = new Vector2(0, 0);
        timerSlider.fillRect.anchorMax = new Vector2(1, 1);
        timerSlider.fillRect.pivot = new Vector2(0.5f, 0.5f);

        timerSlider.minValue = 0;
        timerSlider.maxValue = 1;
        timerSlider.value = 1;
        timerSlider.interactable = false;

        timerSlider.gameObject.SetActive(false);
    }

    private void StretchToParentSize(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }
    #endregion

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
        for (int i = choiceButtonsContainer.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = choiceButtonsContainer.transform.GetChild(i).gameObject;
            if (child != choiceButtonTemplate.gameObject) Destroy(child);
        }

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
        foreach (var button in choiceButtonsContainer.GetComponentsInChildren<Button>())
        {
            button.interactable = false;
        }
        choiceButtonsContainer.SetActive(false);
        choiceButtonTemplate.interactable = true;
    }

    #region Name Input

    private void LoadNGWordList()
    {
        TextAsset ngWordAsset = Resources.Load<TextAsset>("Data/NGWordList");
        if (ngWordAsset != null)
        {
            string[] words = ngWordAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var word in words)
            {
                if(!string.IsNullOrWhiteSpace(word))
                {
                    ngWordList.Add(word.Trim());
                }
            }
        }
        else
        {
            Debug.LogWarning("NGWordList.csv not found in Resources/Data.");
        }
    }

    public void ShowNameInput(Action<string> onNameConfirmedCallback)
    {
        this.onNameConfirmed = onNameConfirmedCallback;
        nameInputField.text = "";
        nameInputPanel?.SetActive(true);
    }

    private bool IsNameValid(string name, out string foundNGWord)
    {
        foundNGWord = null;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (name.Length > 10)
        {
            return false;
        }

        foreach (var ngWord in ngWordList)
        {
            if (name.Contains(ngWord))
            {
                foundNGWord = ngWord;
                return false;
            }
        }

        return true;
    }

    private void OnNameInputSubmit()
    {
        string playerName = nameInputField.text;
        string foundNGWord;

        if (!IsNameValid(playerName, out foundNGWord))
        {
            if (foundNGWord != null)
            {
                // NG word was found
                ngWordWarningPopup?.SetActive(true);
                nameInputField.text = ""; // Clear the input as requested
            }
            else
            {
                // Name is empty or too long
                // For now, we just log a warning. A dedicated popup could be implemented here.
                Debug.LogWarning("Player name is invalid (empty or too long).");
            }
            return;
        }

        confirmNameText.text = playerName;
        nameInputPanel?.SetActive(false);
        nameConfirmPopup?.SetActive(true);
    }

    private void OnConfirmAccept()
    {
        string confirmedName = confirmNameText.text;
        nameConfirmPopup?.SetActive(false);
        onNameConfirmed?.Invoke(confirmedName);
        onNameConfirmed = null;
    }

    private void OnConfirmBack()
    {
        nameConfirmPopup?.SetActive(false);
        nameInputPanel?.SetActive(true);
    }

    #endregion
}
