using TMPro;
using UnityEngine;

/// <summary>
/// A block that reports damage and destruction to BlockGridManager.
/// HP presentation and bonus behavior are extended in the next phase.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BrickBlock : MonoBehaviour
{
    [SerializeField, Min(1)] private int hitPoints = 1;
    [SerializeField] private TMP_Text hitPointsText;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private BlockGridManager blockGridManager;

    public int HitPoints => hitPoints;
    public Bounds WorldBounds => boxCollider != null ? boxCollider.bounds : new Bounds(transform.position, Vector3.zero);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (hitPointsText == null)
            hitPointsText = GetComponentInChildren<TMP_Text>(true);

        RefreshVisual();
    }

    public void Configure(int newHitPoints, BlockGridManager manager)
    {
        hitPoints = Mathf.Max(1, newHitPoints);
        blockGridManager = manager;
        RefreshVisual();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent<BallScript>(out _))
            TakeHit();
    }

    private void TakeHit()
    {
        hitPoints--;

        if (hitPoints <= 0)
        {
            if (blockGridManager != null)
                blockGridManager.NotifyBlockDestroyed(this);

            Destroy(gameObject);
            return;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (hitPointsText != null)
        {
            hitPointsText.text = hitPoints.ToString();
            hitPointsText.alignment = TextAlignmentOptions.Center;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = new Color32(0x22, 0xC5, 0x5E, 0xFF);
    }
}
