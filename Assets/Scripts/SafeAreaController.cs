using UnityEngine;

/// <summary>
/// Fits a UI RectTransform to the device's safe area.
///
/// Attach this component to a full-screen UI container located directly
/// under a full-screen Canvas. Put HUD elements such as score, pause,
/// settings, lives, etc. inside that container.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaController : MonoBehaviour
{
    [Header("Options")]
    [SerializeField]
    private bool applyHorizontal = true;

    [SerializeField]
    private bool applyVertical = true;

    private RectTransform rectTransform;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        Rect currentSafeArea = Screen.safeArea;
        Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (currentSafeArea != lastSafeArea || currentScreenSize != lastScreenSize)
        {
            ApplySafeArea();
        }
    }

    [ContextMenu("Apply Safe Area")]
    public void ApplySafeArea()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (!applyHorizontal)
        {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
        }

        if (!applyVertical)
        {
            anchorMin.y = 0f;
            anchorMax.y = 1f;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // Remove offsets so the RectTransform exactly matches the anchors.
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
