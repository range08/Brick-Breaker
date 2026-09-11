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
    private bool gameOver;

    public GameState State { get; private set; } = GameState.Start;
    public int Round { get; private set; } = 1;
    public bool CanAcceptAim => !gameOver && State == GameState.Aiming;
    public BallManager BallManager => ballManager;
    public BlockGridManager BlockGridManager => blockGridManager;
    public GameHud GameHud => gameHud;
    public TrajectoryPreview TrajectoryPreview => trajectoryPreview;

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

        if (ballManager == null)
            ballManager = gameObject.AddComponent<BallManager>();

        if (blockGridManager == null)
            blockGridManager = gameObject.AddComponent<BlockGridManager>();

        if (gameHud == null)
            gameHud = gameObject.AddComponent<GameHud>();

        if (trajectoryPreview == null)
            trajectoryPreview = gameObject.AddComponent<TrajectoryPreview>();
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
        BeginGame();
    }

    public void BeginGame()
    {
        if (gameOver)
            return;

        State = GameState.Aiming;
        ballManager.PrepareForRound();
        gameHud?.Refresh(Round, ballManager.PermanentBallCount);
    }

    public void NotifyBallLaunched(BallScript launchedBall)
    {
        if (!gameOver)
            State = GameState.Playing;
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
    }

    public void NotifyBonusBallCollected()
    {
        gameHud?.Refresh(Round, ballManager.PermanentBallCount);
    }

    public void TriggerGameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        State = GameState.GameOver;
        ballManager.StopAllBalls();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
