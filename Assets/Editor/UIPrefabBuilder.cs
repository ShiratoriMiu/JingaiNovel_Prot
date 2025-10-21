using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class UIPrefabBuilder
{
    [MenuItem("Tools/Create Save/Load UI Prefab")]
    public static void CreateSaveLoadUIPrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs/SaveLoadUI.prefab";

        // Create parent object for the prefab
        GameObject root = new GameObject("SaveLoadUI");
        root.AddComponent<RectTransform>();

        // Add components to root
        SaveLoadUI saveLoadUI = root.AddComponent<SaveLoadUI>();
        root.AddComponent<CanvasGroup>();

        // --- Background Panel (for visuals and blocking raycasts) ---
        GameObject mainPanel = CreatePanel(root, "MainPanel");
        RectTransform panelRect = mainPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800, 600);
        mainPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // --- Title Text ---
        TextMeshProUGUI titleText = CreateText(mainPanel, "TitleText", "セーブ／ロード", 36);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -20);

        // --- Close Button ---
        Button closeButton = CreateButton(mainPanel, "CloseButton", "X");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-10, -10);
        closeRect.sizeDelta = new Vector2(40, 40);

        // --- Notification Text ---
        TextMeshProUGUI notificationText = CreateText(mainPanel, "NotificationText", "", 24);
        RectTransform notificationRect = notificationText.GetComponent<RectTransform>();
        notificationRect.anchorMin = new Vector2(0.5f, 0f);
        notificationRect.anchorMax = new Vector2(0.5f, 0f);
        notificationRect.pivot = new Vector2(0.5f, 0f);
        notificationRect.anchoredPosition = new Vector2(0, 15);

        // --- Slot Buttons ---
        List<Button> slotButtons = new List<Button>();
        List<TextMeshProUGUI> slotTexts = new List<TextMeshProUGUI>();

        GameObject grid = new GameObject("SlotGrid");
        grid.transform.SetParent(mainPanel.transform, false);
        RectTransform gridRect = grid.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(700, 450);
        gridRect.anchoredPosition = new Vector2(0, -30);

        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(680, 80);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        int saveSlotCount = 5; // As per spec, default to 5
        for (int i = 0; i < saveSlotCount; i++)
        {
            Button slotButton = CreateButton(grid, $"SlotButton_{i}", "");
            slotButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TextMeshProUGUI slotText = CreateText(slotButton.gameObject, $"SlotText_{i}", $"スロット {i + 1}\n(空きデータ)", 22);
            slotText.alignment = TextAlignmentOptions.Center;

            slotButtons.Add(slotButton);
            slotTexts.Add(slotText);
        }

        // --- Wire up references ---
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        saveLoadUI.GetType().GetField("closeButton", flags).SetValue(saveLoadUI, closeButton);
        saveLoadUI.GetType().GetField("slotButtons", flags).SetValue(saveLoadUI, slotButtons);
        saveLoadUI.GetType().GetField("slotTexts", flags).SetValue(saveLoadUI, slotTexts);
        saveLoadUI.GetType().GetField("titleText", flags).SetValue(saveLoadUI, titleText);
        saveLoadUI.GetType().GetField("notificationText", flags).SetValue(saveLoadUI, notificationText);

        // --- Create Prefab ---
        if (!Directory.Exists("Assets/Resources/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Resources/Prefabs");
        }
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);

        Debug.Log($"SaveLoadUI prefab created at '{prefabPath}'");
    }

    private static GameObject CreatePanel(GameObject parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);
        panel.AddComponent<Image>();
        return panel;
    }

    private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        return textComponent;
    }

    private static Button CreateButton(GameObject parent, string name, string buttonText)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();

        if (!string.IsNullOrEmpty(buttonText))
        {
            CreateText(buttonObj, "Text", buttonText, 24);
        }
        return button;
    }

    [MenuItem("Tools/Create In-Game Menu UI Prefab")]
    public static void CreateInGameMenuUIPrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs/InGameMenuUI.prefab";

        // --- Root Object ---
        GameObject root = new GameObject("InGameMenuUI");
        root.AddComponent<RectTransform>();
        root.AddComponent<CanvasGroup>();
        InGameMenuUI menuUI = root.AddComponent<InGameMenuUI>();

        // --- Menu Panel ---
        GameObject menuPanel = CreatePanel(root, "MenuPanel");
        RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 300);
        menuPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // --- Menu Title ---
        CreateText(menuPanel, "TitleText", "メニュー", 30);
        // ... positioning for title ...

        // --- Return to Title Button ---
        Button returnToTitleButton = CreateButton(menuPanel, "ReturnToTitleButton", "タイトルへ戻る");
        RectTransform returnRect = returnToTitleButton.GetComponent<RectTransform>();
        returnRect.anchorMin = new Vector2(0.5f, 0.5f);
        returnRect.anchorMax = new Vector2(0.5f, 0.5f);
        returnRect.pivot = new Vector2(0.5f, 0.5f);
        returnRect.anchoredPosition = new Vector2(0, 0);
        returnRect.sizeDelta = new Vector2(250, 50);

        // --- Close Menu Button ---
        Button closeMenuButton = CreateButton(menuPanel, "CloseMenuButton", "閉じる");
        RectTransform closeRect = closeMenuButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0, 20);
        closeRect.sizeDelta = new Vector2(180, 50);

        // --- Confirmation Dialog ---
        GameObject confirmationDialog = CreatePanel(root, "ConfirmationDialog");
        RectTransform confirmRect = confirmationDialog.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRect.sizeDelta = new Vector2(500, 250);

        CreateText(confirmationDialog, "ConfirmText", "セーブしていないデータは失われます。\nよろしいですか？", 24);

        Button confirmReturnButton = CreateButton(confirmationDialog, "ConfirmButton", "はい");
        RectTransform yesRect = confirmReturnButton.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(0.5f, 0f);
        yesRect.anchorMax = new Vector2(0.5f, 0f);
        yesRect.anchoredPosition = new Vector2(-100, 20);
        yesRect.sizeDelta = new Vector2(120, 50);

        Button cancelReturnButton = CreateButton(confirmationDialog, "CancelButton", "いいえ");
        RectTransform noRect = cancelReturnButton.GetComponent<RectTransform>();
        noRect.anchorMin = new Vector2(0.5f, 0f);
        noRect.anchorMax = new Vector2(0.5f, 0f);
        noRect.anchoredPosition = new Vector2(100, 20);
        noRect.sizeDelta = new Vector2(120, 50);

        // --- Wire up references ---
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        menuUI.GetType().GetField("returnToTitleButton", flags).SetValue(menuUI, returnToTitleButton);
        menuUI.GetType().GetField("closeMenuButton", flags).SetValue(menuUI, closeMenuButton);
        menuUI.GetType().GetField("confirmationDialog", flags).SetValue(menuUI, confirmationDialog);
        menuUI.GetType().GetField("confirmReturnButton", flags).SetValue(menuUI, confirmReturnButton);
        menuUI.GetType().GetField("cancelReturnButton", flags).SetValue(menuUI, cancelReturnButton);

        // --- Create Prefab ---
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);

        Debug.Log($"InGameMenuUI prefab created at '{prefabPath}'");
    }
}
