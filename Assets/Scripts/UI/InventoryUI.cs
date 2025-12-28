using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel; // Object cha chứa toàn bộ inventory UI
    [SerializeField] private ScrollRect scrollView; // ScrollView để hiển thị items
    [SerializeField] private Transform contentContainer; // Container trong ScrollView để chứa item slots
    [SerializeField] private GameObject inventorySlotPrefab; // Prefab cho mỗi item slot (InventorySlot.prefab)
    [SerializeField] private ItemInfoPanel itemInfoPanel; // Panel hiển thị thông tin item
    [SerializeField] private WalletInventoryUI walletInventoryUI; // Reference tới WalletInventoryUI
    
    [Header("Tab System")]
    [SerializeField] private Button inventoryTabButton; // Button để chuyển sang tab Inventory thường
    [SerializeField] private Button walletTabButton; // Button để chuyển sang tab Wallet Inventory
    [SerializeField] private Button equippingTabButton; // Button để chuyển sang tab Equipping
    [SerializeField] private GameObject normalInventoryContent; // Panel chứa inventory thường
    [SerializeField] private GameObject walletInventoryContent; // Panel chứa wallet inventory
    [SerializeField] private EquippingUI equippingUI; // Reference tới EquippingUI
    
    [Header("Game Token UI")]
    [SerializeField] private GameObject gameTokenPanel; // Panel hiển thị game token balance
    [SerializeField] private TextMeshProUGUI gameTokenBalanceText; // Text hiển thị số dư token
    [SerializeField] private Button refreshTokenBalanceButton; // Button để refresh token balance
    [SerializeField] private TextMeshProUGUI tokenStatusText; // Text hiển thị trạng thái (loading, error, etc.)
    
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I; // Phím để bật/tắt inventory

    private bool isInventoryOpen = false;
    private Dictionary<int, GameObject> itemSlotInstances = new Dictionary<int, GameObject>();
    private enum InventoryTab { Normal, Wallet, Equipping }
    private InventoryTab currentTab = InventoryTab.Normal; // Tab mặc định là Inventory thường

    // Static instance để các script khác có thể truy cập
    public static InventoryUI Instance { get; private set; }

    /// <summary>
    /// Kiểm tra inventory có đang mở không
    /// </summary>
    public bool IsInventoryOpen => isInventoryOpen;

    /// <summary>
    /// Static method để kiểm tra inventory có đang mở không (từ bất kỳ đâu)
    /// </summary>
    public static bool IsInventoryOpenStatic()
    {
        if (Instance != null)
        {
            return Instance.isInventoryOpen;
        }
        return false;
    }

    private void Awake()
    {
        // Setup singleton instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ẩn inventory panel lúc đầu
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        // Tự động tìm ItemInfoPanel nếu chưa được assign
        if (itemInfoPanel == null)
        {
            itemInfoPanel = FindAnyObjectByType<ItemInfoPanel>();
        }

        // Tự động tìm WalletInventoryUI nếu chưa được assign
        if (walletInventoryUI == null)
        {
            walletInventoryUI = FindAnyObjectByType<WalletInventoryUI>();
        }

        // Tự động tìm EquippingUI nếu chưa được assign
        if (equippingUI == null)
        {
            equippingUI = FindAnyObjectByType<EquippingUI>();
        }

        // Đăng ký event từ InventoryManager
        StartCoroutine(WaitForInventoryManager());

        // Setup game token UI
        SetupGameTokenUI();

        // Setup tab system
        SetupTabSystem();
    }

    /// <summary>
    /// Setup tab system với 2 buttons
    /// </summary>
    private void SetupTabSystem()
    {
        // Tự động tìm các buttons nếu chưa được assign
        if (inventoryTabButton == null && inventoryPanel != null)
        {
            Transform tabTransform = inventoryPanel.transform.Find("InventoryTabButton");
            if (tabTransform != null)
            {
                inventoryTabButton = tabTransform.GetComponent<Button>();
            }
        }

        if (walletTabButton == null && inventoryPanel != null)
        {
            Transform tabTransform = inventoryPanel.transform.Find("WalletTabButton");
            if (tabTransform != null)
            {
                walletTabButton = tabTransform.GetComponent<Button>();
            }
        }

        if (equippingTabButton == null && inventoryPanel != null)
        {
            Transform tabTransform = inventoryPanel.transform.Find("EquippingTabButton");
            if (tabTransform != null)
            {
                equippingTabButton = tabTransform.GetComponent<Button>();
            }
        }

        // Tự động tìm content panels nếu chưa được assign
        if (normalInventoryContent == null && inventoryPanel != null)
        {
            Transform contentTransform = inventoryPanel.transform.Find("NormalInventoryContent");
            if (contentTransform != null)
            {
                normalInventoryContent = contentTransform.gameObject;
            }
            else
            {
                // Fallback: tìm scrollView hoặc contentContainer
                if (scrollView != null)
                {
                    normalInventoryContent = scrollView.gameObject;
                }
            }
        }

        if (walletInventoryContent == null && walletInventoryUI != null)
        {
            // Lấy wallet inventory panel từ WalletInventoryUI
            if (walletInventoryUI.WalletInventoryPanel != null)
            {
                walletInventoryContent = walletInventoryUI.WalletInventoryPanel;
            }
        }

        // Setup button listeners
        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.RemoveAllListeners();
            inventoryTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Normal));
        }

        if (walletTabButton != null)
        {
            walletTabButton.onClick.RemoveAllListeners();
            walletTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Wallet));
        }

        if (equippingTabButton != null)
        {
            equippingTabButton.onClick.RemoveAllListeners();
            equippingTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Equipping));
        }

        // Set tab mặc định
        SwitchTab(InventoryTab.Normal, false); // false = không refresh UI
    }

    /// <summary>
    /// Setup game token UI components
    /// </summary>
    private void SetupGameTokenUI()
    {
        // Tự động tìm các component nếu chưa được assign
        if (gameTokenPanel == null)
        {
            // Tìm trong inventory panel
            if (inventoryPanel != null)
            {
                Transform tokenPanelTransform = inventoryPanel.transform.Find("GameTokenPanel");
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

    private IEnumerator WaitForInventoryManager()
    {
        // Đợi InventoryManager được khởi tạo
        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        // Đăng ký events
        InventoryManager.Instance.OnInventoryLoaded += OnInventoryLoaded;
        InventoryManager.Instance.OnItemAdded += OnItemAdded;

        // Nếu inventory đã được load trước đó, refresh UI ngay
        RefreshInventoryUI();
    }

    private void OnDestroy()
    {
        // Hủy đăng ký events khi object bị destroy
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryLoaded -= OnInventoryLoaded;
            InventoryManager.Instance.OnItemAdded -= OnItemAdded;
        }

        if (GameTokenBalanceManager.Instance != null)
        {
            GameTokenBalanceManager.Instance.OnBalanceUpdated -= OnTokenBalanceUpdated;
        }
    }

    private void Update()
    {
        // Kiểm tra phím 'I' để bật/tắt inventory
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// Bật/tắt inventory UI
    /// </summary>
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
        }

        // Khi mở inventory, hiển thị tab hiện tại
        if (isInventoryOpen)
        {
            // Reset về tab Normal khi mở inventory
            currentTab = InventoryTab.Normal;
            SwitchTab(InventoryTab.Normal);
            
            // Luôn hiển thị equipping panel khi inventory mở
            if (equippingUI != null)
            {
                equippingUI.ShowEquipping();
            }
            
            // Tự động refresh token balance khi mở inventory lần đầu (nếu chưa có balance)
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
        else
        {
            // Ẩn tất cả content khi đóng inventory (bao gồm equipping panel)
            if (normalInventoryContent != null)
            {
                normalInventoryContent.SetActive(false);
            }
            if (walletInventoryContent != null)
            {
                walletInventoryContent.SetActive(false);
            }
            if (equippingUI != null && equippingUI.EquippingPanel != null)
            {
                equippingUI.EquippingPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Chuyển đổi giữa các tab
    /// </summary>
    private void SwitchTab(InventoryTab tab, bool refreshUI = true)
    {
        currentTab = tab;

        // Ẩn tất cả content trước (trừ equipping panel - luôn hiển thị)
        if (normalInventoryContent != null)
        {
            normalInventoryContent.SetActive(false);
        }
        if (walletInventoryUI != null)
        {
            walletInventoryUI.HideWalletInventory();
        }
        // Equipping panel luôn hiển thị khi inventory mở, không cần ẩn

        // Hiển thị content của tab được chọn
        switch (tab)
        {
            case InventoryTab.Normal:
                if (normalInventoryContent != null)
                {
                    normalInventoryContent.SetActive(true);
                }
                if (refreshUI)
                {
                    RefreshInventoryUI();
                }
                break;

            case InventoryTab.Wallet:
                // Tự động fetch wallet inventory lần đầu khi chuyển sang tab này
                if (walletInventoryUI != null)
                {
                    walletInventoryUI.ShowWalletInventory();
                }
                break;

            case InventoryTab.Equipping:
                // Hiển thị equipping UI
                if (equippingUI != null)
                {
                    equippingUI.ShowEquipping();
                }
                break;
        }

        // Update button states (visual feedback)
        UpdateTabButtonStates();
    }

    /// <summary>
    /// Update visual state của tab buttons (highlight active tab)
    /// </summary>
    private void UpdateTabButtonStates()
    {
        // Có thể thêm logic để highlight button của tab đang active
        // Ví dụ: đổi màu, scale, etc.
        // Tạm thời để trống, có thể implement sau nếu cần
    }

    /// <summary>
    /// Refresh toàn bộ inventory UI từ InventoryManager
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUI] InventoryManager.Instance is null!");
            return;
        }

        if (contentContainer == null)
        {
            Debug.LogError("[InventoryUI] ContentContainer is null! Please assign it in inspector.");
            return;
        }

        // Lấy tất cả items từ InventoryManager
        Dictionary<int, InventoryItem> allItems = InventoryManager.Instance.GetAllItems();

        // Xóa các slot cũ
        ClearInventorySlots();

        // Tạo slot mới cho mỗi item
        foreach (var kvp in allItems)
        {
            int itemID = kvp.Key;
            InventoryItem inventoryItem = kvp.Value;

            // Lấy ItemData từ ItemDatabase
            ItemData itemData = ItemDatabase.Instance?.GetItemByID(itemID);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryUI] ItemData not found for ID: {itemID}");
                continue;
            }

            // Tạo slot mới
            CreateInventorySlot(itemID, inventoryItem, itemData);
        }

        Debug.Log($"[InventoryUI] Refreshed inventory UI with {allItems.Count} items");
    }

    /// <summary>
    /// Tạo một inventory slot mới
    /// </summary>
    private void CreateInventorySlot(int itemID, InventoryItem inventoryItem, ItemData itemData)
    {
        if (inventorySlotPrefab == null)
        {
            Debug.LogError("[InventoryUI] InventorySlotPrefab is null! Please assign it in inspector.");
            return;
        }

        // Instantiate slot prefab
        GameObject slotInstance = Instantiate(inventorySlotPrefab, contentContainer);
        
        // Lấy component InventoryItemUI (sẽ tạo sau)
        InventoryItemUI itemUI = slotInstance.GetComponent<InventoryItemUI>();
        if (itemUI == null)
        {
            // Nếu chưa có component, thêm vào
            itemUI = slotInstance.AddComponent<InventoryItemUI>();
        }

        // Setup slot với item data
        itemUI.SetupItem(itemID, inventoryItem, itemData);

        // Lưu reference
        itemSlotInstances[itemID] = slotInstance;
    }

    /// <summary>
    /// Xóa tất cả inventory slots
    /// </summary>
    private void ClearInventorySlots()
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
        itemSlotInstances.Clear();
    }

    /// <summary>
    /// Được gọi khi inventory được load từ PlayFab (event callback)
    /// </summary>
    private void OnInventoryLoaded()
    {
        RefreshInventoryUI();
    }

    /// <summary>
    /// Được gọi khi item được thêm vào inventory (event callback)
    /// </summary>
    private void OnItemAdded(int itemID)
    {
        // Refresh toàn bộ UI
        RefreshInventoryUI();
    }

    /// <summary>
    /// Hiển thị thông tin item trong info panel
    /// </summary>
    public void ShowItemInfo(ItemData itemData)
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.ShowItemInfo(itemData);
        }
        else
        {
            Debug.LogWarning("[InventoryUI] ItemInfoPanel is not assigned!");
        }
    }

    /// <summary>
    /// Ẩn info panel
    /// </summary>
    public void HideItemInfo()
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.Hide();
        }
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
    /// Update status text
    /// </summary>
    private void UpdateTokenStatusText(string message)
    {
        if (tokenStatusText != null)
        {
            tokenStatusText.text = message;
        }
        Debug.Log($"[InventoryUI] Token Status: {message}");
    }
}

