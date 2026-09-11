using UnityEngine;

/// <summary>
/// A single physics ball. Volley input and lifecycle ownership live in BallManager.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallScript : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField, Min(0.1f)] private float speed = 15f;

    private Rigidbody2D ballRigidbody;
    private BallManager ballManager;
    private Vector3 launchPosition;
    private bool isLaunched;
    private bool isReturning;

    public bool IsLaunched => isLaunched;
    public bool HasReturned => isReturning;
    public float Speed => speed;
    public Vector2 Velocity => ballRigidbody != null ? ballRigidbody.linearVelocity : Vector2.zero;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody2D>();
        ballRigidbody.gravityScale = 0f;
        ballRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        ballRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        launchPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (!isLaunched || ballRigidbody.linearVelocity.sqrMagnitude <= 0.01f)
            return;

        ballRigidbody.linearVelocity = ballRigidbody.linearVelocity.normalized * speed;
    }

    public void Initialize(BallManager owner)
    {
        ballManager = owner;
    }

    public void PrepareForLaunch()
    {
        PrepareForLaunch(launchPosition);
    }

    public void PrepareForLaunch(Vector3 position)
    {
        launchPosition = position;
        isLaunched = false;
        isReturning = false;

        gameObject.SetActive(true);
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
        transform.position = launchPosition;
    }

    public void SetLaunchPosition(Vector3 position)
    {
        launchPosition = position;
        PrepareForLaunch(position);
    }

    public void Launch(Vector2 direction)
    {
        if (!gameObject.activeInHierarchy)
            return;

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
        launchPosition = position;
        transform.position = position;
        gameObject.SetActive(false);
    }

    public void NotifyDeathZone()
    {
        if (!isLaunched || isReturning)
            return;

        isLaunched = false;
        isReturning = true;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;

        if (ballManager == null)
            ballManager = FindFirstObjectByType<BallManager>();

        if (ballManager != null)
            ballManager.NotifyBallReturned(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLaunched || isReturning || GameManager.Instance == null)
            return;

        if (collision.collider.TryGetComponent<BrickBlock>(out _))
            GameManager.Instance.AudioManager?.PlayBallBlock();
        else
            GameManager.Instance.AudioManager?.PlayBallWall();
    }

    public float GetWorldRadius()
    {
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider == null)
            return 0.25f;

        Vector3 scale = transform.lossyScale;
        float scaleFactor = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        return circleCollider.radius * scaleFactor;
    }

    private void OnDisable()
    {
        if (ballRigidbody == null)
            return;

        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
    }
}
