using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Adds only the two new counters required by the round system. Existing UI
/// objects and their RectTransforms are not modified.
/// </summary>
public class GameHud : MonoBehaviour
{
    private TMP_Text roundText;
    private TMP_Text ballCountText;
    private TMP_Text styleSource;
    private RectTransform safeArea;
    private Button pauseButton;
    private TMP_Text pauseButtonText;
    private GameObject startScreen;
    private Button startButton;

    public TMP_Text RoundText => roundText;
    public TMP_Text BallCountText => ballCountText;
    public Button PauseButton => pauseButton;
    public Button StartButton => startButton;

    public void Initialize()
    {
        if (safeArea == null)
        {
            SafeAreaController safeAreaController = FindFirstObjectByType<SafeAreaController>();
            safeArea = safeAreaController != null ? safeAreaController.GetComponent<RectTransform>() : null;
        }

        if (safeArea == null)
            return;

        if (styleSource == null)
        {
            Transform source = safeArea.Find("ScoreText");
            styleSource = source != null ? source.GetComponent<TMP_Text>() : null;
        }

        roundText = FindOrCreateText("RoundText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(260f, 72f), TextAlignmentOptions.Left);
        ballCountText = FindOrCreateText("BallCountText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(260f, 72f), TextAlignmentOptions.Right);
        pauseButton = FindOrCreatePauseButton();
        startButton = FindOrCreateStartScreen();

        // Score is no longer part of the game loop. Keep its user-authored
        // RectTransform and font asset untouched, but hide the obsolete value.
        if (styleSource != null)
            styleSource.gameObject.SetActive(false);
    }

    public void Refresh(int round, int permanentBallCount)
    {
        if (roundText != null)
            roundText.text = $"ROUND {round:00}";

        if (ballCountText != null)
            ballCountText.text = $"BALLS {permanentBallCount:00}";
    }

    public void SetPauseCallback(UnityAction callback)
    {
        if (pauseButton == null)
            return;

        pauseButton.onClick.RemoveAllListeners();

        if (callback != null)
            pauseButton.onClick.AddListener(callback);
    }

    public void SetPauseVisible(bool visible)
    {
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(visible);
    }

    public void SetPaused(bool paused)
    {
        if (pauseButtonText != null)
            pauseButtonText.text = paused ? "RESUME" : "PAUSE";
    }

    public void SetStartCallback(UnityAction callback)
    {
        if (startButton == null)
            return;

        startButton.onClick.RemoveAllListeners();

        if (callback != null)
            startButton.onClick.AddListener(callback);
    }

    public void ShowStartScreen()
    {
        if (startScreen != null)
            startScreen.SetActive(true);

        SetPauseVisible(false);
    }

    public void HideStartScreen()
    {
        if (startScreen != null)
            startScreen.SetActive(false);
    }

    private TMP_Text FindOrCreateText(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        Transform existing = safeArea.Find(objectName);

        if (existing != null)
            return existing.GetComponent<TMP_Text>();

        GameObject textObject = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(safeArea, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = anchorMin;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.fontSize = 32f;
        text.color = new Color32(0x24, 0x2A, 0x3A, 0xFF);

        if (styleSource != null)
        {
            text.font = styleSource.font;
            text.fontSharedMaterial = styleSource.fontSharedMaterial;
        }

        return text;
    }

    private Button FindOrCreatePauseButton()
    {
        Transform existing = safeArea.Find("PauseButton");

        if (existing != null)
        {
            pauseButtonText = existing.GetComponentInChildren<TMP_Text>(true);
            return existing.GetComponent<Button>();
        }

        GameObject buttonObject = new("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(safeArea, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = new Vector2(0f, -24f);
        buttonRect.sizeDelta = new Vector2(150f, 64f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(0xFF, 0xFF, 0xFF, 220);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color32(0xFF, 0xFF, 0xFF, 220);
        colors.highlightedColor = new Color32(0xF0, 0xF3, 0xFA, 240);
        colors.pressedColor = new Color32(0xD8, 0xDE, 0xEC, 255);
        button.colors = colors;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        pauseButtonText = labelObject.GetComponent<TextMeshProUGUI>();
        pauseButtonText.text = "PAUSE";
        pauseButtonText.alignment = TextAlignmentOptions.Center;
        pauseButtonText.raycastTarget = false;
        pauseButtonText.enableAutoSizing = false;
        pauseButtonText.fontSize = 24f;
        pauseButtonText.color = new Color32(0x24, 0x2A, 0x3A, 0xFF);

        if (styleSource != null)
        {
            pauseButtonText.font = styleSource.font;
            pauseButtonText.fontSharedMaterial = styleSource.fontSharedMaterial;
        }

        return button;
    }

    private Button FindOrCreateStartScreen()
    {
        Transform existing = safeArea.Find("StartScreen");

        if (existing != null)
        {
            startScreen = existing.gameObject;
            ConfigureStartScreenCanvas();
            return existing.GetComponentInChildren<Button>(true);
        }

        startScreen = new GameObject("StartScreen", typeof(RectTransform), typeof(Image));
        startScreen.transform.SetParent(safeArea, false);
        ConfigureStartScreenCanvas();

        RectTransform screenRect = startScreen.GetComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.offsetMin = Vector2.zero;
        screenRect.offsetMax = Vector2.zero;

        Image screenImage = startScreen.GetComponent<Image>();
        screenImage.color = new Color32(0xF6, 0xF8, 0xFC, 255);
        screenImage.raycastTarget = true;

        GameObject titleObject = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(startScreen.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -180f);
        titleRect.sizeDelta = new Vector2(760f, 120f);

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "BRICK BREAKER";
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = false;
        title.fontSize = 64f;
        title.color = new Color32(0x24, 0x2A, 0x3A, 0xFF);
        title.raycastTarget = false;
        ApplyStyle(title);

        GameObject buttonObject = new("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(startScreen.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -430f);
        buttonRect.sizeDelta = new Vector2(320f, 104f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color32(0xFF, 0xFF, 0xFF, 235);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color32(0xFF, 0xFF, 0xFF, 235);
        colors.highlightedColor = new Color32(0xF0, 0xF3, 0xFA, 245);
        colors.pressedColor = new Color32(0xD8, 0xDE, 0xEC, 255);
        button.colors = colors;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "START";
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.fontSize = 32f;
        label.color = new Color32(0x24, 0x2A, 0x3A, 0xFF);
        label.raycastTarget = false;
        ApplyStyle(label);

        startScreen.SetActive(false);
        return button;
    }

    private void ConfigureStartScreenCanvas()
    {
        if (startScreen == null)
            return;

        Canvas canvas = startScreen.GetComponent<Canvas>();

        if (canvas == null)
            canvas = startScreen.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
    }

    private void ApplyStyle(TMP_Text text)
    {
        if (styleSource == null)
            return;

        text.font = styleSource.font;
        text.fontSharedMaterial = styleSource.fontSharedMaterial;
    }
}
