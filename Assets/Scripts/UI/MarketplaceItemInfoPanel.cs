using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel hiển thị thông tin chi tiết của item trong marketplace khi click vào
/// Khác với ItemInfoPanel - chỉ dùng cho marketplace với Buy button và Price
/// </summary>
public class MarketplaceItemInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel; // Panel chứa toàn bộ info
    [SerializeField] private Image iconImage; // Image để hiển thị icon
    [SerializeField] private TextMeshProUGUI nameText; // Text để hiển thị tên item
    [SerializeField] private TextMeshProUGUI descriptionText; // Text để hiển thị description
    [SerializeField] private TextMeshProUGUI priceText; // Text để hiển thị price
    [SerializeField] private Button buyButton; // Button để mua item
    [SerializeField] private TextMeshProUGUI buyButtonText; // Text component của buyButton

    private string currentTokenId;
    private ItemData currentItemData;
    private string currentPrice;
    private string currentSellerAddress;
    private bool isOwnItem = false; // Flag để biết item có phải của chính người chơi không

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

        if (priceText == null)
        {
            Transform priceTransform = transform.Find("PriceText");
            if (priceTransform != null)
            {
                priceText = priceTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (buyButton == null)
        {
            Transform buyTransform = transform.Find("BuyButton");
            if (buyTransform != null)
            {
                buyButton = buyTransform.GetComponent<Button>();
            }
        }

        // Tự động tìm buyButtonText
        if (buyButton != null && buyButtonText == null)
        {
            buyButtonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Setup buy button click
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    private void Start()
    {
        // Ẩn panel lúc đầu
        Hide();
    }

    /// <summary>
    /// Hiển thị thông tin marketplace item
    /// </summary>
    /// <param name="itemData">ItemData của item</param>
    /// <param name="tokenId">Token ID của item trên blockchain</param>
    /// <param name="price">Price của item (string format, ví dụ: "10.50")</param>
    /// <param name="sellerAddress">Địa chỉ của người bán</param>
    public void ShowItemInfo(ItemData itemData, string tokenId, string price, string sellerAddress = "")
    {
        if (itemData == null)
        {
            Debug.LogWarning("[MarketplaceItemInfoPanel] Cannot show info for null item!");
            return;
        }

        currentItemData = itemData;
        currentTokenId = tokenId;
        currentPrice = price;
        currentSellerAddress = sellerAddress;

        // Hiển thị panel
        if (panel != null)
        {
            panel.SetActive(true);
        }

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

        // Update price
        if (priceText != null)
        {
            priceText.text = $"Price: {price} GTK";
        }

        // Update buy button
        // Kiểm tra nếu item là của chính người chơi (seller address trùng với wallet address)
        isOwnItem = false;
        if (!string.IsNullOrEmpty(currentSellerAddress))
        {
            string playerWalletAddress = InventoryManager.Instance?.GetWalletAddress();
            if (!string.IsNullOrEmpty(playerWalletAddress))
            {
                // So sánh address (case-insensitive, có thể có checksum)
                isOwnItem = currentSellerAddress.Equals(playerWalletAddress, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (buyButton != null)
        {
            buyButton.interactable = true; // Luôn enable (có thể là Buy hoặc Cancel)
            
            // Đổi màu button thành đỏ nếu là item của chính mình
            if (isOwnItem)
            {
                var colors = buyButton.colors;
                colors.normalColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Đỏ
                colors.highlightedColor = new Color(0.8f, 0.15f, 0.15f, 1f);
                colors.pressedColor = new Color(0.7f, 0.1f, 0.1f, 1f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                colors.selectedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
                buyButton.colors = colors;
            }
            else
            {
                // Reset về màu mặc định (xanh/tím)
                var colors = buyButton.colors;
                colors.normalColor = new Color(0.4f, 0.5f, 0.9f, 1f); // Xanh/tím
                colors.highlightedColor = new Color(0.35f, 0.45f, 0.85f, 1f);
                colors.pressedColor = new Color(0.3f, 0.4f, 0.8f, 1f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                colors.selectedColor = new Color(0.4f, 0.5f, 0.9f, 1f);
                buyButton.colors = colors;
            }
        }

        if (buyButtonText != null)
        {
            if (isOwnItem)
            {
                buyButtonText.text = "Hủy bán";
            }
            else
            {
                buyButtonText.text = "Buy";
            }
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
    /// Xử lý khi click Buy button (hoặc Cancel button nếu là item của chính mình)
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (string.IsNullOrEmpty(currentTokenId))
        {
            Debug.LogError("[MarketplaceItemInfoPanel] Cannot process: tokenId is null or empty!");
            return;
        }

        if (MarketplaceManager.Instance == null)
        {
            Debug.LogError("[MarketplaceItemInfoPanel] MarketplaceManager.Instance is null!");
            return;
        }

        // Disable button để tránh click nhiều lần
        if (buyButton != null)
        {
            buyButton.interactable = false;
        }

        if (buyButtonText != null)
        {
            buyButtonText.text = "Processing...";
        }

        // Nếu là item của chính mình → hủy listing
        if (isOwnItem)
        {
            // Gọi MarketplaceManager để hủy listing
            MarketplaceManager.Instance.CancelListing(currentTokenId);
            
            // Ẩn panel sau khi mở trang web
            Hide();
            
            // Refresh marketplace UI sau một chút (chuyển sang MarketplaceUI để tránh lỗi khi GameObject inactive)
            if (MarketplaceUI.Instance != null)
            {
                MarketplaceUI.Instance.StartCoroutine(MarketplaceUI.Instance.DelayedRefreshMarketplace());
            }
        }
        else
        {
            // Mua item (logic cũ)
            // Lấy price từ MarketplaceDataManager
            if (MarketplaceDataManager.Instance == null)
            {
                OnBuyFailed("MarketplaceDataManager chưa được khởi tạo!");
                return;
            }

            var listings = MarketplaceDataManager.Instance.GetAllListings();
            if (!listings.ContainsKey(currentTokenId))
            {
                OnBuyFailed("Item không còn trong marketplace!");
                return;
            }

            var listing = listings[currentTokenId];
            string price = listing.priceInGTK.ToString("F2");

            // Gọi MarketplaceManager để mua item
            MarketplaceManager.Instance.BuyItem(currentTokenId, OnBuySuccess, OnBuyFailed);
        }
    }


    /// <summary>
    /// Callback khi mua thành công
    /// </summary>
    private void OnBuySuccess()
    {
        Debug.Log($"[MarketplaceItemInfoPanel] Đã mua item thành công: {currentTokenId}");
        
        // Ẩn panel
        Hide();

        // Refresh marketplace UI nếu có
        if (MarketplaceUI.Instance != null)
        {
            MarketplaceUI.Instance.RefreshMarketplaceUI();
        }

        // Có thể hiển thị thông báo thành công
        // TODO: Có thể thêm notification system
    }

    /// <summary>
    /// Callback khi mua thất bại
    /// </summary>
    private void OnBuyFailed(string error)
    {
        Debug.LogError($"[MarketplaceItemInfoPanel] Mua item thất bại: {error}");

        // Enable lại button
        if (buyButton != null)
        {
            buyButton.interactable = true;
        }

        if (buyButtonText != null)
        {
            buyButtonText.text = "Buy";
        }

        // Có thể hiển thị thông báo lỗi
        // TODO: Có thể thêm notification system
    }
}

