using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the persistent ball count and the balls taking part in the current volley.
/// Input is intentionally handled here so a volley can lock out additional drags.
/// </summary>
public class BallManager : MonoBehaviour
{
    [Header("Ball References")]
    [SerializeField] private BallScript ballTemplate;

    [Header("Permanent Ball Count")]
    [SerializeField, Min(1)] private int permanentBallCount = 1;

    [Header("Volley")]
    [SerializeField, Range(0.05f, 0.2f)] private float launchInterval = 0.08f;

    private readonly List<BallScript> pooledBalls = new();
    private readonly List<BallScript> activeBalls = new();

    private GameManager gameManager;
    private Camera worldCamera;
    private Vector3 nextLaunchPosition;
    private Vector2 dragStartScreenPosition;
    private bool isDragging;
    private bool launchSequenceRunning;
    private bool volleyActive;
    private bool firstLandingSaved;
    private int returnedBallCount;
    private Vector2 lastLaunchDirection = Vector2.up;

    public int PermanentBallCount => permanentBallCount;
    public int ActiveBallCount => activeBalls.Count;
    public int ReturnedBallCount => returnedBallCount;
    public int CurrentVolleyCount { get; private set; }
    public bool IsVolleyActive => volleyActive;
    public bool IsLaunchSequenceRunning => launchSequenceRunning;
    public Vector3 NextLaunchPosition => nextLaunchPosition;
    public Vector2 LastLaunchDirection => lastLaunchDirection;
    public IReadOnlyList<BallScript> ActiveBalls => activeBalls;

    public void Initialize(GameManager owner, BallScript template)
    {
        gameManager = owner;
        worldCamera = Camera.main;

        if (ballTemplate == null)
            ballTemplate = template;

        if (ballTemplate == null)
            return;

        if (!pooledBalls.Contains(ballTemplate))
            pooledBalls.Add(ballTemplate);

        nextLaunchPosition = ballTemplate.transform.position;
        ballTemplate.Initialize(this);
        ballTemplate.gameObject.SetActive(false);
    }

    public void PrepareForRound()
    {
        StopAllBalls();

        returnedBallCount = 0;
        CurrentVolleyCount = permanentBallCount;
        firstLandingSaved = false;
        isDragging = false;
        launchSequenceRunning = false;
        volleyActive = false;

        EnsurePoolSize(CurrentVolleyCount);

        for (int i = 0; i < CurrentVolleyCount; i++)
        {
            BallScript ball = pooledBalls[i];
            ball.Initialize(this);
            ball.PrepareForLaunch(nextLaunchPosition);
        }
    }

    public void SetNextLaunchPosition(Vector3 position)
    {
        nextLaunchPosition = position;
        nextLaunchPosition.z = 0f;
    }

    public void AddPermanentBall(int amount)
    {
        if (amount <= 0)
            return;

        permanentBallCount += amount;
    }

    public bool TryLaunch(Vector2 direction)
    {
        if (gameManager == null || !gameManager.CanAcceptAim || launchSequenceRunning || volleyActive)
            return false;

        direction = ClampLaunchDirection(direction);

        if (direction.sqrMagnitude < 0.01f)
            return false;

        StartCoroutine(LaunchVolley(direction));
        return true;
    }

    public void NotifyBallReturned(BallScript ball)
    {
        if (!volleyActive || ball == null || !activeBalls.Contains(ball))
            return;

        if (!firstLandingSaved)
        {
            firstLandingSaved = true;
            SetNextLaunchPosition(new Vector3(ball.transform.position.x, nextLaunchPosition.y, 0f));
        }

        returnedBallCount++;
        activeBalls.Remove(ball);
        ball.StopAndReturn(nextLaunchPosition);

        if (returnedBallCount >= CurrentVolleyCount && !launchSequenceRunning && activeBalls.Count == 0)
            CompleteVolley();
    }

    public void StopAllBalls()
    {
        for (int i = 0; i < pooledBalls.Count; i++)
        {
            BallScript ball = pooledBalls[i];

            if (ball != null)
                ball.StopAndReturn(nextLaunchPosition);
        }

        activeBalls.Clear();
        launchSequenceRunning = false;
        volleyActive = false;
        isDragging = false;
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.CanAcceptAim || launchSequenceRunning || volleyActive)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (TryGetPointerDown(out Vector2 pointerPosition))
        {
            dragStartScreenPosition = pointerPosition;
            isDragging = true;
        }

        if (!isDragging || !TryGetPointerUp(out pointerPosition))
            return;

        isDragging = false;

        if (worldCamera == null)
            return;

        Vector2 targetWorldPosition = worldCamera.ScreenToWorldPoint(pointerPosition);
        Vector2 direction = targetWorldPosition - (Vector2)nextLaunchPosition;

        if ((pointerPosition - dragStartScreenPosition).sqrMagnitude < GetMinimumDragPixelsSquared())
            return;

        TryLaunch(direction);
    }

    private IEnumerator LaunchVolley(Vector2 direction)
    {
        lastLaunchDirection = direction;
        launchSequenceRunning = true;
        volleyActive = true;
        returnedBallCount = 0;
        firstLandingSaved = false;
        activeBalls.Clear();

        EnsurePoolSize(CurrentVolleyCount);

        for (int i = 0; i < CurrentVolleyCount; i++)
        {
            BallScript ball = pooledBalls[i];
            ball.PrepareForLaunch(nextLaunchPosition);
            activeBalls.Add(ball);

            if (i > 0)
                yield return new WaitForSeconds(launchInterval);

            ball.Launch(direction);

            if (gameManager != null)
                gameManager.NotifyBallLaunched(ball);
        }

        launchSequenceRunning = false;

        if (gameManager != null)
            gameManager.NotifyVolleyLaunchSequenceComplete();
    }

    private void CompleteVolley()
    {
        volleyActive = false;
        launchSequenceRunning = false;

        for (int i = 0; i < pooledBalls.Count; i++)
        {
            BallScript ball = pooledBalls[i];

            if (ball != null)
                ball.StopAndReturn(nextLaunchPosition);
        }

        if (gameManager != null)
            gameManager.NotifyAllBallsReturned(nextLaunchPosition);
    }

    private void EnsurePoolSize(int requiredCount)
    {
        if (ballTemplate == null)
            return;

        while (pooledBalls.Count < requiredCount)
        {
            BallScript clone = Instantiate(ballTemplate, nextLaunchPosition, Quaternion.identity, transform);
            clone.name = $"Ball_{pooledBalls.Count + 1}";
            clone.Initialize(this);
            clone.gameObject.SetActive(false);
            pooledBalls.Add(clone);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private float GetMinimumDragPixelsSquared()
    {
        return 35f * 35f;
    }

    private static Vector2 ClampLaunchDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return Vector2.zero;

        direction.Normalize();

        if (direction.y < 0.2f)
        {
            direction.y = 0.2f;
            direction.Normalize();
        }

        return direction;
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
