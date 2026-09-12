using UnityEngine;

/// <summary>
/// A single physics ball. Volley input and lifecycle ownership live in BallManager.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallScript : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField, Min(0.1f)] private float speed = 15f;

    [SerializeField, Range(0.01f, 0.2f)] private float minimumVerticalComponent = 0.1f;

    private Rigidbody2D ballRigidbody;
    private CircleCollider2D ballCollider;
    private BallManager ballManager;
    private Vector3 launchPosition;
    private bool isLaunched;
    private bool isReturning;

    private void Awake()
    {
        CachePhysicsComponents();
        SetPhysicsEnabled(false);
        launchPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (!isLaunched || ballRigidbody == null || ballRigidbody.linearVelocity.sqrMagnitude <= 0.01f)
            return;

        Vector2 direction = ballRigidbody.linearVelocity.normalized;

        if (Mathf.Abs(direction.y) < minimumVerticalComponent)
        {
            float verticalSign = direction.y == 0f ? 1f : Mathf.Sign(direction.y);
            direction.y = verticalSign * minimumVerticalComponent;
            direction.Normalize();
        }

        ballRigidbody.linearVelocity = direction * speed;
    }

    public void Initialize(BallManager owner)
    {
        ballManager = owner;
        CachePhysicsComponents();
        SetPhysicsEnabled(false);
    }

    public void PrepareForLaunch(Vector3 position)
    {
        launchPosition = position;
        isLaunched = false;
        isReturning = false;

        SetPhysicsEnabled(false);
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
        transform.position = launchPosition;
        gameObject.SetActive(false);
    }

    public void ShowReadyForAim(Vector3 position)
    {
        PrepareForLaunch(position);
        gameObject.SetActive(true);
    }

    public void Launch(Vector2 direction)
    {
        if (ballRigidbody == null)
            return;

        gameObject.SetActive(true);
        SetPhysicsEnabled(true);
        isReturning = false;
        isLaunched = true;
        ballRigidbody.linearVelocity = direction.normalized * speed;
    }

    public void StopAndReturn(Vector3 position)
    {
        isLaunched = false;
        isReturning = true;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
        SetPhysicsEnabled(false);
        launchPosition = position;
        transform.position = position;
        gameObject.SetActive(false);
    }

    public void HoldAtLaunchPosition(Vector3 position)
    {
        launchPosition = position;
        transform.position = position;
        gameObject.SetActive(true);
    }

    public void SetReturnPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void NotifyDeathZone()
    {
        if (!isLaunched || isReturning)
            return;

        isLaunched = false;
        isReturning = true;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
        SetPhysicsEnabled(false);

        if (ballManager == null)
            ballManager = FindFirstObjectByType<BallManager>();

        if (ballManager != null)
            ballManager.NotifyBallReturned(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLaunched || isReturning || GameManager.Instance == null)
            return;

        if (collision.collider.TryGetComponent<BallScript>(out _))
            return;

        if (collision.collider.TryGetComponent<BrickBlock>(out _))
            GameManager.Instance.AudioManager?.PlayBallBlock();
        else
            GameManager.Instance.AudioManager?.PlayBallWall();
    }

    public float GetWorldRadius()
    {
        if (ballCollider == null)
            return 0.25f;

        Vector3 scale = transform.lossyScale;
        float scaleFactor = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        return ballCollider.radius * scaleFactor;
    }

    private void OnDisable()
    {
        if (ballRigidbody == null)
            return;

        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (ballRigidbody != null)
            ballRigidbody.simulated = enabled;

        if (ballCollider != null)
            ballCollider.enabled = enabled;
    }

    private void CachePhysicsComponents()
    {
        if (ballRigidbody == null)
            ballRigidbody = GetComponent<Rigidbody2D>();

        if (ballCollider == null)
            ballCollider = GetComponent<CircleCollider2D>();

        if (ballRigidbody == null)
            return;

        ballRigidbody.gravityScale = 0f;
        ballRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        ballRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
}
