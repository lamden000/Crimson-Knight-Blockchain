using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI để hiển thị marketplace - items đang được bán
/// </summary>
public class MarketplaceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject marketplacePanel; // Panel chứa toàn bộ marketplace UI
    [SerializeField] private ScrollRect scrollView; // ScrollView để hiển thị items
    [SerializeField] private Transform contentContainer; // Container trong ScrollView để chứa item slots
    [SerializeField] private GameObject marketplaceSlotPrefab; // Prefab cho mỗi marketplace item slot
    [SerializeField] private ItemInfoPanel itemInfoPanel; // Panel hiển thị thông tin item (dùng chung)
    [SerializeField] private Button refreshButton; // Button để refresh marketplace
    [SerializeField] private TextMeshProUGUI statusText; // Text hiển thị trạng thái
    
    private Dictionary<string, GameObject> marketplaceSlotInstances = new Dictionary<string, GameObject>(); // Key: tokenId

    // Static instance
    public static MarketplaceUI Instance { get; private set; }

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
        // Ẩn marketplace panel lúc đầu
        if (marketplacePanel != null)
        {
            marketplacePanel.SetActive(false);
        }

        // Tự động tìm ItemInfoPanel nếu chưa được assign
        if (itemInfoPanel == null)
        {
            itemInfoPanel = FindAnyObjectByType<ItemInfoPanel>();
        }

        // Setup refresh button
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        // Đăng ký event từ MarketplaceDataManager
        StartCoroutine(WaitForMarketplaceDataManager());
    }

    /// <summary>
    /// Đợi MarketplaceDataManager được khởi tạo và đăng ký events
    /// </summary>
    private IEnumerator WaitForMarketplaceDataManager()
    {
        while (MarketplaceDataManager.Instance == null)
        {
            yield return null;
        }

        // Đăng ký events
        MarketplaceDataManager.Instance.OnMarketplaceDataRefreshed += OnMarketplaceDataRefreshed;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký events
        if (MarketplaceDataManager.Instance != null)
        {
            MarketplaceDataManager.Instance.OnMarketplaceDataRefreshed -= OnMarketplaceDataRefreshed;
        }
    }

    /// <summary>
    /// Callback khi marketplace data được refresh
    /// </summary>
    private void OnMarketplaceDataRefreshed()
    {
        // Update UI
        var allListings = MarketplaceDataManager.Instance.GetAllListings();
        
        ClearMarketplaceSlots();

        if (allListings.Count == 0)
        {
            UpdateStatusText("Marketplace chưa có items nào. Hãy bán item từ wallet inventory!");
        }
        else
        {
            foreach (var listing in allListings.Values)
            {
                if (listing != null && listing.itemData != null)
                {
                    CreateMarketplaceSlot(listing.tokenId, listing.itemData, listing.priceInGTK.ToString("F2"), listing.sellerAddress);
                }
            }
            
            UpdateStatusText($"Đã tải {allListings.Count} items từ marketplace");
        }
    }

    /// <summary>
    /// Kiểm tra marketplace có đang mở không
    /// </summary>
    public bool IsMarketplaceOpen
    {
        get
        {
            return marketplacePanel != null && marketplacePanel.activeSelf;
        }
    }

    /// <summary>
    /// Hiển thị marketplace UI
    /// </summary>
    public void ShowMarketplace()
    {
        if (marketplacePanel != null)
        {
            marketplacePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[MarketplaceUI] MarketplacePanel is null! Vui lòng assign marketplacePanel trong Inspector.");
        }
        
        // Tự động refresh khi mở
        RefreshMarketplaceUI();
    }

    /// <summary>
    /// Ẩn marketplace UI
    /// </summary>
    public void HideMarketplace()
    {
        if (marketplacePanel != null)
        {
            marketplacePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Refresh marketplace UI - lấy danh sách items đang được bán từ smart contract
    /// </summary>
    public void RefreshMarketplaceUI()
    {
        if (contentContainer == null)
        {
            Debug.LogError("[MarketplaceUI] ContentContainer is null! Please assign it in inspector.");
            return;
        }

        UpdateStatusText("Đang tải items từ marketplace...");

        // Fetch listings từ MarketplaceDataManager
        if (MarketplaceDataManager.Instance == null)
        {
            UpdateStatusText("MarketplaceDataManager chưa được khởi tạo!");
            return;
        }

        // Kiểm tra đang refresh không
        if (MarketplaceDataManager.Instance.IsRefreshing())
        {
            UpdateStatusText("Đang refresh, vui lòng đợi...");
            return;
        }

        // Refresh listings
        MarketplaceDataManager.Instance.RefreshMarketplaceListings();

        // Đợi một chút rồi update UI
        StartCoroutine(WaitAndUpdateUI());
    }

    /// <summary>
    /// Đợi và update UI sau khi fetch listings
    /// </summary>
    private IEnumerator WaitAndUpdateUI()
    {
        // Đợi MarketplaceDataManager refresh xong
        while (MarketplaceDataManager.Instance.IsRefreshing())
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Lấy listings và update UI
        var allListings = MarketplaceDataManager.Instance.GetAllListings();
        
        // Xóa các slot cũ
        ClearMarketplaceSlots();

        if (allListings.Count == 0)
        {
            UpdateStatusText("Marketplace chưa có items nào. Hãy bán item từ wallet inventory!");
        }
        else
        {
            // Tạo slot cho mỗi listing
            foreach (var listing in allListings.Values)
            {
                if (listing != null && listing.itemData != null)
                {
                    CreateMarketplaceSlot(listing.tokenId, listing.itemData, listing.priceInGTK.ToString("F2"), listing.sellerAddress);
                }
            }
            
            UpdateStatusText($"Đã tải {allListings.Count} items từ marketplace");
        }
    }

    /// <summary>
    /// Tạo một marketplace slot mới
    /// </summary>
    private void CreateMarketplaceSlot(string tokenId, ItemData itemData, string price, string sellerAddress)
    {
        if (marketplaceSlotPrefab == null)
        {
            Debug.LogError("[MarketplaceUI] MarketplaceSlotPrefab is null! Please assign it in inspector.");
            return;
        }

        // Instantiate slot prefab
        GameObject slotInstance = Instantiate(marketplaceSlotPrefab, contentContainer);
        
        // Setup slot với item data
        MarketplaceSlotUI itemUI = slotInstance.GetComponent<MarketplaceSlotUI>();
        if (itemUI == null)
        {
            itemUI = slotInstance.AddComponent<MarketplaceSlotUI>();
        }

        // Setup slot
        itemUI.SetupMarketplaceItem(tokenId, itemData, price, sellerAddress);
        
        // Set reference tới MarketplaceUI để có thể hiển thị ItemInfoPanel
        itemUI.SetMarketplaceUI(this);

        // Lưu reference (dùng tokenId làm key)
        marketplaceSlotInstances[tokenId] = slotInstance;
    }

    /// <summary>
    /// Xóa tất cả marketplace slots
    /// </summary>
    private void ClearMarketplaceSlots()
    {
        if (contentContainer == null) return;

        // Xóa tất cả children của content container
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = contentContainer.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear dictionary
        marketplaceSlotInstances.Clear();
    }

    /// <summary>
    /// Xử lý khi click refresh button
    /// </summary>
    private void OnRefreshButtonClicked()
    {
        RefreshMarketplaceUI();
    }

    /// <summary>
    /// Update status text
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[MarketplaceUI] {message}");
    }

    /// <summary>
    /// Hiển thị thông tin item trong info panel (dùng chung với InventoryUI)
    /// </summary>
    public void ShowItemInfo(ItemData itemData, string tokenId, string price)
    {
        if (itemInfoPanel != null)
        {
            // Pass thông tin marketplace item
            // TODO: Có thể cần tạo overload method riêng cho marketplace items
            itemInfoPanel.ShowItemInfo(itemData, isFromWallet: false, isFromEquipping: false);
            
            // TODO: Có thể cần thêm logic để hiển thị nút Buy trong ItemInfoPanel
        }
        else
        {
            Debug.LogWarning("[MarketplaceUI] ItemInfoPanel is not assigned!");
        }
    }
}

/// <summary>
/// Component để hiển thị thông tin marketplace item trong slot
/// </summary>
public class MarketplaceSlotUI : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI sellerText;
    [SerializeField] private Button button;

    private string tokenId;
    private ItemData itemData;
    private string price;
    private string sellerAddress;
    private MarketplaceUI marketplaceUI;

    private void Awake()
    {
        // Tự động tìm các component
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

        if (itemNameText == null)
        {
            itemNameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
        }
    }

    public void SetupMarketplaceItem(string tokenId, ItemData itemData, string price, string sellerAddress)
    {
        this.tokenId = tokenId;
        this.itemData = itemData;
        this.price = price;
        this.sellerAddress = sellerAddress;

        UpdateUI();
    }

    /// <summary>
    /// Set reference tới MarketplaceUI
    /// </summary>
    public void SetMarketplaceUI(MarketplaceUI ui)
    {
        marketplaceUI = ui;
    }

    /// <summary>
    /// Cập nhật UI hiển thị
    /// </summary>
    private void UpdateUI()
    {
        // Setup icon
        if (iconImage != null && itemData != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        // Setup item name
        if (itemNameText != null && itemData != null)
        {
            itemNameText.text = itemData.itemName ?? "Unknown Item";
        }

        // Setup price
        if (priceText != null)
        {
            priceText.text = $"{price} GTK";
        }

        // Setup seller (optional)
        if (sellerText != null && !string.IsNullOrEmpty(sellerAddress))
        {
            sellerText.text = $"Seller: {sellerAddress.Substring(0, 6)}...{sellerAddress.Substring(sellerAddress.Length - 4)}";
        }
    }

    /// <summary>
    /// Xử lý khi click vào marketplace item slot
    /// </summary>
    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (marketplaceUI != null && itemData != null)
        {
            marketplaceUI.ShowItemInfo(itemData, tokenId, price);
        }
    }
}

