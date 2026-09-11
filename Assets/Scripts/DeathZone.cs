using UnityEngine;

/// <summary>
/// Trigger below the play field. It never bounces a ball; it forwards the
/// return event to that ball's BallManager.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DeathZone : MonoBehaviour
{
    private BoxCollider2D zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out BallScript ball))
            ball.NotifyDeathZone();
    }
}
