using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates and advances the block grid. A row is added only after the current
/// volley has completely returned.
/// </summary>
public class BlockGridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BrickBlock brickTemplate;
    [SerializeField] private Camera targetCamera;

    [Header("Grid")]
    [SerializeField, Range(3, 8)] private int columns = 7;
    [SerializeField, Range(2, 7)] private int startingRows = 5;
    [SerializeField, Min(0.1f)] private float horizontalSpacing = 1.2f;
    [SerializeField, Min(0.1f)] private float verticalSpacing = 1.2f;
    [SerializeField] private Vector2 boardOrigin = new(-3.75f, 5.3f);

    [Header("HP Generation")]
    [SerializeField, Min(1)] private int minimumHpAtRoundOne = 1;
    [SerializeField, Range(0f, 1f)] private float hpGrowthPerRound = 0.35f;
    [SerializeField, Range(0, 3)] private int hpVariance = 1;
    [SerializeField, Min(1)] private int maxGeneratedHp = 12;

    [Header("Bonus Ball")]
    [SerializeField, Range(0f, 1f)] private float bonusBlockChance = 0.15f;

    [Header("Dead Line")]
    [Tooltip("Distance above the initial ball launch position used as the block dead line.")]
    [SerializeField, Min(0.1f)] private float deadLinePaddingFromBottom = 1.5f;

    private readonly List<BrickBlock> activeBlocks = new();
    private GameManager gameManager;
    private BallManager ballManager;
    private float initialLaunchY;
    private bool initialLaunchYInitialized;

    public int ActiveBlockCount => activeBlocks.Count;
    public float VerticalSpacing => verticalSpacing;
    public float DeadLineY => GetDeadLineY();
    public IReadOnlyList<BrickBlock> ActiveBlocks => activeBlocks;
    public int HpVariance => hpVariance;

    public void Initialize(GameManager owner, BrickBlock template)
    {
        gameManager = owner;
        ballManager = owner != null ? owner.BallManager : FindFirstObjectByType<BallManager>();
        targetCamera = targetCamera != null ? targetCamera : Camera.main;
        brickTemplate = brickTemplate != null ? brickTemplate : template;

        BallScript launchBall = FindFirstObjectByType<BallScript>();

        if (launchBall != null)
        {
            initialLaunchY = launchBall.transform.position.y;
            initialLaunchYInitialized = true;
        }
    }

    public void BuildInitialGrid(int round)
    {
        if (brickTemplate == null)
            return;

        brickTemplate.gameObject.SetActive(false);
        activeBlocks.Clear();

        for (int row = startingRows - 1; row >= 0; row--)
            CreateRow(row, round);
    }

    public bool AdvanceGrid(int round)
    {
        MoveBlocksDownOneRow();
        Physics2D.SyncTransforms();

        if (IsDeadLineCrossed())
            return false;

        CreateRow(0, round);
        return true;
    }

    public void NotifyBlockHit(BrickBlock block, Vector2 hitPoint)
    {
        if (gameManager != null)
            gameManager.NotifyBlockHit(block, hitPoint);
    }

    public void NotifyBonusCollected(BrickBlock block)
    {
        if (ballManager == null)
            ballManager = FindFirstObjectByType<BallManager>();

        if (ballManager != null)
            ballManager.AddPermanentBall(1);

        if (gameManager != null)
            gameManager.NotifyBonusBallCollected();
    }

    public void NotifyBlockDestroyed(BrickBlock block)
    {
        if (block != null)
            activeBlocks.Remove(block);

        if (gameManager != null)
            gameManager.NotifyBlockDestroyed(block);
    }

    public Color GetHpColor(int hitPoints)
    {
        float ratio = Mathf.Clamp01(hitPoints / (float)Mathf.Max(1, maxGeneratedHp));

        if (ratio <= 0.25f)
            return new Color32(0x20, 0xC9, 0x63, 0xFF);

        if (ratio <= 0.5f)
            return new Color32(0xF2, 0xC9, 0x4C, 0xFF);

        if (ratio <= 0.75f)
            return new Color32(0xF2, 0x8B, 0x3C, 0xFF);

        return new Color32(0xE9, 0x4B, 0x55, 0xFF);
    }

    public bool IsDeadLineCrossed()
    {
        float deadLineY = GetDeadLineY();

        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            BrickBlock block = activeBlocks[i];

            if (block == null)
            {
                activeBlocks.RemoveAt(i);
                continue;
            }

            if (block.WorldBounds.min.y <= deadLineY)
                return true;
        }

        return false;
    }

    private void MoveBlocksDownOneRow()
    {
        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            BrickBlock block = activeBlocks[i];

            if (block == null)
            {
                activeBlocks.RemoveAt(i);
                continue;
            }

            block.transform.position += Vector3.down * verticalSpacing;
        }
    }

    private void CreateRow(int row, int round)
    {
        for (int column = 0; column < columns; column++)
        {
            float x = boardOrigin.x + column * horizontalSpacing;
            float y = boardOrigin.y - row * verticalSpacing;
            bool isBonus = Random.value < bonusBlockChance;
            int minimumHp = GetMinimumHpForCell(x, y, round);
            int hitPoints = isBonus ? 1 : GenerateHp(minimumHp);

            BrickBlock block = Instantiate(brickTemplate, transform);
            block.gameObject.name = $"Brick_R{round}_{row + 1}_{column + 1}";
            block.transform.position = new Vector3(x, y, 0f);
            block.gameObject.SetActive(true);
            block.Configure(hitPoints, this, isBonus);
            activeBlocks.Add(block);
        }
    }

    private int GetMinimumHpForCell(float x, float y, int round)
    {
        int minimumHp = GetRoundMinimumHp(round);
        float nearestBelowY = float.MinValue;
        BrickBlock nearestBelow = null;

        for (int i = 0; i < activeBlocks.Count; i++)
        {
            BrickBlock candidate = activeBlocks[i];

            if (candidate == null || candidate.IsBonus)
                continue;

            Vector3 candidatePosition = candidate.transform.position;

            if (Mathf.Abs(candidatePosition.x - x) > horizontalSpacing * 0.25f || candidatePosition.y >= y)
                continue;

            if (candidatePosition.y > nearestBelowY)
            {
                nearestBelowY = candidatePosition.y;
                nearestBelow = candidate;
            }
        }

        if (nearestBelow != null)
            minimumHp = Mathf.Max(minimumHp, nearestBelow.HitPoints);

        return minimumHp;
    }

    private int GenerateHp(int minimumHp)
    {
        int safeMinimum = Mathf.Max(1, minimumHp);
        int availableVariance = Mathf.Min(hpVariance, Mathf.Max(0, maxGeneratedHp - safeMinimum));
        return Random.Range(safeMinimum, safeMinimum + availableVariance + 1);
    }

    private int GetRoundMinimumHp(int round)
    {
        int growth = Mathf.FloorToInt(Mathf.Max(0, round - 1) * hpGrowthPerRound);
        return Mathf.Clamp(minimumHpAtRoundOne + growth, minimumHpAtRoundOne, maxGeneratedHp);
    }

    private float GetDeadLineY()
    {
        if (!initialLaunchYInitialized)
        {
            if (ballManager == null)
                ballManager = gameManager != null ? gameManager.BallManager : FindFirstObjectByType<BallManager>();

            BallScript launchBall = FindFirstObjectByType<BallScript>();

            if (launchBall != null)
            {
                initialLaunchY = launchBall.transform.position.y;
                initialLaunchYInitialized = true;
            }
            else if (ballManager != null)
            {
                initialLaunchY = ballManager.NextLaunchPosition.y;
                initialLaunchYInitialized = true;
            }
        }

        if (initialLaunchYInitialized)
            return initialLaunchY + deadLinePaddingFromBottom;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return float.MinValue;

        return targetCamera.transform.position.y - targetCamera.orthographicSize + deadLinePaddingFromBottom;
    }
}
