using UnityEngine;

/// <summary>
/// Portrait 2D mobile camera controller.
///
/// Keeps the visible world width constant across devices.
/// Taller screens simply reveal more vertical world space,
/// which is useful for portrait games such as Block Breaker.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MobileCameraController : MonoBehaviour
{
    [Header("World View")]
    [SerializeField, Min(0.1f)]
    private float targetWorldWidth = 10f;

    [Header("Optional Aspect Ratio Clamp")]
    [Tooltip("Enable this if you want to prevent extremely tall/narrow displays from changing the camera too much.")]
    [SerializeField]
    private bool clampAspectRatio = false;

    [Tooltip("Minimum width / height ratio. 9:20 ≈ 0.45.")]
    [SerializeField, Range(0.3f, 1f)]
    private float minimumAspect = 9f / 20f;

    [Tooltip("Maximum width / height ratio. 9:16 = 0.5625.")]
    [SerializeField, Range(0.3f, 1f)]
    private float maximumAspect = 9f / 16f;

    private Camera targetCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    public float TargetWorldWidth => targetWorldWidth;
    public float VisibleWorldHeight => targetCamera != null
        ? targetCamera.orthographicSize * 2f
        : 0f;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning(
                $"{nameof(MobileCameraController)} is intended for an Orthographic camera. " +
                "Orthographic mode has been enabled automatically.",
                this);

            targetCamera.orthographic = true;
        }

        RefreshCameraSize();
    }

    private void OnEnable()
    {
        RefreshCameraSize();
    }

    private void Update()
    {
        // Useful in the Editor/Game View and for rare runtime resolution changes.
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            RefreshCameraSize();
        }
    }

    [ContextMenu("Refresh Camera Size")]
    public void RefreshCameraSize()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        float aspect = (float)Screen.width / Screen.height;

        if (clampAspectRatio)
        {
            float min = Mathf.Min(minimumAspect, maximumAspect);
            float max = Mathf.Max(minimumAspect, maximumAspect);
            aspect = Mathf.Clamp(aspect, min, max);
        }

        // Orthographic Size = half of the visible vertical world size.
        // visibleWidth = (orthographicSize * 2) * aspect
        // therefore:
        // orthographicSize = targetWorldWidth / (2 * aspect)
        targetCamera.orthographicSize = targetWorldWidth / (2f * aspect);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        targetWorldWidth = Mathf.Max(0.1f, targetWorldWidth);

        if (!Application.isPlaying)
        {
            Camera cam = GetComponent<Camera>();

            if (cam != null && cam.orthographic && Screen.width > 0 && Screen.height > 0)
            {
                float aspect = (float)Screen.width / Screen.height;

                if (clampAspectRatio)
                {
                    float min = Mathf.Min(minimumAspect, maximumAspect);
                    float max = Mathf.Max(minimumAspect, maximumAspect);
                    aspect = Mathf.Clamp(aspect, min, max);
                }

                cam.orthographicSize = targetWorldWidth / (2f * aspect);
            }
        }
    }
#endif
}
