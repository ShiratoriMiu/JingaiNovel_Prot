using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class SaveLoadUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button closeButton;
    [SerializeField] private List<Button> slotButtons;
    [SerializeField] private List<TextMeshProUGUI> slotTexts;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI notificationText;

    public bool IsVisible => canvasGroup.alpha > 0;
    private CanvasGroup canvasGroup;
    private bool isSaveMode;
    private GameManager gameManager; // Only needed for saving

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Hide);
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int slotIndex = i; // Important for lambda capture
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
        }
        Hide(); // Start hidden
    }

    public void Show(bool saveMode)
    {
        this.isSaveMode = saveMode;

        if (isSaveMode)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("SaveLoadUI: GameManager not found in the scene. Cannot perform save.");
                Hide();
                return;
            }
            titleText.text = "データを保存";
        }
        else
        {
            titleText.text = "データを読み込む";
        }

        notificationText.text = "";
        RefreshUI();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void RefreshUI()
    {
        List<GameData> allData = SaveLoadManager.Instance.GetAllSaveDataInfo();
        for (int i = 0; i < slotButtons.Count; i++)
        {
            if (i < allData.Count && allData[i] != null)
            {
                slotTexts[i].text = $"主人公: {allData[i].playerName}\n{allData[i].saveTimestamp}";
                if (!isSaveMode)
                {
                    slotButtons[i].interactable = true;
                }
            }
            else
            {
                slotTexts[i].text = "データがありません";
                if (!isSaveMode)
                {
                    slotButtons[i].interactable = false;
                }
            }
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (isSaveMode)
        {
            if (gameManager == null)
            {
                Debug.LogError("GameManager reference is missing. Aborting save.");
                return;
            }

            GameData dataToSave = gameManager.GetCurrentGameData();
            dataToSave.saveTimestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            SaveLoadManager.Instance.SaveGame(slotIndex, dataToSave);

            StartCoroutine(ShowNotification("セーブしました"));
            RefreshUI();
        }
        else
        {
            SaveLoadManager.Instance.LoadGameAndSwitchScene(slotIndex);
        }
    }

    private System.Collections.IEnumerator ShowNotification(string message)
    {
        notificationText.text = message;
        yield return new WaitForSeconds(2.0f);
        notificationText.text = "";
    }
}
