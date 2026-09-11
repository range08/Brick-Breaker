using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A numbered block or a +1 bonus block. Destruction is animated briefly so
/// hits remain readable when many balls are active.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BrickBlock : MonoBehaviour
{
    [SerializeField, Min(1)] private int hitPoints = 1;
    [SerializeField] private TMP_Text hitPointsText;
    [SerializeField, Range(0.1f, 0.3f)] private float destructionDuration = 0.15f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private BlockGridManager blockGridManager;
    private bool isBonus;
    private bool isDestroying;

    public int HitPoints => hitPoints;
    public bool IsBonus => isBonus;
    public Bounds WorldBounds => boxCollider != null ? boxCollider.bounds : new Bounds(transform.position, Vector3.zero);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (hitPointsText == null)
            hitPointsText = GetComponentInChildren<TMP_Text>(true);

        RefreshVisual();
    }

    public void Configure(int newHitPoints, BlockGridManager manager, bool bonus)
    {
        hitPoints = Mathf.Max(1, newHitPoints);
        blockGridManager = manager;
        isBonus = bonus;
        isDestroying = false;
        transform.localScale = Vector3.one;
        RefreshVisual();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroying || !collision.collider.TryGetComponent<BallScript>(out _))
            return;

        Vector2 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        TakeHit(hitPoint);
    }

    private void TakeHit(Vector2 hitPoint)
    {
        if (isBonus)
        {
            isDestroying = true;
            blockGridManager?.NotifyBonusCollected(this);
            blockGridManager?.NotifyBlockDestroyed(this);
            StartCoroutine(PlayDestroyAnimation());
            return;
        }

        hitPoints--;
        blockGridManager?.NotifyBlockHit(this, hitPoint);

        if (hitPoints <= 0)
        {
            isDestroying = true;
            blockGridManager?.NotifyBlockDestroyed(this);
            StartCoroutine(PlayDestroyAnimation());
            return;
        }

        RefreshVisual();
    }

    private IEnumerator PlayDestroyAnimation()
    {
        if (boxCollider != null)
            boxCollider.enabled = false;

        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < destructionDuration)
        {
            float normalized = elapsed / destructionDuration;
            float scale;

            if (normalized < 0.35f)
                scale = Mathf.Lerp(1f, 1.15f, normalized / 0.35f);
            else
                scale = Mathf.Lerp(1.15f, 0f, (normalized - 0.35f) / 0.65f);

            transform.localScale = initialScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void RefreshVisual()
    {
        if (hitPointsText != null)
        {
            hitPointsText.text = isBonus ? "+1" : hitPoints.ToString();
            hitPointsText.alignment = TextAlignmentOptions.Center;
        }

        if (spriteRenderer == null)
            return;

        spriteRenderer.color = isBonus
            ? new Color32(0xF5, 0xB9, 0x28, 0xFF)
            : blockGridManager != null
                ? blockGridManager.GetHpColor(hitPoints)
                : new Color32(0x20, 0xC9, 0x63, 0xFF);
    }
}
