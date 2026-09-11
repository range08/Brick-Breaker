using TMPro;
using UnityEngine;

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

    public TMP_Text RoundText => roundText;
    public TMP_Text BallCountText => ballCountText;

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
}
