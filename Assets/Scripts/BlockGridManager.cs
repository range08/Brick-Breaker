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
    [SerializeField, Range(2, 7)] private int startingRows = 4;
    [SerializeField, Min(0.1f)] private float horizontalSpacing = 1.2f;
    [SerializeField, Min(0.1f)] private float verticalSpacing = 1.2f;
    [SerializeField] private Vector2 boardOrigin = new(-3.75f, 4.7f);
    [SerializeField] private bool centerGridOnCamera = true;

    [Header("Row Generation")]
    [SerializeField, Range(0.1f, 1f)] private float rowOccupancy = 0.78f;
    [SerializeField, Range(1, 7)] private int minimumBlocksPerRow = 5;

    [Header("HP Generation")]
    [SerializeField, Min(1)] private int minimumHpAtRoundOne = 1;
    [SerializeField, Range(0f, 1f)] private float hpGrowthPerRound = 0.5f;
    [SerializeField, Range(0, 3)] private int hpVariance = 2;
    [SerializeField, Min(1)] private int maxGeneratedHp = 12;

    [Header("Bonus Ball")]
    [SerializeField, Range(0f, 1f)] private float bonusRowChance = 0.25f;

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
    public int Columns => columns;
    public float HorizontalSpacing => horizontalSpacing;
    public float RowOccupancy => rowOccupancy;
    public int MinimumBlocksPerRow => minimumBlocksPerRow;
    public float HpGrowthPerRound => hpGrowthPerRound;
    public float BonusRowChance => bonusRowChance;
    public float BrickWidth => GetWorldBrickWidth();
    public float GridOuterLeft => GetGridStartX() - GetWorldBrickHalfWidth();
    public float GridOuterRight => GetGridStartX() + (columns - 1) * horizontalSpacing + GetWorldBrickHalfWidth();

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
        float t = Mathf.InverseLerp(1f, Mathf.Max(1, maxGeneratedHp), hitPoints);
        Color lowHp = new Color32(0x52, 0xD9, 0x7B, 0xFF);
        Color highHp = new Color32(0x0B, 0x6B, 0x3A, 0xFF);
        return Color.Lerp(lowHp, highHp, t);
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
        List<int> selectedColumns = GetSelectedColumns();
        int bonusColumn = Random.value < bonusRowChance
            ? selectedColumns[Random.Range(0, selectedColumns.Count)]
            : -1;

        for (int i = 0; i < selectedColumns.Count; i++)
        {
            int column = selectedColumns[i];
            float x = GetGridStartX() + column * horizontalSpacing;
            float y = boardOrigin.y - row * verticalSpacing;
            bool isBonus = column == bonusColumn;
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

    private List<int> GetSelectedColumns()
    {
        int safeColumnCount = Mathf.Max(1, columns);
        int minimumCount = Mathf.Clamp(minimumBlocksPerRow, 1, safeColumnCount);
        int maximumCount = Mathf.Max(minimumCount, safeColumnCount - 1);
        float exactCount = safeColumnCount * Mathf.Clamp01(rowOccupancy);
        int lowerCount = Mathf.FloorToInt(exactCount);
        int upperCount = Mathf.CeilToInt(exactCount);
        int targetCount = lowerCount == upperCount
            ? lowerCount
            : Random.value < exactCount - lowerCount ? upperCount : lowerCount;
        targetCount = Mathf.Clamp(targetCount, minimumCount, maximumCount);

        List<int> availableColumns = new(safeColumnCount);

        for (int column = 0; column < safeColumnCount; column++)
            availableColumns.Add(column);

        for (int i = 0; i < targetCount; i++)
        {
            int swapIndex = Random.Range(i, availableColumns.Count);
            (availableColumns[i], availableColumns[swapIndex]) =
                (availableColumns[swapIndex], availableColumns[i]);
        }

        if (availableColumns.Count > targetCount)
            availableColumns.RemoveRange(targetCount, availableColumns.Count - targetCount);

        return availableColumns;
    }

    private int GetMinimumHpForCell(float x, float y, int round)
    {
        // Do not carry HP upward from the row below. That made a fresh
        // five-row board ramp from 1 HP to 5+ HP before the first volley.
        // Round progression now controls the floor and hpVariance supplies
        // the small amount of local variety.
        return GetRoundMinimumHp(round);
    }

    public float GetGridStartX()
    {
        float totalWidth = Mathf.Max(0, columns - 1) * horizontalSpacing;
        float centerX = centerGridOnCamera && targetCamera != null
            ? targetCamera.transform.position.x
            : boardOrigin.x + totalWidth * 0.5f;
        return centerX - totalWidth * 0.5f;
    }

    public float GetLeftGameplayBoundary(float padding)
    {
        return GridOuterLeft - Mathf.Max(0f, padding);
    }

    public float GetRightGameplayBoundary(float padding)
    {
        return GridOuterRight + Mathf.Max(0f, padding);
    }

    private float GetWorldBrickWidth()
    {
        if (brickTemplate == null)
            return 1f;

        BoxCollider2D collider = brickTemplate.GetComponent<BoxCollider2D>();
        Vector3 scale = brickTemplate.transform.lossyScale;
        float localWidth = collider != null ? collider.size.x : 1f;
        return Mathf.Abs(localWidth * scale.x);
    }

    private float GetWorldBrickHalfWidth()
    {
        return GetWorldBrickWidth() * 0.5f;
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
