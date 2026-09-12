using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks block hits for the current round and adds a temporary second copy of
/// the permanent volley when Fever starts. The border is a separate, narrow UI
/// object so the existing HUD layout remains untouched.
/// </summary>
public class FeverController : MonoBehaviour
{
    private const float BorderThickness = 18f;
    private const float BorderHueOffset = 0.04f;
    private static readonly float[] CornerHueOffsets = { -0.5f, 0.5f, 1.5f, 2.5f };

    [Header("Fever")]
    [SerializeField, Min(1)] private int hitsToActivate = 30;
    [SerializeField, Min(0.01f)] private float borderCycleSpeed = 0.35f;
    [SerializeField, Range(0f, 1f)] private float borderSaturation = 0.8f;
    [SerializeField, Range(0f, 1f)] private float borderValue = 1f;
    [SerializeField, Range(0f, 1f)] private float borderAlpha = 0.8f;

    private readonly Image[] borderImages = new Image[4];
    private readonly Image[] cornerImages = new Image[4];
    private BallManager ballManager;
    private RectTransform borderParent;
    private GameObject borderObject;
    private bool feverActive;
    private int hitCount;
    private float hue;

    public int HitCount => hitCount;
    public int HitsToActivate => hitsToActivate;
    public bool IsFeverActive => feverActive;
    public bool IsBorderVisible => borderObject != null && borderObject.activeSelf;

    public void Initialize(GameManager owner)
    {
        ballManager = owner != null ? owner.BallManager : FindFirstObjectByType<BallManager>();
        CreateBorder();
        EndRound();
    }

    public void BeginRound()
    {
        hitCount = 0;
        feverActive = false;
        SetBorderVisible(false);
    }

    public void RegisterHit()
    {
        if (feverActive)
            return;

        hitCount++;

        if (hitCount >= hitsToActivate)
            EnterFever();
    }

    public void EndRound()
    {
        hitCount = 0;

        if (feverActive)
            ballManager?.EndFever();

        feverActive = false;
        SetBorderVisible(false);
    }

    private void EnterFever()
    {
        if (feverActive || ballManager == null || !ballManager.IsVolleyActive)
            return;

        feverActive = true;
        int addedBalls = ballManager.AddTemporaryFeverBalls();

        if (addedBalls <= 0)
        {
            feverActive = false;
            return;
        }

        hue = 0f;
        SetBorderVisible(true);
    }

    private void Update()
    {
        if (!feverActive || borderImages[0] == null)
            return;

        hue = Mathf.Repeat(hue + Time.deltaTime * borderCycleSpeed, 1f);

        for (int i = 0; i < borderImages.Length; i++)
            borderImages[i].color = GetBorderColor(hue + i * BorderHueOffset);

        for (int i = 0; i < cornerImages.Length; i++)
            cornerImages[i].color = GetBorderColor(hue + CornerHueOffsets[i] * BorderHueOffset);
    }

    private void CreateBorder()
    {
        SafeAreaController safeAreaController = FindFirstObjectByType<SafeAreaController>();
        borderParent = safeAreaController != null
            ? safeAreaController.GetComponent<RectTransform>()
            : FindFirstObjectByType<Canvas>()?.GetComponent<RectTransform>();

        if (borderParent == null)
            return;

        Transform existing = borderParent.Find("FeverBorder");

        if (existing != null)
        {
            borderObject = existing.gameObject;
        }
        else
        {
            borderObject = new GameObject("FeverBorder", typeof(RectTransform));
            borderObject.transform.SetParent(borderParent, false);
        }

        RectTransform borderRect = borderObject.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        borderImages[0] = CreateBorderImage(borderObject.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-BorderThickness * 2f, BorderThickness));
        borderImages[1] = CreateBorderImage(borderObject.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(BorderThickness, -BorderThickness * 2f));
        borderImages[2] = CreateBorderImage(borderObject.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-BorderThickness * 2f, BorderThickness));
        borderImages[3] = CreateBorderImage(borderObject.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(BorderThickness, -BorderThickness * 2f));

        cornerImages[0] = CreateBorderImage(borderObject.transform, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.one * BorderThickness);
        cornerImages[1] = CreateBorderImage(borderObject.transform, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.one * BorderThickness);
        cornerImages[2] = CreateBorderImage(borderObject.transform, "BottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.one * BorderThickness);
        cornerImages[3] = CreateBorderImage(borderObject.transform, "BottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.one * BorderThickness);

        SetBorderVisible(false);
    }

    private Image CreateBorderImage(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        Transform existing = parent.Find(objectName);
        GameObject edgeObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(Image));

        if (existing == null)
            edgeObject.transform.SetParent(parent, false);

        RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
        edgeRect.anchorMin = anchorMin;
        edgeRect.anchorMax = anchorMax;
        edgeRect.pivot = pivot;
        edgeRect.sizeDelta = sizeDelta;
        edgeRect.anchoredPosition = Vector2.zero;

        Image image = edgeObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }

    private Color GetBorderColor(float targetHue)
    {
        Color color = Color.HSVToRGB(Mathf.Repeat(targetHue, 1f), borderSaturation, borderValue);
        color.a = borderAlpha;
        return color;
    }

    private void SetBorderVisible(bool visible)
    {
        if (borderObject != null)
            borderObject.SetActive(visible);
    }
}
