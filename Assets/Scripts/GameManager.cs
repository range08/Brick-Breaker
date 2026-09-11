using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Start,
    Aiming,
    Playing,
    RoundEnd,
    GameOver,
    Pause
}

/// <summary>
/// Owns global game state and moves the game from one completed volley to the
/// next round. Feature-specific work is delegated to the managers on this object.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private BallScript ball;
    [SerializeField] private BrickBlock brickTemplate;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    private BallManager ballManager;
    private BlockGridManager blockGridManager;
    private GameHud gameHud;
    private TrajectoryPreview trajectoryPreview;
    private TimeController timeController;
    private FeverController feverController;
    private AudioManager audioManager;
    private HitEffectPool hitEffectPool;
    private bool gameOver;
    private GameState stateBeforePause = GameState.Aiming;

    public GameState State { get; private set; } = GameState.Start;
    public int Round { get; private set; } = 1;
    public bool CanAcceptAim => !gameOver && State == GameState.Aiming;
    public BallManager BallManager => ballManager;
    public BlockGridManager BlockGridManager => blockGridManager;
    public GameHud GameHud => gameHud;
    public TrajectoryPreview TrajectoryPreview => trajectoryPreview;
    public TimeController TimeController => timeController;
    public FeverController FeverController => feverController;
    public AudioManager AudioManager => audioManager;
    public HitEffectPool HitEffectPool => hitEffectPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Application.targetFrameRate = 60;
        ballManager = GetComponent<BallManager>();
        blockGridManager = GetComponent<BlockGridManager>();
        gameHud = GetComponent<GameHud>();
        trajectoryPreview = GetComponent<TrajectoryPreview>();
        timeController = GetComponent<TimeController>();
        feverController = GetComponent<FeverController>();
        audioManager = GetComponent<AudioManager>();
        hitEffectPool = GetComponent<HitEffectPool>();

        if (ballManager == null)
            ballManager = gameObject.AddComponent<BallManager>();

        if (blockGridManager == null)
            blockGridManager = gameObject.AddComponent<BlockGridManager>();

        if (gameHud == null)
            gameHud = gameObject.AddComponent<GameHud>();

        if (trajectoryPreview == null)
            trajectoryPreview = gameObject.AddComponent<TrajectoryPreview>();

        if (timeController == null)
            timeController = gameObject.AddComponent<TimeController>();

        if (feverController == null)
            feverController = gameObject.AddComponent<FeverController>();

        if (audioManager == null)
            audioManager = gameObject.AddComponent<AudioManager>();

        if (hitEffectPool == null)
            hitEffectPool = gameObject.AddComponent<HitEffectPool>();
    }

    private void Start()
    {
        ResolveSceneReferences();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        blockGridManager.Initialize(this, brickTemplate);
        blockGridManager.BuildInitialGrid(Round);
        ballManager.Initialize(this, ball);
        gameHud.Initialize();
        gameHud.SetPauseCallback(TogglePause);
        gameHud.SetStartCallback(BeginGame);
        timeController.Initialize();
        feverController.Initialize(this);
        audioManager.Initialize();
        hitEffectPool.Initialize();
        PrepareStartScreen();
    }

    public void BeginGame()
    {
        if (gameOver)
            return;

        gameHud?.HideStartScreen();
        State = GameState.Aiming;
        stateBeforePause = GameState.Aiming;
        timeController.BeginRound();
        feverController.BeginRound();
        ballManager.PrepareForRound();
        gameHud?.Refresh(Round, ballManager.PermanentBallCount);
        gameHud?.SetPauseVisible(true);
        gameHud?.SetPaused(false);
    }

    public void NotifyBallLaunched(BallScript launchedBall)
    {
        if (!gameOver)
        {
            State = GameState.Playing;
            timeController.BeginFlight();
        }
    }

    public void NotifyVolleyLaunchSequenceComplete()
    {
        if (!gameOver)
            State = GameState.Playing;
    }

    public void NotifyAllBallsReturned(Vector3 nextLaunchPosition)
    {
        if (gameOver)
            return;

        State = GameState.RoundEnd;
        timeController.EndRound();
        feverController.EndRound();
        Round++;

        if (!blockGridManager.AdvanceGrid(Round))
        {
            TriggerGameOver();
            return;
        }

        BeginGame();
    }

    public void NotifyBlockHit(BrickBlock block, Vector2 hitPoint)
    {
        hitEffectPool?.Play(hitPoint, block != null ? block.VisualColor : Color.white);

        bool wasFeverActive = feverController != null && feverController.IsFeverActive;
        feverController?.RegisterHit();

        if (!wasFeverActive && feverController != null && feverController.IsFeverActive)
            audioManager?.PlayFeverStart();
    }

    public void NotifyBlockDestroyed(BrickBlock block)
    {
        audioManager?.PlayBlockDestroy();
    }

    public void NotifyBonusBallCollected()
    {
        audioManager?.PlayBonusBall();
        gameHud?.Refresh(Round, ballManager.PermanentBallCount);
    }

    public void TriggerGameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        State = GameState.GameOver;
        timeController.SetGameOver();
        ballManager.StopAllBalls();
        feverController.EndRound();
        audioManager?.PlayGameOver();
        gameHud?.SetPauseVisible(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TogglePause()
    {
        if (gameOver)
            return;

        if (State == GameState.Pause)
        {
            State = stateBeforePause;
            timeController.SetPaused(false);
            gameHud?.SetPaused(false);
            return;
        }

        if (State != GameState.Aiming && State != GameState.Playing)
            return;

        stateBeforePause = State;
        State = GameState.Pause;
        timeController.SetPaused(true);
        gameHud?.SetPaused(true);
    }

    private void PrepareStartScreen()
    {
        gameOver = false;
        State = GameState.Start;
        stateBeforePause = GameState.Aiming;
        timeController.BeginRound();
        feverController.BeginRound();
        ballManager.PrepareForRound();
        gameHud?.Refresh(Round, ballManager.PermanentBallCount);
        gameHud?.ShowStartScreen();
    }

    private void ResolveSceneReferences()
    {
        if (ball == null)
            ball = FindFirstObjectByType<BallScript>();

        if (brickTemplate == null)
            brickTemplate = FindFirstObjectByType<BrickBlock>();

        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");

        if (restartButton == null && gameOverPanel != null)
            restartButton = gameOverPanel.GetComponentInChildren<Button>(true);
    }
}
