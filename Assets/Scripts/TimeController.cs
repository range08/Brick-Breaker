using UnityEngine;

/// <summary>
/// The only owner of Time.timeScale. Automatic speed-up uses unscaled time so
/// the thresholds remain stable while the game is already accelerated.
/// </summary>
public class TimeController : MonoBehaviour
{
    [Header("Automatic Speed")]
    [SerializeField, Min(0f)] private float doubleSpeedAfterSeconds = 8f;
    [SerializeField, Min(0f)] private float tripleSpeedAfterSeconds = 14f;
    [SerializeField, Range(1f, 3f)] private float maximumAutomaticScale = 3f;

    private float flightElapsed;
    private float activeScale = 1f;
    private float pauseResumeScale = 1f;
    private bool flightActive;
    private bool paused;
    private bool gameOver;

    public float ActiveScale => activeScale;
    public float FlightElapsed => flightElapsed;
    public bool IsPaused => paused;

    public void Initialize()
    {
        flightElapsed = 0f;
        activeScale = 1f;
        pauseResumeScale = 1f;
        flightActive = false;
        paused = false;
        gameOver = false;
        ApplyScale(1f);
    }

    public void BeginRound()
    {
        flightElapsed = 0f;
        flightActive = false;
        paused = false;
        gameOver = false;
        pauseResumeScale = 1f;
        ApplyScale(1f);
    }

    public void BeginFlight()
    {
        if (gameOver || paused || flightActive)
            return;

        flightElapsed = 0f;
        flightActive = true;
        ApplyScale(1f);
    }

    public void EndRound()
    {
        flightActive = false;
        paused = false;
        pauseResumeScale = 1f;
        ApplyScale(1f);
    }

    public void SetGameOver()
    {
        gameOver = true;
        flightActive = false;
        paused = false;
        pauseResumeScale = 1f;
        ApplyScale(1f);
    }

    public bool SetPaused(bool shouldPause)
    {
        if (gameOver)
            return false;

        if (shouldPause)
        {
            if (paused)
                return true;

            pauseResumeScale = activeScale;
            paused = true;
            ApplyScale(0f);
            return true;
        }

        if (!paused)
            return false;

        paused = false;
        ApplyScale(pauseResumeScale);
        return true;
    }

    private void Update()
    {
        if (!flightActive || paused || gameOver)
            return;

        flightElapsed += Time.unscaledDeltaTime;

        float targetScale = 1f;

        if (flightElapsed >= tripleSpeedAfterSeconds)
            targetScale = maximumAutomaticScale;
        else if (flightElapsed >= doubleSpeedAfterSeconds)
            targetScale = Mathf.Min(2f, maximumAutomaticScale);

        if (!Mathf.Approximately(targetScale, activeScale))
            ApplyScale(targetScale);
    }

    private void ApplyScale(float scale)
    {
        activeScale = Mathf.Clamp(scale, 0f, maximumAutomaticScale);
        Time.timeScale = activeScale;
    }

    private void OnDisable()
    {
        if (Time.timeScale != 1f)
            Time.timeScale = 1f;
    }
}
