using System;
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
    [SerializeField] private Button useButton; // Button để use/unequip item (cho EquipmentItem)
    [SerializeField] private TextMeshProUGUI useButtonText; // Text component của useButton để đổi text
    [SerializeField] private Image useButtonBackground; // Image background của useButton để đổi màu
    [SerializeField] private Button sellButton; // Button để bán item (chỉ hiện khi item từ wallet)

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

        if (useButton == null)
        {
            Transform useTransform = transform.Find("UseButton");
            if (useTransform != null)
            {
                useButton = useTransform.GetComponent<Button>();
            }
        }

        if (sellButton == null)
        {
            Transform sellTransform = transform.Find("SellButton");
            if (sellTransform != null)
            {
                sellButton = sellTransform.GetComponent<Button>();
            }
        }

        // Tự động tìm useButtonText và useButtonBackground
        if (useButton != null)
        {
            // Tìm TextMeshProUGUI trong useButton
            if (useButtonText == null)
            {
                useButtonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            // Tìm Image background của useButton (thường là Image component của chính button)
            if (useButtonBackground == null)
            {
                useButtonBackground = useButton.GetComponent<Image>();
            }

            // Lưu màu gốc của button
            if (useButtonBackground != null)
            {
                originalButtonColor = useButtonBackground.color;
            }
        }

        // Setup withdraw button click
        if (withdrawButton != null)
        {
            withdrawButton.onClick.RemoveAllListeners();
            withdrawButton.onClick.AddListener(OnWithdrawButtonClicked);
        }

        // Setup use button click
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            // Sẽ được set động trong ShowItemInfo dựa trên isFromEquipping
        }

        // Setup sell button click
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellButtonClicked);
        }
    }

    private ItemData currentItemData;
    private bool currentItemIsFromWallet = false; // Track item hiện tại có từ wallet không
    private WalletInventoryManager.WalletNFT currentWalletNFT = null; // NFT data nếu item từ wallet
    private Color originalButtonColor = Color.white; // Lưu màu gốc của button

    private void Start()
    {
        // Ẩn panel lúc đầu
        Hide();
    }

    /// <summary>
    /// Hiển thị thông tin item
    /// </summary>
    /// <param name="itemData">ItemData của item</param>
    /// <param name="isFromWallet">True nếu item từ wallet inventory (đã withdraw), false nếu từ inventory thường</param>
    /// <param name="isFromEquipping">True nếu item từ equipping tab</param>
    /// <param name="walletNFT">WalletNFT data nếu item từ wallet (để lấy tokenId)</param>
    public void ShowItemInfo(ItemData itemData, bool isFromWallet = false, bool isFromEquipping = false, WalletInventoryManager.WalletNFT walletNFT = null)
    {
        if (itemData == null)
        {
            Debug.LogWarning("[ItemInfoPanel] Cannot show info for null item!");
            return;
        }

        currentItemData = itemData;
        currentItemIsFromWallet = isFromWallet; // Lưu flag isFromWallet
        currentWalletNFT = walletNFT; // Lưu NFT data nếu có

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

        // Update withdraw button
        // - Disable nếu item từ wallet (đã withdraw rồi) hoặc từ equipping tab
        // - Enable nếu item từ inventory thường và có thể withdraw
        if (withdrawButton != null)
        {
            if (isFromWallet || isFromEquipping)
            {
                // Item từ wallet hoặc equipping tab - không thể withdraw
                withdrawButton.interactable = false;
            }
            else
            {
                // Item từ inventory thường - enable nếu có thể withdraw
            withdrawButton.interactable = itemData.withdrawable;
            }
        }

        // Update use button
        // - Hiển thị cho EquipmentItem (từ inventory thường, equipping tab, hoặc wallet)
        if (useButton != null)
        {
            bool isEquipment = itemData is EquipmentItem;
            bool shouldShow = isEquipment; // Cho phép use từ cả wallet
            
            useButton.gameObject.SetActive(shouldShow);
            useButton.interactable = shouldShow;

            if (shouldShow)
            {
                // Setup button dựa trên isFromEquipping
                if (isFromEquipping)
                {
                    // Item từ equipping tab - đổi thành "Unequip" và màu đỏ
                    if (useButtonText != null)
                    {
                        useButtonText.text = "Unequip";
                    }

                    if (useButtonBackground != null)
                    {
                        useButtonBackground.color = Color.red;
                    }

                    // Setup click handler cho unequip
                    useButton.onClick.RemoveAllListeners();
                    useButton.onClick.AddListener(OnUnequipButtonClicked);
                }
                else
                {
                    // Item từ inventory thường hoặc wallet - giữ nguyên "Use" và màu mặc định
                    if (useButtonText != null)
                    {
                        useButtonText.text = "Use";
                    }

                    if (useButtonBackground != null)
                    {
                        useButtonBackground.color = originalButtonColor; // Khôi phục màu gốc
                    }

                    // Setup click handler cho use
                    useButton.onClick.RemoveAllListeners();
                    useButton.onClick.AddListener(OnUseButtonClicked);
                }
            }
        }

        // Update sell button
        // - Chỉ hiển thị khi item từ wallet (đã withdraw, có tokenId)
        if (sellButton != null)
        {
            bool shouldShowSell = isFromWallet && walletNFT != null && !string.IsNullOrEmpty(walletNFT.tokenId);
            sellButton.gameObject.SetActive(shouldShowSell);
            sellButton.interactable = shouldShowSell;
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
    /// Xử lý khi click use button
    /// </summary>
    private void OnUseButtonClicked()
    {
        if (currentItemData == null)
        {
            return;
        }

        // Chỉ xử lý cho EquipmentItem
        if (currentItemData is EquipmentItem equipmentItem)
        {
            if (currentItemIsFromWallet)
            {
                // Item từ wallet - không xóa khỏi wallet, chỉ equip
                EquippingUI equippingUI = FindAnyObjectByType<EquippingUI>();
                if (equippingUI != null)
                {
                    // Equip item vào equipping tab với flag isFromWallet = true
                    equippingUI.EquipItem(equipmentItem, 1, isFromWallet: true);
                }
                else
                {
                    Debug.LogWarning("[ItemInfoPanel] EquippingUI not found! Cannot equip item.");
                }
            }
            else
            {
                // Item từ inventory thường - kiểm tra số lượng
                int currentQuantity = InventoryManager.Instance?.GetItemQuantity(currentItemData.itemID) ?? 0;
                if (currentQuantity <= 0)
                {
                    Debug.LogWarning("[ItemInfoPanel] Cannot use: no items available!");
                    return;
                }

                // Chuyển item sang equipping tab
                EquippingUI equippingUI = FindAnyObjectByType<EquippingUI>();
                if (equippingUI != null)
                {
                    // Xóa 1 item khỏi inventory
                    InventoryManager.Instance?.RemoveItem(currentItemData.itemID, 1);
                    
                    // Equip item vào equipping tab với flag isFromWallet = false
                    equippingUI.EquipItem(equipmentItem, 1, isFromWallet: false);
                    
                    // Refresh inventory UI để cập nhật số lượng
                    if (InventoryUI.Instance != null)
                    {
                        InventoryUI.Instance.RefreshInventoryUI();
                    }
                }
                else
                {
                    Debug.LogWarning("[ItemInfoPanel] EquippingUI not found! Cannot equip item.");
                }
            }

            // Ẩn panel sau khi sử dụng
            Hide();
        }
        else
        {
            Debug.LogWarning("[ItemInfoPanel] Item is not an EquipmentItem!");
        }
    }

    /// <summary>
    /// Xử lý khi click unequip button
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        if (currentItemData == null)
        {
            return;
        }

        // Chỉ xử lý cho EquipmentItem
        if (currentItemData is EquipmentItem equipmentItem)
        {
            EquippingUI equippingUI = FindAnyObjectByType<EquippingUI>();
            if (equippingUI != null)
            {
                // Unequip item
                equippingUI.UnequipItem(equipmentItem.slot);
                
                // Refresh equipping UI
                equippingUI.RefreshEquippedItems();
                
                // Refresh inventory UI để hiển thị item đã được chuyển về
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.RefreshInventoryUI();
                }
            }
            else
            {
                Debug.LogWarning("[ItemInfoPanel] EquippingUI not found! Cannot unequip item.");
            }

            // Ẩn panel sau khi unequip
            Hide();
        }
        else
        {
            Debug.LogWarning("[ItemInfoPanel] Item is not an EquipmentItem!");
        }
    }

    /// <summary>
    /// Xử lý khi click sell button
    /// </summary>
    private void OnSellButtonClicked()
    {
        if (currentItemData == null || !currentItemIsFromWallet || currentWalletNFT == null)
        {
            Debug.LogWarning("[ItemInfoPanel] Cannot sell: item is not from wallet or missing NFT data!");
            return;
        }

        // Hiển thị dialog để nhập giá bán
        if (UIManager.Instance != null && UIManager.Instance.DialogFactory != null)
        {
            var dialog = UIManager.Instance.DialogFactory.CreateSellItemDialog();
            if (dialog != null)
            {
                dialog.Setup(currentItemData, currentWalletNFT, OnSellConfirmed);
                dialog.Show();
            }
            else
            {
                Debug.LogError("[ItemInfoPanel] SellItemDialog prefab is not assigned in DialogFactory!");
            }
        }
        else
        {
            Debug.LogError("[ItemInfoPanel] UIManager or DialogFactory not found!");
        }
    }

    /// <summary>
    /// Callback khi sell được confirm
    /// </summary>
    private void OnSellConfirmed(string tokenId, string price)
    {
        if (MarketplaceManager.Instance == null)
        {
            Debug.LogError("[ItemInfoPanel] MarketplaceManager is null!");
            return;
        }

        if (currentItemData == null || currentWalletNFT == null)
        {
            Debug.LogError("[ItemInfoPanel] currentItemData hoặc currentWalletNFT is null!");
            return;
        }

        // Convert tokenId từ hex string sang int rồi lại sang hex (để đảm bảo format đúng)
        string tokenIdHex = ConvertTokenIdToHex(tokenId);

        // Convert price từ GTK sang wei (price * 10^18)
        string priceInWei = ConvertGTKToWei(price);

        // Lấy seller address từ wallet
        string sellerAddress = InventoryManager.Instance?.GetWalletAddress();
        if (string.IsNullOrEmpty(sellerAddress))
        {
            Debug.LogError("[ItemInfoPanel] Wallet address chưa được set! Vui lòng liên kết ví trước.");
            return;
        }

        // Lưu listing vào PlayFab Title Data ngay khi confirm (pre-list)
        if (MarketplacePlayFabManager.Instance != null)
        {
            MarketplacePlayFabManager.Instance.AddListing(
                tokenIdHex,
                sellerAddress,
                priceInWei,
                currentItemData.itemID,
                "" // transactionHash sẽ được update sau khi transaction thành công
            );
            Debug.Log($"[ItemInfoPanel] Đã lưu listing vào PlayFab Title Data: TokenId={tokenIdHex}, Price={price} GTK");
        }
        else
        {
            Debug.LogWarning("[ItemInfoPanel] MarketplacePlayFabManager chưa được khởi tạo!");
        }

        // Mở trang web để ký transaction bán
        MarketplaceManager.Instance.OpenSellItemPage(tokenIdHex, price);
    }

    /// <summary>
    /// Convert GTK (Game Token) sang wei (price * 10^18)
    /// </summary>
    private string ConvertGTKToWei(string gtkPrice)
    {
        try
        {
            // Parse price từ string
            if (!float.TryParse(gtkPrice, out float priceFloat))
            {
                Debug.LogWarning($"[ItemInfoPanel] Không thể parse price '{gtkPrice}'");
                return "0";
            }

            // Convert sang wei (multiply by 10^18)
            System.Numerics.BigInteger priceWei = (System.Numerics.BigInteger)(priceFloat * Math.Pow(10, 18));
            
            // Convert sang hex string
            string hex = priceWei.ToString("X");
            // Pad to even length
            if (hex.Length % 2 != 0)
            {
                hex = "0" + hex;
            }
            return "0x" + hex;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ItemInfoPanel] Lỗi convert GTK to wei: {e.Message}");
            return "0";
        }
    }

    /// <summary>
    /// Convert tokenId sang hex format (nếu chưa phải hex)
    /// </summary>
    private string ConvertTokenIdToHex(string tokenId)
    {
        if (string.IsNullOrEmpty(tokenId))
        {
            return "0x0";
        }

        // Nếu đã là hex format (có "0x"), giữ nguyên
        if (tokenId.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
        {
            return tokenId;
        }

        // Nếu là số int, convert sang hex
        if (int.TryParse(tokenId, out int tokenIdInt))
        {
            return $"0x{tokenIdInt:X}"; // Convert int sang hex với format "0x..."
        }

        // Nếu không parse được, thử parse như hex string không có prefix
        try
        {
            int parsed = Convert.ToInt32(tokenId, 16);
            return $"0x{parsed:X}";
        }
        catch
        {
            Debug.LogWarning($"[ItemInfoPanel] Không thể convert tokenId '{tokenId}' sang hex, dùng giá trị gốc");
            return tokenId;
        }
    }

    /// <summary>
    /// Callback khi withdraw được confirm
    /// </summary>
    private void OnWithdrawConfirmed(int itemID, int quantity, string walletAddress)
    {
        // Xử lý withdraw logic ở đây (sẽ tích hợp blockchain sau)
        Debug.Log($"[ItemInfoPanel] Withdrawing {quantity}x item {itemID} to wallet: {walletAddress}");
        
        if (InventoryManager.Instance == null || WithdrawManager.Instance == null)
        {
            Debug.LogError("[ItemInfoPanel] InventoryManager or WithdrawManager is null!");
            return;
        }

        // Lấy ItemData để kiểm tra loại item
        ItemData itemData = ItemDatabase.Instance?.GetItemByID(itemID);
        if (itemData == null)
        {
            Debug.LogError($"[ItemInfoPanel] Không tìm thấy ItemData với ID: {itemID}");
            return;
        }

        // Xử lý riêng cho CoinItem
        if (itemData is CoinItem coinItem)
        {
            // Với coin, withdraw với quantity cụ thể
            WithdrawManager.Instance.WithdrawCoin(coinItem, quantity);
            // Xóa coin khỏi inventory sau khi withdraw thành công (sẽ xóa sau khi mint thành công trên blockchain)
            // Tạm thời xóa ngay để test
            InventoryManager.Instance.RemoveItem(itemID, quantity);
        }
        else
        {
            // Với NFT items, withdraw như bình thường
            WithdrawManager.Instance.WithdrawItem(itemID);
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

