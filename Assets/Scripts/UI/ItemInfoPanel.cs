using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel hiển thị thông tin chi tiết của item khi click vào
/// </summary>
public class ItemInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel; // Panel chứa toàn bộ info
    [SerializeField] private Image iconImage; // Image để hiển thị icon
    [SerializeField] private TextMeshProUGUI nameText; // Text để hiển thị tên item
    [SerializeField] private TextMeshProUGUI descriptionText; // Text để hiển thị description
    [SerializeField] private Button withdrawButton; // Button để withdraw item

    private void Awake()
    {
        // Tự động tìm các component nếu chưa được assign
        if (panel == null)
        {
            panel = gameObject;
        }

        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
            else
            {
                iconImage = GetComponentInChildren<Image>();
            }
        }

        if (nameText == null)
        {
            Transform nameTransform = transform.Find("NameText");
            if (nameTransform != null)
            {
                nameText = nameTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (descriptionText == null)
        {
            Transform descTransform = transform.Find("DescriptionText");
            if (descTransform != null)
            {
                descriptionText = descTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (withdrawButton == null)
        {
            Transform withdrawTransform = transform.Find("WithdrawButton");
            if (withdrawTransform != null)
            {
                withdrawButton = withdrawTransform.GetComponent<Button>();
            }
        }

        // Setup withdraw button click
        if (withdrawButton != null)
        {
            withdrawButton.onClick.RemoveAllListeners();
            withdrawButton.onClick.AddListener(OnWithdrawButtonClicked);
        }
    }

    private ItemData currentItemData;

    private void Start()
    {
        // Ẩn panel lúc đầu
        Hide();
    }

    /// <summary>
    /// Hiển thị thông tin item
    /// </summary>
    public void ShowItemInfo(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("[ItemInfoPanel] Cannot show info for null item!");
            return;
        }

        currentItemData = itemData;

        // Update icon
        if (iconImage != null)
        {
            if (itemData.icon != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        // Update name
        if (nameText != null)
        {
            nameText.text = itemData.itemName ?? "Unknown Item";
        }

        // Update description
        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrEmpty(itemData.description) 
                ? "No description available." 
                : itemData.description;
        }

        // Update withdraw button - chỉ enable nếu item withdrawable
        if (withdrawButton != null)
        {
            withdrawButton.interactable = itemData.withdrawable;
        }

        // Hiển thị panel
        Show();
    }

    /// <summary>
    /// Xử lý khi click withdraw button
    /// </summary>
    private void OnWithdrawButtonClicked()
    {
        if (currentItemData == null || !currentItemData.withdrawable)
        {
            return;
        }

        // Lấy số lượng item hiện có
        int currentQuantity = InventoryManager.Instance?.GetItemQuantity(currentItemData.itemID) ?? 0;
        
        if (currentQuantity <= 0)
        {
            Debug.LogWarning("[ItemInfoPanel] Cannot withdraw: no items available!");
            return;
        }

        // Hiển thị withdraw dialog
        if (UIManager.Instance != null && UIManager.Instance.DialogFactory != null)
        {
            var dialog = UIManager.Instance.DialogFactory.CreateWithdrawDialog();
            if (dialog != null)
            {
                dialog.Setup(currentItemData, currentQuantity, OnWithdrawConfirmed);
                dialog.Show();
            }
        }
        else
        {
            Debug.LogError("[ItemInfoPanel] UIManager or DialogFactory not found!");
        }
    }

    /// <summary>
    /// Callback khi withdraw được confirm
    /// </summary>
    private void OnWithdrawConfirmed(int itemID, int quantity, string walletAddress)
    {
        // Xử lý withdraw logic ở đây (sẽ tích hợp blockchain sau)
        Debug.Log($"[ItemInfoPanel] Withdrawing {quantity}x item {itemID} to wallet: {walletAddress}");
        
        // Xóa item khỏi inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(itemID, quantity);
        }
    }

    /// <summary>
    /// Hiển thị panel
    /// </summary>
    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    /// <summary>
    /// Ẩn panel
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle panel (bật/tắt)
    /// </summary>
    public void Toggle()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    /// <summary>
    /// Kiểm tra panel có đang hiển thị không
    /// </summary>
    public bool IsVisible()
    {
        return panel != null && panel.activeSelf;
    }
}

