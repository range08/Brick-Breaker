using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the single playable ball, including the ready/launch state and
/// mouse or touch drag input used to aim it.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallScript : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField, Min(0.1f)]
    private float speed = 7.5f;

    [SerializeField, Min(0.01f)]
    private float minimumDragPixels = 35f;

    [SerializeField, Range(0f, 0.8f)]
    private float minimumUpwardComponent = 0.2f;

    private Rigidbody2D ballRigidbody;
    private Camera worldCamera;
    private Vector3 launchPosition;
    private Vector2 dragStartScreenPosition;
    private bool isDragging;
    private bool isLaunched;

    public bool IsLaunched => isLaunched;
    public float Speed => speed;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody2D>();
        ballRigidbody.gravityScale = 0f;
        ballRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        ballRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        launchPosition = transform.position;
    }

    private void Start()
    {
        worldCamera = Camera.main;
        PrepareForLaunch();
    }

    private void Update()
    {
        if (isLaunched)
            return;

        if (TryGetPointerDown(out Vector2 pointerPosition))
        {
            dragStartScreenPosition = pointerPosition;
            isDragging = true;
        }

        if (!isDragging)
            return;

        if (TryGetPointerUp(out pointerPosition))
        {
            isDragging = false;
            TryLaunch(pointerPosition);
        }
    }

    private void FixedUpdate()
    {
        if (!isLaunched)
            return;

        // Keep the speed stable after repeated Rigidbody2D collisions.
        if (ballRigidbody.linearVelocity.sqrMagnitude > 0.01f)
            ballRigidbody.linearVelocity = ballRigidbody.linearVelocity.normalized * speed;
    }

    public void PrepareForLaunch()
    {
        isLaunched = false;
        isDragging = false;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
        transform.position = launchPosition;
    }

    public void SetLaunchPosition(Vector3 position)
    {
        launchPosition = position;
        PrepareForLaunch();
    }

    private void TryLaunch(Vector2 releaseScreenPosition)
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        if ((releaseScreenPosition - dragStartScreenPosition).sqrMagnitude < minimumDragPixels * minimumDragPixels)
            return;

        Vector2 targetWorldPosition = worldCamera.ScreenToWorldPoint(releaseScreenPosition);
        Vector2 direction = targetWorldPosition - (Vector2)transform.position;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        // Prevent a nearly horizontal shot from getting trapped along the bottom edge.
        if (direction.y < minimumUpwardComponent)
        {
            direction.y = minimumUpwardComponent;
            direction.Normalize();
        }

        isLaunched = true;
        ballRigidbody.linearVelocity = direction * speed;

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyBallLaunched();
    }

    private static bool TryGetPointerDown(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private static bool TryGetPointerUp(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }
}
