using UnityEngine;

public class TargetIndicatorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject arrowIndicator; // GameObject chứa mũi tên (sprite hoặc image)
    [SerializeField] private float offsetY = 50f; // Offset Y để mũi tên hiển thị phía trên target

    private Transform currentTarget;
    private Camera mainCamera;
    private RectTransform canvasRect;
    private RectTransform arrowRect;

    public static TargetIndicatorUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        
        // Tìm Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        // Lấy RectTransform của arrow indicator
        if (arrowIndicator != null)
        {
            arrowRect = arrowIndicator.GetComponent<RectTransform>();
            arrowIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateArrowPosition();
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;

        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(target != null);
        }
    }

    private void UpdateArrowPosition()
    {
        if (currentTarget == null || arrowIndicator == null || mainCamera == null || canvasRect == null)
        {
            if (arrowIndicator != null)
                arrowIndicator.SetActive(false);
            return;
        }

        // Convert world position to screen position
        Vector3 worldPos = currentTarget.position + new Vector3(0, offsetY, 0);
        Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPos);

        // Convert screen position to canvas position
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out canvasPos
        );

        // Set arrow position
        if (arrowRect != null)
        {
            arrowRect.anchoredPosition = canvasPos;
        }
    }
}

