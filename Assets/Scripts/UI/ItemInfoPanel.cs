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
    }

    private ItemData currentItemData;
    private bool currentItemIsFromWallet = false; // Track item hiện tại có từ wallet không
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
    public void ShowItemInfo(ItemData itemData, bool isFromWallet = false, bool isFromEquipping = false)
    {
        if (itemData == null)
        {
            Debug.LogWarning("[ItemInfoPanel] Cannot show info for null item!");
            return;
        }

        currentItemData = itemData;
        currentItemIsFromWallet = isFromWallet; // Lưu flag isFromWallet

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

