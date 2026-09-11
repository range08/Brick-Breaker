using System.Collections.Generic;
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
/// Coordinates the current prototype loop. Board ownership is moved into
/// BlockGridManager in the next phase; this phase focuses on volley lifecycle.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private BallScript ball;
    [SerializeField] private BrickBlock brickTemplate;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Temporary Board Settings")]
    [SerializeField, Range(3, 8)] private int columns = 7;
    [SerializeField, Range(2, 7)] private int rows = 5;
    [SerializeField, Min(0.1f)] private float horizontalSpacing = 1.2f;
    [SerializeField, Min(0.1f)] private float verticalSpacing = 1.2f;
    [SerializeField] private Vector2 boardOrigin = new(-3.75f, 5.3f);

    private readonly List<BrickBlock> activeBricks = new();
    private BallManager ballManager;
    private bool gameOver;

    public GameState State { get; private set; } = GameState.Start;
    public int Round { get; private set; } = 1;
    public bool CanAcceptAim => !gameOver && State == GameState.Aiming;
    public BallManager BallManager => ballManager;

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

        if (ballManager == null)
            ballManager = gameObject.AddComponent<BallManager>();
    }

    private void Start()
    {
        ResolveSceneReferences();
        BuildBoard();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        ballManager.Initialize(this, ball);
        BeginGame();
    }

    public void BeginGame()
    {
        if (gameOver)
            return;

        State = GameState.Aiming;
        ballManager.PrepareForRound();
    }

    public void NotifyBallLaunched(BallScript launchedBall)
    {
        if (gameOver)
            return;

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
    }

    // Compatibility hooks removed with the old score system in Phase 3.
    public void NotifyBlockDamaged()
    {
    }

    public void NotifyBlockDestroyed(BrickBlock block)
    {
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

    private void BuildBoard()
    {
        if (brickTemplate == null)
            return;

        brickTemplate.gameObject.SetActive(false);
        activeBricks.Clear();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                BrickBlock block = Instantiate(brickTemplate, transform);
                block.gameObject.name = $"Brick_{row + 1}_{column + 1}";
                block.transform.position = new Vector3(
                    boardOrigin.x + column * horizontalSpacing,
                    boardOrigin.y - row * verticalSpacing,
                    0f);
                block.gameObject.SetActive(true);
                block.Configure(row + 1, this);
                activeBricks.Add(block);
            }
        }
    }
}
