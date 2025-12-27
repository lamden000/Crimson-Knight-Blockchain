using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI để hiển thị NFT từ blockchain wallet
/// </summary>
public class WalletInventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject walletInventoryPanel; // Panel chứa toàn bộ wallet inventory UI
    [SerializeField] private ScrollRect scrollView; // ScrollView để hiển thị items
    [SerializeField] private Transform contentContainer; // Container trong ScrollView để chứa item slots
    [SerializeField] private GameObject walletItemSlotPrefab; // Prefab cho mỗi wallet item slot
    [SerializeField] private Button refreshButton; // Button để refresh wallet inventory
    [SerializeField] private TextMeshProUGUI statusText; // Text hiển thị trạng thái
    [SerializeField] private TextMeshProUGUI walletAddressText; // Text hiển thị địa chỉ ví
    [SerializeField] private ItemInfoPanel itemInfoPanel; // Panel hiển thị thông tin item (dùng chung với InventoryUI)
    
    private bool isWalletInventoryOpen = false;
    private Dictionary<int, GameObject> walletItemSlotInstances = new Dictionary<int, GameObject>();
    private bool hasFetchedOnce = false; // Flag để track xem đã fetch lần đầu chưa

    private void Start()
    {
        // Ẩn wallet inventory panel lúc đầu
        if (walletInventoryPanel != null)
        {
            walletInventoryPanel.SetActive(false);
        }

        // Tự động tìm ItemInfoPanel nếu chưa được assign (dùng chung với InventoryUI)
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

        // Đăng ký event từ WalletInventoryManager
        StartCoroutine(WaitForWalletInventoryManager());
    }

    private IEnumerator WaitForWalletInventoryManager()
    {
        // Đợi WalletInventoryManager được khởi tạo
        while (WalletInventoryManager.Instance == null)
        {
            yield return null;
        }

        // Đăng ký events
        WalletInventoryManager.Instance.OnWalletInventoryRefreshed += OnWalletInventoryRefreshed;

        // Tự động refresh khi mở lần đầu
        if (isWalletInventoryOpen)
        {
            RefreshWalletInventoryUI();
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký events khi object bị destroy
        if (WalletInventoryManager.Instance != null)
        {
            WalletInventoryManager.Instance.OnWalletInventoryRefreshed -= OnWalletInventoryRefreshed;
        }
    }

    /// <summary>
    /// Hiển thị wallet inventory UI (được gọi từ InventoryUI khi mở inventory)
    /// </summary>
    public void ShowWalletInventory()
    {
        isWalletInventoryOpen = true;
        
        if (walletInventoryPanel != null)
        {
            walletInventoryPanel.SetActive(true);
        }

        // Tự động fetch wallet inventory lần đầu khi mở
        if (!hasFetchedOnce)
        {
            AutoFetchWalletInventory();
        }
        else
        {
            // Nếu đã fetch rồi, chỉ refresh UI
            RefreshWalletInventoryUI();
        }
    }

    /// <summary>
    /// Ẩn wallet inventory UI (được gọi từ InventoryUI khi đóng inventory)
    /// </summary>
    public void HideWalletInventory()
    {
        isWalletInventoryOpen = false;
        
        if (walletInventoryPanel != null)
        {
            walletInventoryPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Tự động fetch wallet inventory lần đầu
    /// </summary>
    private void AutoFetchWalletInventory()
    {
        if (WalletInventoryManager.Instance == null)
        {
            Debug.LogWarning("[WalletInventoryUI] WalletInventoryManager chưa được khởi tạo!");
            return;
        }

        // Kiểm tra wallet address
        string walletAddress = InventoryManager.Instance?.GetWalletAddress();
        if (string.IsNullOrEmpty(walletAddress))
        {
            UpdateStatusText("Chưa liên kết ví. Wallet inventory sẽ trống.");
            hasFetchedOnce = true; // Đánh dấu đã thử fetch để không thử lại
            RefreshWalletInventoryUI(); // Refresh UI để hiển thị empty state
            return;
        }

        // Kiểm tra đang refresh không
        if (WalletInventoryManager.Instance.IsRefreshing())
        {
            UpdateStatusText("Đang tải NFT từ blockchain...");
            return;
        }

        UpdateStatusText("Đang tải NFT từ blockchain...");
        WalletInventoryManager.Instance.RefreshWalletInventory();
        hasFetchedOnce = true;
    }

    /// <summary>
    /// Refresh toàn bộ wallet inventory UI
    /// </summary>
    public void RefreshWalletInventoryUI()
    {
        if (WalletInventoryManager.Instance == null)
        {
            Debug.LogWarning("[WalletInventoryUI] WalletInventoryManager.Instance is null!");
            UpdateStatusText("Wallet Inventory Manager chưa được khởi tạo!");
            return;
        }

        if (contentContainer == null)
        {
            Debug.LogError("[WalletInventoryUI] ContentContainer is null! Please assign it in inspector.");
            return;
        }

        // Lấy tất cả NFT từ wallet
        Dictionary<int, WalletInventoryManager.WalletNFT> allNFTs = WalletInventoryManager.Instance.GetAllWalletNFTs();

        // Xóa các slot cũ
        ClearWalletItemSlots();

        // Hiển thị wallet address
        string walletAddress = InventoryManager.Instance?.GetWalletAddress() ?? "";
        if (walletAddressText != null)
        {
            if (!string.IsNullOrEmpty(walletAddress))
            {
                walletAddressText.text = $"Wallet: {walletAddress.Substring(0, 6)}...{walletAddress.Substring(walletAddress.Length - 4)}";
            }
            else
            {
                walletAddressText.text = "Wallet: Chưa liên kết";
            }
        }

        // Nhóm NFT theo itemID và đếm số lượng
        Dictionary<int, int> itemCounts = new Dictionary<int, int>();
        Dictionary<int, WalletInventoryManager.WalletNFT> firstNFTByItem = new Dictionary<int, WalletInventoryManager.WalletNFT>();

        foreach (var nft in allNFTs.Values)
        {
            if (nft.itemID > 0)
            {
                if (!itemCounts.ContainsKey(nft.itemID))
                {
                    itemCounts[nft.itemID] = 0;
                    firstNFTByItem[nft.itemID] = nft;
                }
                itemCounts[nft.itemID]++;
            }
        }

        // Tạo slot mới cho mỗi item type
        foreach (var kvp in itemCounts)
        {
            int itemID = kvp.Key;
            int count = kvp.Value;
            WalletInventoryManager.WalletNFT nft = firstNFTByItem[itemID];

            // Lấy ItemData từ ItemDatabase
            ItemData itemData = ItemDatabase.Instance?.GetItemByID(itemID);
            if (itemData == null)
            {
                Debug.LogWarning($"[WalletInventoryUI] ItemData not found for ID: {itemID}");
                continue;
            }

            // Tạo slot mới
            CreateWalletItemSlot(itemID, count, itemData, nft);
        }

        // Update status
        if (allNFTs.Count == 0)
        {
            UpdateStatusText("Không có NFT nào trong ví. Hãy withdraw item từ game!");
        }
        else
        {
            UpdateStatusText($"Đã tải {allNFTs.Count} NFT từ blockchain");
        }

        Debug.Log($"[WalletInventoryUI] Refreshed wallet inventory UI with {itemCounts.Count} unique items");
    }

    /// <summary>
    /// Tạo một wallet item slot mới
    /// </summary>
    private void CreateWalletItemSlot(int itemID, int quantity, ItemData itemData, WalletInventoryManager.WalletNFT nft)
    {
        if (walletItemSlotPrefab == null)
        {
            Debug.LogError("[WalletInventoryUI] WalletItemSlotPrefab is null! Please assign it in inspector.");
            return;
        }

        // Instantiate slot prefab
        GameObject slotInstance = Instantiate(walletItemSlotPrefab, contentContainer);
        
        // Setup slot với item data
        // Giả sử prefab có component WalletItemSlotUI (sẽ tạo sau nếu cần)
        WalletItemSlotUI itemUI = slotInstance.GetComponent<WalletItemSlotUI>();
        if (itemUI == null)
        {
            // Nếu chưa có component, thêm vào
            itemUI = slotInstance.AddComponent<WalletItemSlotUI>();
        }

        // Setup slot
        itemUI.SetupWalletItem(itemID, quantity, itemData, nft);
        
        // Set reference tới WalletInventoryUI để có thể hiển thị ItemInfoPanel
        itemUI.SetWalletInventoryUI(this);

        // Lưu reference (dùng itemID làm key)
        walletItemSlotInstances[itemID] = slotInstance;
    }

    /// <summary>
    /// Xóa tất cả wallet item slots
    /// </summary>
    private void ClearWalletItemSlots()
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
        walletItemSlotInstances.Clear();
    }

    /// <summary>
    /// Được gọi khi wallet inventory được refresh (event callback)
    /// </summary>
    private void OnWalletInventoryRefreshed()
    {
        RefreshWalletInventoryUI();
    }

    /// <summary>
    /// Xử lý khi click refresh button
    /// </summary>
    private void OnRefreshButtonClicked()
    {
        if (WalletInventoryManager.Instance == null)
        {
            UpdateStatusText("Wallet Inventory Manager chưa được khởi tạo!");
            return;
        }

        if (WalletInventoryManager.Instance.IsRefreshing())
        {
            UpdateStatusText("Đang refresh, vui lòng đợi...");
            return;
        }

        // Kiểm tra wallet address
        string walletAddress = InventoryManager.Instance?.GetWalletAddress();
        if (string.IsNullOrEmpty(walletAddress))
        {
            UpdateStatusText("Chưa liên kết ví! Vui lòng liên kết ví trước.");
            return;
        }

        UpdateStatusText("Đang tải NFT từ blockchain...");
        WalletInventoryManager.Instance.RefreshWalletInventory();
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
        Debug.Log($"[WalletInventoryUI] {message}");
    }


    /// <summary>
    /// Hiển thị thông tin item trong info panel (dùng chung với InventoryUI)
    /// </summary>
    public void ShowItemInfo(ItemData itemData)
    {
        if (itemInfoPanel != null)
        {
            // Pass isFromWallet = true vì item từ wallet inventory (đã withdraw rồi)
            itemInfoPanel.ShowItemInfo(itemData, isFromWallet: true);
        }
        else
        {
            Debug.LogWarning("[WalletInventoryUI] ItemInfoPanel is not assigned!");
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
}

/// <summary>
/// Component để hiển thị thông tin wallet item trong slot
/// </summary>
public class WalletItemSlotUI : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI tokenIdText;
    [SerializeField] private Button button; // Button để click (optional)

    private int itemID;
    private WalletInventoryManager.WalletNFT nft;
    private ItemData itemData;
    private WalletInventoryUI walletInventoryUI;

    private void Awake()
    {
        // Tự động tìm các component nếu chưa được assign (giống InventoryItemUI)
        if (iconImage == null)
        {
            // Tìm Image component trong children có tên "Icon"
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
            else
            {
                // Nếu không tìm thấy, lấy Image component đầu tiên trong children
                iconImage = GetComponentInChildren<Image>();
            }
        }

        if (quantityText == null)
        {
            // Tìm TextMeshProUGUI có tên chứa "quantity" hoặc "quanity" (typo trong prefab)
            Transform quantityTransform = transform.Find("quanityTxt");
            if (quantityTransform == null)
            {
                quantityTransform = transform.Find("quantityTxt");
            }
            if (quantityTransform != null)
            {
                quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Nếu không tìm thấy, lấy TextMeshProUGUI đầu tiên
                quantityText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        // Nếu chưa có Button, thêm Button component để có thể click
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
        }
    }

    public void SetupWalletItem(int itemID, int quantity, ItemData itemData, WalletInventoryManager.WalletNFT nft)
    {
        this.itemID = itemID;
        this.nft = nft;
        this.itemData = itemData;

        UpdateUI(quantity);
    }

    /// <summary>
    /// Set reference tới WalletInventoryUI để có thể hiển thị ItemInfoPanel
    /// </summary>
    public void SetWalletInventoryUI(WalletInventoryUI walletUI)
    {
        walletInventoryUI = walletUI;
    }

    /// <summary>
    /// Cập nhật UI hiển thị
    /// </summary>
    private void UpdateUI(int quantity)
    {
        // Setup icon
        if (iconImage != null)
        {
            if (itemData != null && itemData.icon != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
                Debug.LogWarning($"[WalletItemSlotUI] ItemData hoặc icon is null cho itemID: {itemID}");
            }
        }
        else
        {
            Debug.LogWarning($"[WalletItemSlotUI] iconImage is null! Không thể hiển thị icon.");
        }

        // Setup quantity
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // Setup item name
        if (itemNameText != null && itemData != null)
        {
            itemNameText.text = itemData.itemName ?? "Unknown Item";
        }

        // Setup token ID (optional)
        if (tokenIdText != null && nft != null)
        {
            tokenIdText.text = $"Token ID: {nft.tokenId}";
        }
    }

    /// <summary>
    /// Xử lý khi click vào wallet item slot
    /// </summary>
    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Gọi WalletInventoryUI để hiển thị info panel
        if (walletInventoryUI != null && itemData != null)
        {
            walletInventoryUI.ShowItemInfo(itemData);
        }
        else if (itemData != null)
        {
            // Fallback: tìm WalletInventoryUI trong scene
            WalletInventoryUI foundUI = FindAnyObjectByType<WalletInventoryUI>();
            if (foundUI != null)
            {
                foundUI.ShowItemInfo(itemData);
            }
        }
    }
}

