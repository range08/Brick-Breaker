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

    [Header("Dead Line")]
    [Tooltip("Distance above the camera bottom used as the block dead line.")]
    [SerializeField, Min(0.1f)] private float deadLinePaddingFromBottom = 1.5f;

    private readonly List<BrickBlock> activeBlocks = new();
    private GameManager gameManager;

    public int ActiveBlockCount => activeBlocks.Count;
    public float VerticalSpacing => verticalSpacing;
    public float DeadLineY => GetDeadLineY();
    public IReadOnlyList<BrickBlock> ActiveBlocks => activeBlocks;

    public void Initialize(GameManager owner, BrickBlock template)
    {
        gameManager = owner;
        targetCamera = targetCamera != null ? targetCamera : Camera.main;
        brickTemplate = brickTemplate != null ? brickTemplate : template;
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

    public void NotifyBlockDestroyed(BrickBlock block)
    {
        if (block != null)
            activeBlocks.Remove(block);
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
            BrickBlock block = Instantiate(brickTemplate, transform);
            block.gameObject.name = $"Brick_R{round}_{column + 1}";
            block.transform.position = new Vector3(
                boardOrigin.x + column * horizontalSpacing,
                boardOrigin.y - row * verticalSpacing,
                0f);
            block.gameObject.SetActive(true);
            block.Configure(row + 1, this);
            activeBlocks.Add(block);
        }
    }

    private float GetDeadLineY()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return float.MinValue;

        return targetCamera.transform.position.y - targetCamera.orthographicSize + deadLinePaddingFromBottom;
    }
}
