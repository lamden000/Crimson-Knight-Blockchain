using UnityEngine;
using System.Collections;

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

    // Camera shake
    private Vector3 shakeOffset = Vector3.zero;
    private Coroutine shakeCoroutine;

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
            transform.position = new Vector3(target.position.x , target.position.y, transform.position.z) + shakeOffset;
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

        // Áp dụng camera shake offset
        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z) + shakeOffset;
    }

    /// <summary>
    /// Rung camera với độ mạnh và thời gian
    /// </summary>
    /// <param name="duration">Thời gian rung (giây)</param>
    /// <param name="magnitude">Độ mạnh rung</param>
    public void ShakeCamera(float duration = 0.2f, float magnitude = 0.1f)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Tạo offset ngẫu nhiên
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Lưu offset để áp dụng trong LateUpdate
            shakeOffset = new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset offset về 0
        shakeOffset = Vector3.zero;
        shakeCoroutine = null;
    }
}