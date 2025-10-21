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

        // Add SaveLoadUI component
        SaveLoadUI saveLoadUI = root.AddComponent<SaveLoadUI>();

        // --- Main Panel ---
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
        saveLoadUI.GetType().GetField("mainPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(saveLoadUI, mainPanel);
        saveLoadUI.GetType().GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(saveLoadUI, closeButton);
        saveLoadUI.GetType().GetField("slotButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(saveLoadUI, slotButtons);
        saveLoadUI.GetType().GetField("slotTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(saveLoadUI, slotTexts);
        saveLoadUI.GetType().GetField("titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(saveLoadUI, titleText);
        saveLoadUI.GetType().GetField("notificationText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(saveLoadUI, notificationText);

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
}
