using UnityEngine;
using UnityEngine.Serialization;

public class WallLayoutController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BlockGridManager blockGridManager;
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private Transform topWall;
    [FormerlySerializedAs("bottomWall")]
    [SerializeField] private Transform deathZone;

    [Header("Wall Settings")]
    [SerializeField] private float wallThickness = 0.3f;

    [Tooltip("Distance from the outermost brick edge to the wall's inner face.")]
    [SerializeField, Min(0f)] private float sideBoundaryPadding = 0.08f;

    [SerializeField, Min(0.1f)] private float deathZoneHeight = 1.2f;

    private float lastCameraSize;
    private float lastAspect;
    private Vector3 lastCameraPosition;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (blockGridManager == null)
            blockGridManager = FindFirstObjectByType<BlockGridManager>();

        if (deathZone == null)
        {
            GameObject deathZoneObject = GameObject.Find("DeathZone");
            if (deathZoneObject != null)
                deathZone = deathZoneObject.transform;
        }
    }

    private void Start()
    {
        LayoutWalls();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        // 카메라 크기 / 화면비 / 위치 중 하나라도 바뀌면 재배치
        if (!Mathf.Approximately(lastCameraSize, targetCamera.orthographicSize) ||
            !Mathf.Approximately(lastAspect, targetCamera.aspect) ||
            lastCameraPosition != targetCamera.transform.position)
        {
            LayoutWalls();
        }
    }

    private void LayoutWalls()
    {
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        Vector3 camPos = targetCamera.transform.position;

        float left = blockGridManager != null
            ? blockGridManager.GetLeftGameplayBoundary(sideBoundaryPadding)
            : camPos.x - halfWidth;
        float right = blockGridManager != null
            ? blockGridManager.GetRightGameplayBoundary(sideBoundaryPadding)
            : camPos.x + halfWidth;
        float top = camPos.y + halfHeight;
        float bottom = camPos.y - halfHeight;

        // 벽의 절반 정도를 화면 밖으로 내보냄
        if (leftWall != null)
            leftWall.position = new Vector3(left - wallThickness / 2f, camPos.y, 0f);

        if (rightWall != null)
            rightWall.position = new Vector3(right + wallThickness / 2f, camPos.y, 0f);

        if (topWall != null)
            topWall.position = new Vector3(camPos.x, top + wallThickness / 2f, 0f);

        if (deathZone == null)
            return;

        deathZone.position = new Vector3(
            camPos.x,
            bottom - deathZoneHeight / 2f,
            0f
        );

        float width = halfWidth * 2f;
        float height = halfHeight * 2f;

        if (leftWall != null)
            leftWall.localScale = new Vector3(wallThickness, height, 1f);

        if (rightWall != null)
            rightWall.localScale = new Vector3(wallThickness, height, 1f);

        if (topWall != null)
            topWall.localScale = new Vector3(width, wallThickness, 1f);

        deathZone.localScale = new Vector3(width + wallThickness * 2f, deathZoneHeight, 1f);

        lastCameraSize = targetCamera.orthographicSize;
        lastAspect = targetCamera.aspect;
        lastCameraPosition = targetCamera.transform.position;
    }
}
