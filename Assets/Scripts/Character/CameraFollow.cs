using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    private BoxCollider2D bounds;
    private float halfHeight;
    private float halfWidth;
    private float orthographicSize;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Camera cam;
    private Vector3 lastBoundsCenter;
    private Vector3 lastBoundsSize;
    private bool immediateSnap = false;

    public void SetOrthographicSize(float orthographicSize)
    {
        this.orthographicSize = orthographicSize;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void InitializeBounds()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        bounds= GameObject.FindGameObjectWithTag("Map Boundary")?.GetComponent<BoxCollider2D>();
        halfHeight =orthographicSize;

        halfWidth = halfHeight * cam.aspect;

        if (bounds != null)
        {
            minBounds = bounds.bounds.min;
            maxBounds = bounds.bounds.max;
        }
        else
        {
            Debug.LogWarning("CameraFollow: Bounds collider is not set!");
        }
        SnapToTarget();
    }

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x , target.position.y, transform.position.z);
    }

    // Snap once and tell LateUpdate to skip lerp for the upcoming frame
    public void SnapToTargetImmediate()
    {
        SnapToTarget();
        immediateSnap = true;
    }

    private void LateUpdate()
    {
        if (target == null || bounds == null) return;

        if (immediateSnap)
        {
            // perform one-frame immediate snap and skip lerp
            immediateSnap = false;
            transform.position = new Vector3(target.position.x , target.position.y, transform.position.z);
            return;
        }

        if (bounds.bounds.center != lastBoundsCenter || bounds.bounds.size != lastBoundsSize)
        {
            minBounds = bounds.bounds.min;
            maxBounds = bounds.bounds.max;
            lastBoundsCenter = bounds.bounds.center;
            lastBoundsSize = bounds.bounds.size;
        }

        Vector3 targetPos = Vector3.Lerp(transform.position, target.position, smoothSpeed * Time.deltaTime);

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
    }

}