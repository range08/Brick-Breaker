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
            <= 1 => new Color32(0x22, 0xC5, 0x5E, 0xFF),
            2 => new Color32(0x16, 0xA3, 0x4A, 0xFF),
            3 => new Color32(0x15, 0x80, 0x3D, 0xFF),
            4 => new Color32(0x16, 0x65, 0x34, 0xFF),
            _ => new Color32(0x14, 0x53, 0x2D, 0xFF)
        };
    }
}
