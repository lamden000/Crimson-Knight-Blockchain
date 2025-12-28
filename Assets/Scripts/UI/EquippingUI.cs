using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI để hiển thị và quản lý equipped items
/// </summary>
public class EquippingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject equippingPanel; // Panel chứa toàn bộ equipping UI
    [SerializeField] private ItemInfoPanel itemInfoPanel; // Panel hiển thị thông tin item (dùng chung)
    
    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotUI headSlot;
    [SerializeField] private EquipmentSlotUI bodySlot;
    [SerializeField] private EquipmentSlotUI legsSlot;
    [SerializeField] private EquipmentSlotUI wingSlot;
    [SerializeField] private EquipmentSlotUI weaponSlot;
    [SerializeField] private EquipmentSlotUI feetSlot;

    private Dictionary<EquipmentSlot, EquipmentSlotUI> slotUIs = new Dictionary<EquipmentSlot, EquipmentSlotUI>();
    private Dictionary<EquipmentSlot, EquipmentItem> equippedItems = new Dictionary<EquipmentSlot, EquipmentItem>(); // EquipmentSlot -> EquipmentItem

    private void Awake()
    {
        // Initialize slot dictionary
        if (headSlot != null) slotUIs[EquipmentSlot.Head] = headSlot;
        if (bodySlot != null) slotUIs[EquipmentSlot.Body] = bodySlot;
        if (legsSlot != null) slotUIs[EquipmentSlot.Legs] = legsSlot;
        if (wingSlot != null) slotUIs[EquipmentSlot.Wing] = wingSlot;
        if (weaponSlot != null) slotUIs[EquipmentSlot.Weapon] = weaponSlot;
        if (feetSlot != null) slotUIs[EquipmentSlot.Feet] = feetSlot;

        // Setup slot click handlers
        foreach (var kvp in slotUIs)
        {
            kvp.Value.SetupSlot(kvp.Key, this);
        }
    }

    private void Start()
    {
        // Tự động tìm ItemInfoPanel nếu chưa được assign
        if (itemInfoPanel == null)
        {
            itemInfoPanel = FindAnyObjectByType<ItemInfoPanel>();
        }

        // Load equipped items từ InventoryManager
        StartCoroutine(WaitForInventoryManager());
    }

    private IEnumerator WaitForInventoryManager()
    {
        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        // Đăng ký event để refresh khi inventory được load từ PlayFab
        InventoryManager.Instance.OnInventoryLoaded += OnInventoryLoaded;

        // Load equipped items lần đầu
        RefreshEquippedItems();
    }

    private void OnDestroy()
    {
        // Hủy đăng ký events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryLoaded -= OnInventoryLoaded;
        }
    }

    /// <summary>
    /// Callback khi inventory được load từ PlayFab (sau khi đăng nhập)
    /// </summary>
    private void OnInventoryLoaded()
    {
        // Load lại equipped items sau khi đăng nhập
        RefreshEquippedItems();
        
        // Apply equipped items lên character
        ApplyEquippedItemsToCharacter();
    }

    /// <summary>
    /// Apply tất cả equipped items lên character
    /// </summary>
    private void ApplyEquippedItemsToCharacter()
    {
        PlayerAnimationController animController = GetLocalPlayerAnimationController();
        if (animController == null) return;

        Character character = animController.GetComponent<Character>();
        if (character == null) return;

        foreach (var kvp in equippedItems)
        {
            EquipmentSlot slot = kvp.Key;
            EquipmentItem item = kvp.Value;
            
            if (item != null)
            {
                CharacterPart characterPart = MapEquipmentSlotToCharacterPart(slot, character);
                if (characterPart != CharacterPart.Eyes)
                {
                    animController.SetPart(characterPart, item.variantId);
                }
            }
        }
    }

    /// <summary>
    /// Hiển thị equipping UI (luôn hiển thị khi inventory mở)
    /// </summary>
    public void ShowEquipping()
    {
        if (equippingPanel != null)
        {
            equippingPanel.SetActive(true);
        }
        RefreshEquippedItems();
    }

    /// <summary>
    /// Equip một item vào slot
    /// </summary>
    public void EquipItem(EquipmentItem equipmentItem, int quantity = 1, bool isFromWallet = false)
    {
        if (equipmentItem == null)
        {
            Debug.LogWarning("[EquippingUI] Cannot equip null item!");
            return;
        }

        EquipmentSlot slot = equipmentItem.slot;

        // Nếu slot đã có item, unequip item cũ trước
        if (equippedItems.ContainsKey(slot))
        {
            UnequipItem(slot, false); // false = không refresh UI ngay
        }

        // Thêm item vào equipped
        equippedItems[slot] = equipmentItem;

        // Update UI
        if (slotUIs.ContainsKey(slot))
        {
            slotUIs[slot].SetEquippedItem(equipmentItem);
        }

        // Lưu vào InventoryManager (với flag isFromWallet được truyền vào)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.EquipItem(slot, equipmentItem.variantId, isFromWallet);
        }

        // Apply equipment lên character
        equipmentItem.Use();

        Debug.Log($"[EquippingUI] Equipped {equipmentItem.itemName} to slot {slot} (isFromWallet: {isFromWallet})");
    }

    /// <summary>
    /// Unequip một item từ slot
    /// </summary>
    public void UnequipItem(EquipmentSlot slot, bool refreshUI = true)
    {
        if (!equippedItems.ContainsKey(slot))
        {
            return;
        }

        EquipmentItem item = equippedItems[slot];

        // Xóa khỏi equipped
        equippedItems.Remove(slot);

        // Kiểm tra item có từ wallet không (dùng flag từ InventoryManager)
        bool isFromWallet = false;
        if (InventoryManager.Instance != null)
        {
            isFromWallet = InventoryManager.Instance.IsEquippedItemFromWallet(slot);
        }
        
        if (isFromWallet)
        {
            // Item từ wallet - chỉ cần set default variant, không cần làm gì thêm
            Debug.Log($"[EquippingUI] Unequipping wallet item {item.itemName}, chỉ set default variant");
        }
        else
        {
            // Item từ inventory thường - trả về inventory
            if (InventoryManager.Instance != null && item != null)
            {
                InventoryManager.Instance.AddItem(item, 1);
                Debug.Log($"[EquippingUI] Trả item {item.itemName} về inventory");
            }
        }

        // Set default variant cho slot
        SetDefaultVariant(slot);

        // Update UI
        if (slotUIs.ContainsKey(slot))
        {
            slotUIs[slot].ClearSlot();
        }

        // Lưu vào InventoryManager
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.UnequipItem(slot);
        }

        if (refreshUI)
        {
            RefreshEquippedItems();
        }

        Debug.Log($"[EquippingUI] Unequipped item from slot {slot} (isFromWallet: {isFromWallet})");
    }

    /// <summary>
    /// Set default variant cho slot khi unequip
    /// </summary>
    private void SetDefaultVariant(EquipmentSlot slot)
    {
        PlayerAnimationController animController = GetLocalPlayerAnimationController();
        if (animController == null) return;

        Character character = animController.GetComponent<Character>();
        if (character == null) return;

        int defaultVariant = GetDefaultVariant(slot);
        CharacterPart characterPart = MapEquipmentSlotToCharacterPart(slot, character);

        if (characterPart != CharacterPart.Eyes) // Eyes là invalid marker
        {
            animController.SetPart(characterPart, defaultVariant);
        }
    }

    /// <summary>
    /// Lấy default variant cho slot
    /// </summary>
    private int GetDefaultVariant(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
            case EquipmentSlot.Body:
            case EquipmentSlot.Legs:
                return 0;
            case EquipmentSlot.Wing:
                return -1;
            case EquipmentSlot.Head:
            case EquipmentSlot.Hair:
            case EquipmentSlot.Feet:
                // Giữ nguyên variant hiện tại (tạm thời)
                return -1; // Hoặc có thể return variant hiện tại
            default:
                return -1;
        }
    }

    /// <summary>
    /// Map EquipmentSlot sang CharacterPart
    /// </summary>
    private CharacterPart MapEquipmentSlotToCharacterPart(EquipmentSlot slot, Character character)
    {
        switch (slot)
        {
            case EquipmentSlot.Head:
                return CharacterPart.Head;
            case EquipmentSlot.Body:
                return CharacterPart.Body;
            case EquipmentSlot.Legs:
                return CharacterPart.Legs;
            case EquipmentSlot.Wing:
                return CharacterPart.Wings;
            case EquipmentSlot.Weapon:
                if (character != null)
                {
                    return character.GetWeaponType();
                }
                return CharacterPart.Sword;
            case EquipmentSlot.Hair:
                return CharacterPart.Hair;
            default:
                return CharacterPart.Eyes;
        }
    }

    /// <summary>
    /// Tìm PlayerAnimationController của local player
    /// </summary>
    private PlayerAnimationController GetLocalPlayerAnimationController()
    {
        PlayerAnimationController[] controllers = FindObjectsByType<PlayerAnimationController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            var photonView = controller.GetComponent<Photon.Pun.PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                return controller;
            }
        }
        return null;
    }

    /// <summary>
    /// Refresh tất cả equipped items từ InventoryManager
    /// </summary>
    public void RefreshEquippedItems()
    {
        if (InventoryManager.Instance == null) return;

        var equippedVariants = InventoryManager.Instance.GetAllEquippedItems();
        equippedItems.Clear();

        foreach (var kvp in equippedVariants)
        {
            EquipmentSlot slot = kvp.Key;
            int variantId = kvp.Value;

            // Tìm EquipmentItem có variantId này trong ItemDatabase
            EquipmentItem item = FindEquipmentItemBySlotAndVariant(slot, variantId);
            
            if (item != null)
            {
                equippedItems[slot] = item;
                
                // Update UI
                if (slotUIs.ContainsKey(slot))
                {
                    slotUIs[slot].SetEquippedItem(item);
                }
            }
            else
            {
                // Không tìm thấy item, clear slot
                if (slotUIs.ContainsKey(slot))
                {
                    slotUIs[slot].ClearSlot();
                }
            }
        }

        // Clear các slot không có trong equippedVariants
        foreach (var kvp in slotUIs)
        {
            if (!equippedVariants.ContainsKey(kvp.Key))
            {
                kvp.Value.ClearSlot();
            }
        }
    }

    /// <summary>
    /// Tìm EquipmentItem theo slot và variantId
    /// </summary>
    private EquipmentItem FindEquipmentItemBySlotAndVariant(EquipmentSlot slot, int variantId)
    {
        if (ItemDatabase.Instance == null || ItemDatabase.Instance.allItems == null)
        {
            return null;
        }

        foreach (var itemData in ItemDatabase.Instance.allItems)
        {
            if (itemData is EquipmentItem equipmentItem)
            {
                if (equipmentItem.slot == slot && equipmentItem.variantId == variantId)
                {
                    return equipmentItem;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Xử lý khi click vào slot
    /// </summary>
    public void OnSlotClicked(EquipmentSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            // Hiển thị item info với flag isFromEquipping = true
            if (itemInfoPanel != null)
            {
                itemInfoPanel.ShowItemInfo(equippedItems[slot], isFromWallet: false, isFromEquipping: true);
            }
        }
    }

    /// <summary>
    /// Kiểm tra và remove equipped items không còn trong wallet (được gọi khi load wallet)
    /// </summary>
    public void CheckAndRemoveWalletItems()
    {
        if (WalletInventoryManager.Instance == null || InventoryManager.Instance == null) return;

        // Lấy danh sách equipped items có flag isFromWallet = true
        var equippedFromWallet = InventoryManager.Instance.GetAllEquippedItemsFromWallet();
        var walletNFTs = WalletInventoryManager.Instance.GetAllWalletNFTs();
        
        // Collect tất cả CIDs từ wallet
        HashSet<string> walletCIDs = new HashSet<string>();
        foreach (var nft in walletNFTs.Values)
        {
            if (!string.IsNullOrEmpty(nft.metadataURL))
            {
                string cid = ExtractCIDFromMetadataURL(nft.metadataURL);
                if (!string.IsNullOrEmpty(cid))
                {
                    walletCIDs.Add(cid);
                }
            }
        }

        // Check equipped items có flag isFromWallet = true
        List<EquipmentSlot> slotsToRemove = new List<EquipmentSlot>();
        foreach (var kvp in equippedFromWallet)
        {
            if (kvp.Value) // isFromWallet = true
            {
                EquipmentSlot slot = kvp.Key;
                
                // Lấy item đang equip trong slot này
                if (equippedItems.ContainsKey(slot))
                {
                    EquipmentItem item = equippedItems[slot];
                    if (item != null && !string.IsNullOrEmpty(item.metadataCID))
                    {
                        // Check xem CID của item có còn trong wallet không
                        string cid = item.metadataCID;
                        if (cid.StartsWith("ipfs://"))
                        {
                            cid = cid.Substring(7);
                        }

                        if (!walletCIDs.Contains(cid))
                        {
                            // Item không còn trong wallet, unequip
                            slotsToRemove.Add(slot);
                        }
                    }
                }
            }
        }

        // Unequip các items không còn trong wallet
        foreach (var slot in slotsToRemove)
        {
            UnequipItem(slot, false);
        }

        if (slotsToRemove.Count > 0)
        {
            RefreshEquippedItems();
            Debug.Log($"[EquippingUI] Removed {slotsToRemove.Count} equipped items that are no longer in wallet");
        }
    }

    /// <summary>
    /// Extract CID từ metadata URL
    /// </summary>
    private string ExtractCIDFromMetadataURL(string metadataURL)
    {
        if (string.IsNullOrEmpty(metadataURL)) return "";

        string normalizedURL = metadataURL.Trim();
        
        if (normalizedURL.StartsWith("ipfs://"))
        {
            return normalizedURL.Substring(7);
        }
        else if (normalizedURL.Contains("/ipfs/"))
        {
            int ipfsIndex = normalizedURL.IndexOf("/ipfs/");
            string cid = normalizedURL.Substring(ipfsIndex + 6);
            int slashIndex = cid.IndexOf('/');
            if (slashIndex > 0)
            {
                cid = cid.Substring(0, slashIndex);
            }
            return cid;
        }
        else if (!normalizedURL.Contains("/") && !normalizedURL.Contains(":"))
        {
            return normalizedURL;
        }

        return "";
    }

    // Public property để InventoryUI có thể truy cập
    public GameObject EquippingPanel => equippingPanel;
}


