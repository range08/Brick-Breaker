using TMPro;
using UnityEngine;

/// <summary>
/// A numbered block that loses one hit point each time the ball collides with it.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BrickBlock : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int hitPoints = 1;

    [SerializeField]
    private TMP_Text hitPointsText;

    private SpriteRenderer spriteRenderer;
    private GameManager gameManager;

    public int HitPoints => hitPoints;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (hitPointsText == null)
            hitPointsText = GetComponentInChildren<TMP_Text>(true);

        RefreshVisual();
    }

    public void Configure(int newHitPoints, GameManager manager)
    {
        hitPoints = Mathf.Max(1, newHitPoints);
        gameManager = manager;
        RefreshVisual();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<BallScript>() != null)
            TakeHit();
    }

    private void TakeHit()
    {
        hitPoints--;

        if (hitPoints <= 0)
        {
            if (gameManager != null)
                gameManager.NotifyBlockDestroyed(this);

            Destroy(gameObject);
            return;
        }

        RefreshVisual();

        if (gameManager != null)
            gameManager.NotifyBlockDamaged();
    }

    private void RefreshVisual()
    {
        if (hitPointsText != null)
            hitPointsText.text = hitPoints.ToString();

        if (spriteRenderer == null)
            return;

        spriteRenderer.color = hitPoints switch
        {
            <= 1 => new Color(0.18f, 0.75f, 0.95f),
            2 => new Color(0.48f, 0.42f, 0.95f),
            3 => new Color(0.82f, 0.35f, 0.88f),
            4 => new Color(0.98f, 0.42f, 0.32f),
            _ => new Color(0.98f, 0.68f, 0.22f)
        };
    }
}
