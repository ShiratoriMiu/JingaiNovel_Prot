using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro; // Required for TextMeshPro

public class SceneBuilder
{
    [MenuItem("Tools/Build Title Scene")]
    public static void BuildTitleScene()
    {
        // --- Scene Setup ---
        string scenePath = "Assets/Scenes/TitleScene.unity";
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera Setup ---
        GameObject cameraObj = GameObject.Find("Main Camera");
        if (cameraObj != null)
        {
            Camera camera = cameraObj.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark grey background
        }

        // --- UI Setup ---
        // Add Event System
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Add Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Manager Setup ---
        GameObject managerObj = new GameObject("TitleManager");
        TitleManager titleManager = managerObj.AddComponent<TitleManager>();

        // --- Button Layout ---
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();

        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(-280, 0); // Position left of center
        containerRect.sizeDelta = new Vector2(220, 300);

        VerticalLayoutGroup layoutGroup = buttonContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // --- Create Buttons ---
        string[] buttonLabels = { "はじめから", "つづきから", "設定", "終了" };
        Button startGameButton = null;

        foreach (string label in buttonLabels)
        {
            GameObject buttonObj = new GameObject(label + " Button");
            buttonObj.transform.SetParent(buttonContainer.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            // A simple default sprite is used here. You might want to assign a custom one.
            buttonImage.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark button color

            Button button = buttonObj.AddComponent<Button>();
            buttonObj.AddComponent<LayoutElement>().preferredHeight = 50;

            GameObject textObj = new GameObject("Text (TMP)");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = label;
            buttonText.fontSize = 28;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            if (label == "はじめから")
            {
                startGameButton = button;
            }
        }

        // --- Button Event ---
        if (startGameButton != null && titleManager != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                startGameButton.onClick,
                titleManager.StartGame
            );
        }

        // --- Build Settings ---
        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(scenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true)
        };
        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();

        // --- Finalization ---
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"TitleScene created at '{scenePath}' and build settings updated.");
    }
}
