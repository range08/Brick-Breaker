using UnityEngine;

/// <summary>
/// Trigger below the play field. It ends the current ball flight without
/// physically bouncing the ball back into the field.
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
        BallScript ball = other.GetComponent<BallScript>();

        if (ball != null && GameManager.Instance != null)
            GameManager.Instance.NotifyBallLost(ball);
    }
}
