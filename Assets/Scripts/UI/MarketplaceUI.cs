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
    [SerializeField] private MarketplaceItemInfoPanel marketplaceItemInfoPanel; // Panel hiển thị thông tin item trong marketplace
    [SerializeField] private Button refreshButton; // Button để refresh marketplace
    [SerializeField] private TextMeshProUGUI statusText; // Text hiển thị trạng thái
    
    [Header("Game Token UI")]
    [SerializeField] private GameObject gameTokenPanel; // Panel hiển thị game token balance
    [SerializeField] private TextMeshProUGUI gameTokenBalanceText; // Text hiển thị số dư token
    [SerializeField] private Button refreshTokenBalanceButton; // Button để refresh token balance
    [SerializeField] private TextMeshProUGUI tokenStatusText; // Text hiển thị trạng thái token (loading, error, etc.)
    
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

        // Tự động tìm MarketplaceItemInfoPanel nếu chưa được assign
        if (marketplaceItemInfoPanel == null)
        {
            marketplaceItemInfoPanel = FindAnyObjectByType<MarketplaceItemInfoPanel>();
        }

        // Setup refresh button
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        // Setup game token UI
        SetupGameTokenUI();

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

        if (GameTokenBalanceManager.Instance != null)
        {
            GameTokenBalanceManager.Instance.OnBalanceUpdated -= OnTokenBalanceUpdated;
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
        
        // Tự động refresh token balance khi mở marketplace lần đầu (nếu chưa có balance)
        if (GameTokenBalanceManager.Instance != null && GameTokenBalanceManager.Instance.GetCurrentBalance() == 0f)
        {
            // Chỉ refresh nếu chưa có balance (lần đầu mở)
            string walletAddress = InventoryManager.Instance?.GetWalletAddress();
            if (!string.IsNullOrEmpty(walletAddress))
            {
                GameTokenBalanceManager.Instance.RefreshBalance();
            }
        }
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
    /// Hiển thị thông tin item trong marketplace info panel
    /// </summary>
    public void ShowItemInfo(ItemData itemData, string tokenId, string price, string sellerAddress = "")
    {
        if (marketplaceItemInfoPanel != null)
        {
            marketplaceItemInfoPanel.ShowItemInfo(itemData, tokenId, price, sellerAddress);
        }
        else
        {
            Debug.LogWarning("[MarketplaceUI] MarketplaceItemInfoPanel is not assigned!");
        }
    }

    /// <summary>
    /// Setup game token UI components
    /// </summary>
    private void SetupGameTokenUI()
    {
        // Tự động tìm các component nếu chưa được assign
        if (gameTokenPanel == null)
        {
            // Tìm trong marketplace panel
            if (marketplacePanel != null)
            {
                Transform tokenPanelTransform = marketplacePanel.transform.Find("GameTokenPanel");
                if (tokenPanelTransform != null)
                {
                    gameTokenPanel = tokenPanelTransform.gameObject;
                }
            }
        }

        if (gameTokenBalanceText == null && gameTokenPanel != null)
        {
            Transform balanceTextTransform = gameTokenPanel.transform.Find("BalanceText");
            if (balanceTextTransform != null)
            {
                gameTokenBalanceText = balanceTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (refreshTokenBalanceButton == null && gameTokenPanel != null)
        {
            Transform refreshButtonTransform = gameTokenPanel.transform.Find("RefreshButton");
            if (refreshButtonTransform != null)
            {
                refreshTokenBalanceButton = refreshButtonTransform.GetComponent<Button>();
            }
        }

        if (tokenStatusText == null && gameTokenPanel != null)
        {
            Transform statusTextTransform = gameTokenPanel.transform.Find("StatusText");
            if (statusTextTransform != null)
            {
                tokenStatusText = statusTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Setup refresh button
        if (refreshTokenBalanceButton != null)
        {
            refreshTokenBalanceButton.onClick.RemoveAllListeners();
            refreshTokenBalanceButton.onClick.AddListener(OnRefreshTokenBalanceClicked);
        }

        // Đăng ký event từ GameTokenBalanceManager
        StartCoroutine(WaitForGameTokenBalanceManager());
    }

    /// <summary>
    /// Đợi GameTokenBalanceManager được khởi tạo và đăng ký events
    /// </summary>
    private IEnumerator WaitForGameTokenBalanceManager()
    {
        while (GameTokenBalanceManager.Instance == null)
        {
            yield return null;
        }

        // Đăng ký events
        GameTokenBalanceManager.Instance.OnBalanceUpdated += OnTokenBalanceUpdated;

        // Load balance hiện tại nếu đã có
        UpdateTokenBalanceUI(GameTokenBalanceManager.Instance.GetCurrentBalance());
    }

    /// <summary>
    /// Xử lý khi click refresh token balance button
    /// </summary>
    private void OnRefreshTokenBalanceClicked()
    {
        if (GameTokenBalanceManager.Instance == null)
        {
            UpdateTokenStatusText("GameTokenBalanceManager chưa được khởi tạo!");
            return;
        }

        if (GameTokenBalanceManager.Instance.IsRefreshing())
        {
            UpdateTokenStatusText("Đang refresh, vui lòng đợi...");
            return;
        }

        // Kiểm tra wallet address
        string walletAddress = InventoryManager.Instance?.GetWalletAddress();
        if (string.IsNullOrEmpty(walletAddress))
        {
            UpdateTokenStatusText("Chưa liên kết ví! Vui lòng liên kết ví trước.");
            return;
        }

        UpdateTokenStatusText("Đang tải số dư token...");
        GameTokenBalanceManager.Instance.RefreshBalance();
    }

    /// <summary>
    /// Callback khi token balance được update
    /// </summary>
    private void OnTokenBalanceUpdated(float balance)
    {
        UpdateTokenBalanceUI(balance);
        UpdateTokenStatusText("Đã update số dư từ ví");
    }

    /// <summary>
    /// Update UI hiển thị token balance
    /// </summary>
    private void UpdateTokenBalanceUI(float balance)
    {
        if (gameTokenBalanceText != null)
        {
            gameTokenBalanceText.text = $"{balance:F2}";
        }
    }

    /// <summary>
    /// Update status text cho token
    /// </summary>
    private void UpdateTokenStatusText(string message)
    {
        if (tokenStatusText != null)
        {
            tokenStatusText.text = message;
        }
        Debug.Log($"[MarketplaceUI] Token Status: {message}");
    }

    /// <summary>
    /// Coroutine để refresh marketplace UI sau một khoảng thời gian
    /// Public để có thể gọi từ MarketplaceItemInfoPanel
    /// </summary>
    public IEnumerator DelayedRefreshMarketplace()
    {
        yield return new WaitForSeconds(2f); // Đợi 2 giây
        
        // Refresh marketplace UI
        RefreshMarketplaceUI();
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

        // Không hiển thị tên trong slot (để tiết kiệm không gian)
        // Tên sẽ hiển thị trong MarketplaceItemInfoPanel khi click vào
        if (itemNameText != null)
        {
            itemNameText.text = ""; // Xóa tên
            itemNameText.gameObject.SetActive(false); // Ẩn text component
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
            marketplaceUI.ShowItemInfo(itemData, tokenId, price, sellerAddress);
        }
    }
}

