using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;

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

    public bool IsDuringAnimationPlaying { get; private set; }

    private void Awake()
    {
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.AddListener(() => OnDialoguePanelClicked?.Invoke());
        }
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
    public void PlayAnimations(string animationCommands)
    {
        if (string.IsNullOrEmpty(animationCommands)) return;

        var commands = animationCommands.Split(',');
        foreach (var command in commands)
        {
            if (string.IsNullOrEmpty(command) || command.Trim().Equals("HideUI", StringComparison.OrdinalIgnoreCase)) continue;

            var parts = command.Split(':');
            if (parts.Length != 2) continue;

            string targetName = parts[0].Trim();
            string trigger = parts[1].Trim();

            AnimationTarget target = animationTargets.Find(t => t.name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (target != null && target.animator != null)
            {
                target.animator.SetTrigger(trigger);
            }
            else
            {
                Debug.LogWarning($"Animation target '{targetName}' not found or animator is null.");
            }
        }
    }

    public void PlayBlockingAnimation(string animationCommands, Action onComplete)
    {
        if (blockingAnimationCoroutine != null) StopCoroutine(blockingAnimationCoroutine);
        blockingAnimationCoroutine = StartCoroutine(BlockingAnimationCoroutine(animationCommands, onComplete));
    }

    private IEnumerator BlockingAnimationCoroutine(string animationCommands, Action onComplete)
    {
        var commands = animationCommands.Split(',').Select(cmd => cmd.Trim()).ToList();
        bool hideUI = commands.Contains("HideUI");

        if (hideUI) SetDialogueBoxVisible(false);

        PlayAnimations(animationCommands);

        yield return null;

        float waitTime = GetMaxAnimationLength();

        if(waitTime <= 0) waitTime = 1f;

        yield return new WaitForSeconds(waitTime);

        if (hideUI) SetDialogueBoxVisible(true);

        onComplete?.Invoke();
        blockingAnimationCoroutine = null;
    }

    private IEnumerator DuringAnimationCoroutine(string animationCommands)
    {
        IsDuringAnimationPlaying = true;
        PlayAnimations(animationCommands);

        yield return null;

        float waitTime = GetMaxAnimationLength();

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        IsDuringAnimationPlaying = false;
        duringAnimationCoroutine = null;
    }

    private float GetMaxAnimationLength()
    {
        float maxTime = 0f;
        foreach (var target in animationTargets)
        {
            if (target.animator != null && target.animator.runtimeAnimatorController != null)
            {
                maxTime = Mathf.Max(maxTime, GetCurrentAnimatorClipLength(target.animator));
            }
        }
        return maxTime;
    }

    private float GetCurrentAnimatorClipLength(Animator animator)
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length;
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
            timerSlider.value = timer / duration;
            yield return null;
        }

        timerSlider.gameObject.SetActive(false);
        onTimeout?.Invoke();
        timerCoroutine = null;
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
}
