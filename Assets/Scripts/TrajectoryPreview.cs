using UnityEngine;

/// <summary>
/// Draws a short, allocation-free multi-bounce trajectory using the same
/// CircleCast shape as the playable ball.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreview : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField, Range(1, 3)] private int maxBounces = 3;
    [SerializeField, Min(1f)] private float maxDistance = 30f;
    [SerializeField, Min(0.001f)] private float castSkin = 0.02f;
    [SerializeField] private LayerMask collisionMask = Physics2D.DefaultRaycastLayers;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[12];
    private readonly Vector3[] linePoints = new Vector3[4];
    private LineRenderer lineRenderer;
    private ContactFilter2D contactFilter;

    public bool IsVisible => lineRenderer != null && lineRenderer.enabled;
    public int PointCount => lineRenderer != null ? lineRenderer.positionCount : 0;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true
        };
        contactFilter.SetLayerMask(collisionMask);

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.widthMultiplier = 0.035f;
        lineRenderer.numCapVertices = 2;
        lineRenderer.startColor = new Color(0.15f, 0.18f, 0.25f, 0.75f);
        lineRenderer.endColor = new Color(0.15f, 0.18f, 0.25f, 0.15f);
        lineRenderer.sortingOrder = 20;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
                lineRenderer.sharedMaterial = new Material(shader);
        }

        Clear();
    }

    public void Draw(Vector2 origin, Vector2 direction, float radius)
    {
        if (lineRenderer == null || direction.sqrMagnitude < 0.001f)
        {
            Clear();
            return;
        }

        direction.Normalize();
        radius = Mathf.Max(0.01f, radius);

        int bounceLimit = Mathf.Clamp(maxBounces, 1, 3);
        int pointCount = 1;
        linePoints[0] = origin;

        Vector2 castOrigin = origin;
        Vector2 castDirection = direction;
        float remainingDistance = maxDistance;

        for (int bounce = 0; bounce < bounceLimit && remainingDistance > castSkin; bounce++)
        {
            int hitCount = Physics2D.CircleCast(castOrigin, radius, castDirection, contactFilter, castHits, remainingDistance);

            if (!TryGetNearestValidHit(hitCount, out RaycastHit2D hit))
            {
                linePoints[pointCount++] = castOrigin + castDirection * remainingDistance;
                break;
            }

            linePoints[pointCount++] = hit.point;
            remainingDistance -= hit.distance;

            if (hit.collider.TryGetComponent<DeathZone>(out _))
                break;

            castOrigin = hit.centroid + hit.normal * castSkin;
            castDirection = Vector2.Reflect(castDirection, hit.normal).normalized;
            remainingDistance = Mathf.Max(0f, remainingDistance - castSkin);
        }

        lineRenderer.positionCount = pointCount;
        lineRenderer.SetPositions(linePoints);
        lineRenderer.enabled = true;
    }

    public void Clear()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }

    private bool TryGetNearestValidHit(int hitCount, out RaycastHit2D nearestHit)
    {
        nearestHit = default;
        float nearestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];

            if (hit.collider == null || hit.collider.TryGetComponent<BallScript>(out _))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                found = true;
            }
        }

        return found;
    }
}
