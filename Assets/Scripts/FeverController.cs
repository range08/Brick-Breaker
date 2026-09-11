using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks block hits for the current round and adds a temporary second copy of
/// the permanent volley when Fever starts. The border is a separate, narrow UI
/// object so the existing HUD layout remains untouched.
/// </summary>
public class FeverController : MonoBehaviour
{
    [Header("Fever")]
    [SerializeField, Min(1)] private int hitsToActivate = 30;
    [SerializeField, Min(0.01f)] private float borderCycleSpeed = 0.35f;
    [SerializeField, Range(0f, 1f)] private float borderSaturation = 0.8f;
    [SerializeField, Range(0f, 1f)] private float borderValue = 1f;
    [SerializeField, Range(0f, 1f)] private float borderAlpha = 0.8f;

    private readonly Image[] borderImages = new Image[4];
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
        Color baseColor = Color.HSVToRGB(hue, borderSaturation, borderValue);
        baseColor.a = borderAlpha;

        for (int i = 0; i < borderImages.Length; i++)
        {
            float shiftedHue = Mathf.Repeat(hue + i * 0.12f, 1f);
            Color edgeColor = Color.HSVToRGB(shiftedHue, borderSaturation, borderValue);
            edgeColor.a = borderAlpha;
            borderImages[i].color = i == 0 ? baseColor : edgeColor;
        }
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

        CreateEdge(borderObject.transform, 0, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 18f), Vector2.zero);
        CreateEdge(borderObject.transform, 1, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), Vector2.zero);
        CreateEdge(borderObject.transform, 2, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), Vector2.zero);
        CreateEdge(borderObject.transform, 3, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(18f, 0f), Vector2.zero);

        SetBorderVisible(false);
    }

    private void CreateEdge(Transform parent, int index, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
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
        edgeRect.anchoredPosition = anchoredPosition;

        Image image = edgeObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.clear;
        borderImages[index] = image;
    }

    private void SetBorderVisible(bool visible)
    {
        if (borderObject != null)
            borderObject.SetActive(visible);
    }
}
