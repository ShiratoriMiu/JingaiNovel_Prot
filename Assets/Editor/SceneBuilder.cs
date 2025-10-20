using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using TMPro; // TextMeshProをインポート

public class SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";

    // メニューからシーンを新規作成するための関数
    [MenuItem("Tools/1. Setup Game Scene")]
    public static void SetupGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject canvasGo = CreateCanvas();
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Scene '{ScenePath}' created with a Canvas set to 1280x720 resolution.");
    }

    // メニューからUI要素をシーンに追加するための関数
    [MenuItem("Tools/2. Add UI Elements to Game Scene")]
    public static void AddUIElementsToScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            Debug.LogError("Canvas not found in the scene. Please run 'Tools/1. Setup Game Scene' first.");
            return;
        }

        // UI要素がすでに追加されていないか確認
        if (canvasGo.transform.Find("BackgroundImage"))
        {
            Debug.Log("UI elements seem to be already added. Aborting.");
            return;
        }

        CreateBackgroundImage(canvasGo);
        CreateCharacterImage(canvasGo);
        CreateTextWindow(canvasGo);
        CreateChoiceButtons(canvasGo);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("UI elements added to the Game Scene.");
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280, 720);

        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    private static void CreateBackgroundImage(GameObject canvasGo)
    {
        GameObject bgImageGo = new GameObject("BackgroundImage");
        bgImageGo.transform.SetParent(canvasGo.transform);
        RawImage rawImage = bgImageGo.AddComponent<RawImage>();
        rawImage.color = new Color(0.5f, 0.5f, 0.5f, 1); // Default color

        RectTransform rect = bgImageGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateCharacterImage(GameObject canvasGo)
    {
        GameObject charImageGo = new GameObject("CharacterImage");
        charImageGo.transform.SetParent(canvasGo.transform);
        Image image = charImageGo.AddComponent<Image>();
        image.color = new Color(1, 1, 1, 0); // Transparent by default

        RectTransform rect = charImageGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500, 1000); // Default size
    }

    private static void CreateTextWindow(GameObject canvasGo)
    {
        // Text Window Panel
        GameObject textWindowGo = new GameObject("TextWindow");
        textWindowGo.transform.SetParent(canvasGo.transform);
        Image panelImage = textWindowGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black

        RectTransform rect = textWindowGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(0, 200); // Height of 200

        // Name Text
        GameObject nameTextGo = new GameObject("NameText");
        nameTextGo.transform.SetParent(textWindowGo.transform);
        TextMeshProUGUI nameText = nameTextGo.AddComponent<TextMeshProUGUI>();
        nameText.text = "Character Name";
        nameText.fontSize = 28;
        nameText.fontStyle = FontStyles.Bold;

        RectTransform nameRect = nameTextGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(0, 1);
        nameRect.pivot = new Vector2(0, 1);
        nameRect.anchoredPosition = new Vector2(30, -20);
        nameRect.sizeDelta = new Vector2(300, 40);

        // Dialogue Text
        GameObject dialogueTextGo = new GameObject("DialogueText");
        dialogueTextGo.transform.SetParent(textWindowGo.transform);
        TextMeshProUGUI dialogueText = dialogueTextGo.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "This is a sample dialogue text. Click to continue...";
        dialogueText.fontSize = 24;

        RectTransform dialogueRect = dialogueTextGo.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0, 0);
        dialogueRect.anchorMax = new Vector2(1, 1);
        dialogueRect.offsetMin = new Vector2(30, 20);
        dialogueRect.offsetMax = new Vector2(-30, -70);
    }

    private static void CreateChoiceButtons(GameObject canvasGo)
    {
        // Layout Group
        GameObject layoutGo = new GameObject("ChoiceButtons");
        layoutGo.transform.SetParent(canvasGo.transform);
        VerticalLayoutGroup layoutGroup = layoutGo.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 15;
        layoutGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform rect = layoutGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(400, 200);

        // Button Template
        GameObject buttonGo = new GameObject("ButtonTemplate");
        buttonGo.transform.SetParent(layoutGo.transform);
        buttonGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        buttonGo.AddComponent<Button>();
        LayoutElement layoutElement = buttonGo.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 400;
        layoutElement.preferredHeight = 60;

        // Text on Button
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform);
        TextMeshProUGUI buttonText = textGo.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Choice Text";
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 24;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Initially inactive
        layoutGo.SetActive(false);
    }
}
