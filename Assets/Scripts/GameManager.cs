using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the small one-ball game loop: board creation, score, turns, lives,
/// UI updates and restart handling.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private BallScript ball;
    [SerializeField] private BrickBlock brickTemplate;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Board")]
    [SerializeField, Range(3, 8)] private int columns = 7;
    [SerializeField, Range(2, 7)] private int rows = 5;
    [SerializeField, Min(0.1f)] private float horizontalSpacing = 1.25f;
    [SerializeField, Min(0.1f)] private float verticalSpacing = 0.82f;
    [SerializeField] private Vector2 boardOrigin = new Vector2(-3.75f, 5.3f);
    [SerializeField, Min(1)] private int startingLives = 3;

    private readonly List<BrickBlock> activeBricks = new();
    private int score;
    private int turn = 1;
    private int lives;
    private int stage = 1;
    private bool gameOver;

    public int Score => score;
    public int Turn => turn;
    public int Lives => lives;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Application.targetFrameRate = 60;
        lives = startingLives;
    }

    private void Start()
    {
        ResolveSceneReferences();
        BuildBoard();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateHud();

        if (ball != null)
            ball.PrepareForLaunch();
    }

    private void ResolveSceneReferences()
    {
        if (ball == null)
            ball = FindFirstObjectByType<BallScript>();

        if (brickTemplate == null)
            brickTemplate = FindFirstObjectByType<BrickBlock>();

        scoreText = scoreText != null ? scoreText : FindText("ScoreText");
        turnText = turnText != null ? turnText : FindText("TurnText");
        livesText = livesText != null ? livesText : FindText("LivesText");
        statusText = statusText != null ? statusText : FindText("StatusText");

        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");

        if (restartButton == null && gameOverPanel != null)
            restartButton = gameOverPanel.GetComponentInChildren<Button>(true);
    }

    private static TMP_Text FindText(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
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

                int hitPoints = 1 + ((row + stage - 1) % 4);
                block.Configure(hitPoints, this);
                activeBricks.Add(block);
            }
        }
    }

    public void NotifyBallLaunched()
    {
        if (statusText != null)
            statusText.text = "공이 진행 중입니다";
    }

    public void NotifyBallLost(BallScript lostBall)
    {
        if (gameOver || lostBall == null || !lostBall.IsLaunched)
            return;

        lives--;

        if (lives <= 0)
        {
            gameOver = true;
            lostBall.PrepareForLaunch();
            lostBall.gameObject.SetActive(false);

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (statusText != null)
                statusText.text = "게임 오버";

            UpdateHud();
            return;
        }

        turn++;
        lostBall.PrepareForLaunch();

        if (statusText != null)
            statusText.text = "터치 후 위로 드래그하여 발사";

        UpdateHud();
    }

    public void NotifyBlockDamaged()
    {
        score += 5;
        UpdateHud();
    }

    public void NotifyBlockDestroyed(BrickBlock block)
    {
        score += 25;
        activeBricks.Remove(block);
        UpdateHud();

        if (activeBricks.Count == 0)
            AdvanceStage();
    }

    private void AdvanceStage()
    {
        stage++;
        turn++;
        BuildBoard();

        if (ball != null)
            ball.PrepareForLaunch();

        if (statusText != null)
            statusText.text = $"STAGE {stage}  ·  터치 후 위로 드래그하여 발사";

        UpdateHud();
    }

    private void UpdateHud()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE  {score:0000}";

        if (turnText != null)
            turnText.text = $"TURN  {turn:00}  ·  STAGE  {stage:00}";

        if (livesText != null)
            livesText.text = $"LIVES  {Mathf.Max(0, lives)}";
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
