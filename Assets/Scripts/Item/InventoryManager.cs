using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public int itemID;
    public int quantity;
    public bool withdrawable; // Item có thể rút ra blockchain không

    public InventoryItem(int id, int qty, bool canWithdraw)
    {
        itemID = id;
        quantity = qty;
        withdrawable = canWithdraw;
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private Dictionary<int, InventoryItem> inventory = new Dictionary<int, InventoryItem>();
    private Dictionary<EquipmentSlot, int> equippedItems = new Dictionary<EquipmentSlot, int>(); // EquipmentSlot -> variantId
    private Dictionary<EquipmentSlot, bool> equippedItemsFromWallet = new Dictionary<EquipmentSlot, bool>(); // EquipmentSlot -> isFromWallet
    private const string INVENTORY_KEY = "PlayerInventory";
    private const string WALLET_ADDRESS_KEY = "WalletAddress";
    private const string EQUIPPED_ITEMS_KEY = "EquippedItems";

    // Event để UI có thể subscribe
    public System.Action OnInventoryLoaded;
    public System.Action<int> OnItemAdded; // itemID

    private string cachedWalletAddress = null; // Cache wallet address để tránh load nhiều lần

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Load inventory từ PlayFab khi player login
    /// </summary>
    public void LoadInventoryFromPlayFab()
    {
        // Load cả inventory, wallet address và equipped items
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { INVENTORY_KEY, WALLET_ADDRESS_KEY, EQUIPPED_ITEMS_KEY }
        };

        PlayFabClientAPI.GetUserData(
            request,
            HandleUserDataLoaded,
            OnInventoryLoadFailed
        );
    }

    private void HandleUserDataLoaded(GetUserDataResult result)
    {
        // Load inventory
        if (result.Data != null && result.Data.ContainsKey(INVENTORY_KEY))
        {
            string inventoryJson = result.Data[INVENTORY_KEY].Value;
            LoadInventoryFromJson(inventoryJson);
            Debug.Log("[InventoryManager] Inventory loaded from PlayFab");
        }
        else
        {
            inventory.Clear();
            Debug.Log("[InventoryManager] No inventory found, creating new one");
        }

        // Load wallet address
        if (result.Data != null && result.Data.ContainsKey(WALLET_ADDRESS_KEY))
        {
            cachedWalletAddress = result.Data[WALLET_ADDRESS_KEY].Value;
            Debug.Log("[InventoryManager] Wallet address loaded from PlayFab");
        }
        else
        {
            cachedWalletAddress = null;
            Debug.Log("[InventoryManager] No wallet address found in PlayFab");
        }

        // Load equipped items
        if (result.Data != null && result.Data.ContainsKey(EQUIPPED_ITEMS_KEY))
        {
            string equippedItemsJson = result.Data[EQUIPPED_ITEMS_KEY].Value;
            LoadEquippedItemsFromJson(equippedItemsJson);
            Debug.Log("[InventoryManager] Equipped items loaded from PlayFab");
        }
        else
        {
            equippedItems.Clear();
            Debug.Log("[InventoryManager] No equipped items found in PlayFab");
        }

        // Gọi event để UI refresh
        OnInventoryLoaded?.Invoke();
    }

    private void HandleInventoryDataLoaded(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey(INVENTORY_KEY))
        {
            string inventoryJson = result.Data[INVENTORY_KEY].Value;
            LoadInventoryFromJson(inventoryJson);
            Debug.Log("[InventoryManager] Inventory loaded from PlayFab");
        }
        else
        {
            // Chưa có inventory, tạo mới
            inventory.Clear();
            Debug.Log("[InventoryManager] No inventory found, creating new one");
        }

        // Gọi event để UI refresh
        OnInventoryLoaded?.Invoke();
    }

    private void OnInventoryLoadFailed(PlayFabError error)
    {
        Debug.LogError($"[InventoryManager] Failed to load inventory: {error.ErrorMessage}");
        inventory.Clear();

        // Vẫn gọi event để UI biết đã load xong (dù có lỗi)
        OnInventoryLoaded?.Invoke();
    }

    private void LoadInventoryFromJson(string json)
    {
        try
        {
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);
            inventory.Clear();

            if (data.items != null)
            {
                foreach (var item in data.items)
                {
                    inventory[item.itemID] = item;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to parse inventory JSON: {e.Message}");
            inventory.Clear();
        }
    }

    /// <summary>
    /// Thêm item vào inventory và lưu lên PlayFab
    /// </summary>
    public void AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null)
        {
            Debug.LogError("[InventoryManager] Cannot add null item!");
            return;
        }

        if (inventory.ContainsKey(itemData.itemID))
        {
            // Item đã có, tăng quantity
            inventory[itemData.itemID].quantity += quantity;
        }
        else
        {
            // Item mới, thêm vào inventory
            inventory[itemData.itemID] = new InventoryItem(
                itemData.itemID,
                quantity,
                itemData.withdrawable
            );
        }

        // Lưu lên PlayFab
        SaveInventoryToPlayFab();

        // Gọi event để UI refresh
        OnItemAdded?.Invoke(itemData.itemID);

        Debug.Log($"[InventoryManager] Added {quantity}x {itemData.itemName} (ID: {itemData.itemID}) to inventory");
    }

    /// <summary>
    /// Lưu inventory lên PlayFab
    /// </summary>
    private void SaveInventoryToPlayFab()
    {
        if (PlayerDataManager.Instance == null || string.IsNullOrEmpty(PlayerDataManager.Instance.Data?.userId))
        {
            Debug.LogWarning("[InventoryManager] Cannot save to PlayFab: Player not logged in");
            return;
        }

        // Convert inventory thành JSON
        InventoryData data = new InventoryData
        {
            items = inventory.Values.ToList()
        };

        string inventoryJson = JsonUtility.ToJson(data);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { INVENTORY_KEY, inventoryJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            OnInventorySaved,
            OnInventorySaveFailed
        );
    }

    private void OnInventorySaved(UpdateUserDataResult result)
    {
        Debug.Log("[InventoryManager] Inventory saved to PlayFab successfully");
    }

    private void OnInventorySaveFailed(PlayFabError error)
    {
        Debug.LogError($"[InventoryManager] Failed to save inventory to PlayFab: {error.ErrorMessage}");
    }

    /// <summary>
    /// Lấy số lượng item trong inventory
    /// </summary>
    public int GetItemQuantity(int itemID)
    {
        if (inventory.TryGetValue(itemID, out var item))
        {
            return item.quantity;
        }
        return 0;
    }

    /// <summary>
    /// Kiểm tra item có trong inventory không
    /// </summary>
    public bool HasItem(int itemID)
    {
        return inventory.ContainsKey(itemID) && inventory[itemID].quantity > 0;
    }

    /// <summary>
    /// Lấy tất cả items có thể rút (withdrawable)
    /// </summary>
    public List<InventoryItem> GetWithdrawableItems()
    {
        return inventory.Values.Where(item => item.withdrawable && item.quantity > 0).ToList();
    }

    /// <summary>
    /// Xóa item khỏi inventory (khi rút ra blockchain)
    /// </summary>
    public bool RemoveItem(int itemID, int quantity = 1)
    {
        if (!inventory.ContainsKey(itemID))
        {
            return false;
        }

        var item = inventory[itemID];
        if (item.quantity < quantity)
        {
            return false;
        }

        item.quantity -= quantity;
        if (item.quantity <= 0)
        {
            inventory.Remove(itemID);
        }

        // Lưu lên PlayFab
        SaveInventoryToPlayFab();

        return true;
    }

    /// <summary>
    /// Lấy toàn bộ inventory (để hiển thị UI)
    /// </summary>
    public Dictionary<int, InventoryItem> GetAllItems()
    {
        return new Dictionary<int, InventoryItem>(inventory);
    }

    /// <summary>
    /// Lấy wallet address từ PlayFab
    /// </summary>
    public string GetWalletAddress()
    {
        // Nếu đã cache, trả về luôn
        if (!string.IsNullOrEmpty(cachedWalletAddress))
        {
            return cachedWalletAddress;
        }

        // Load từ PlayFab (synchronous - cần implement async hoặc cache)
        // Tạm thời return empty, sẽ load trong LoadInventoryFromPlayFab
        return cachedWalletAddress ?? "";
    }

    /// <summary>
    /// Lưu wallet address lên PlayFab
    /// </summary>
    public void SaveWalletAddress(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[InventoryManager] Cannot save empty wallet address!");
            return;
        }

        if (PlayerDataManager.Instance == null || string.IsNullOrEmpty(PlayerDataManager.Instance.Data?.userId))
        {
            Debug.LogWarning("[InventoryManager] Cannot save wallet address: Player not logged in");
            return;
        }

        // Cache wallet address
        cachedWalletAddress = walletAddress;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { WALLET_ADDRESS_KEY, walletAddress }
            }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            OnWalletAddressSaved,
            OnWalletAddressSaveFailed
        );
    }

    private void OnWalletAddressSaved(UpdateUserDataResult result)
    {
        Debug.Log("[InventoryManager] Wallet address saved to PlayFab successfully");
    }

    private void OnWalletAddressSaveFailed(PlayFabError error)
    {
        Debug.LogError($"[InventoryManager] Failed to save wallet address to PlayFab: {error.ErrorMessage}");
        cachedWalletAddress = null; // Reset cache nếu save failed
    }

    /// <summary>
    /// Load wallet address từ PlayFab (gọi trong LoadInventoryFromPlayFab)
    /// </summary>
    private void LoadWalletAddressFromPlayFab()
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { WALLET_ADDRESS_KEY }
        };

        PlayFabClientAPI.GetUserData(
            request,
            OnWalletAddressLoaded,
            OnWalletAddressLoadFailed
        );
    }

    private void OnWalletAddressLoaded(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey(WALLET_ADDRESS_KEY))
        {
            cachedWalletAddress = result.Data[WALLET_ADDRESS_KEY].Value;
            Debug.Log("[InventoryManager] Wallet address loaded from PlayFab");
        }
        else
        {
            cachedWalletAddress = null;
            Debug.Log("[InventoryManager] No wallet address found in PlayFab");
        }
    }

    private void OnWalletAddressLoadFailed(PlayFabError error)
    {
        Debug.LogError($"[InventoryManager] Failed to load wallet address: {error.ErrorMessage}");
        cachedWalletAddress = null;
    }

    /// <summary>
    /// Lưu equipped item (khi player use equipment)
    /// </summary>
    public void EquipItem(EquipmentSlot slot, int variantId, bool isFromWallet = false)
    {
        equippedItems[slot] = variantId;
        equippedItemsFromWallet[slot] = isFromWallet;
        SaveEquippedItemsToPlayFab();
        Debug.Log($"[InventoryManager] Equipped item: Slot={slot}, Variant={variantId}, IsFromWallet={isFromWallet}");
    }

    /// <summary>
    /// Gỡ equipped item
    /// </summary>
    public void UnequipItem(EquipmentSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            equippedItems.Remove(slot);
            if (equippedItemsFromWallet.ContainsKey(slot))
            {
                equippedItemsFromWallet.Remove(slot);
            }
            SaveEquippedItemsToPlayFab();
            Debug.Log($"[InventoryManager] Unequipped item: Slot={slot}");
        }
    }

    /// <summary>
    /// Lấy variant ID của equipped item trong slot
    /// </summary>
    public int GetEquippedVariant(EquipmentSlot slot)
    {
        if (equippedItems.TryGetValue(slot, out int variantId))
        {
            return variantId;
        }
        return -1; // Không có equipment trong slot này
    }

    /// <summary>
    /// Lấy tất cả equipped items
    /// </summary>
    public Dictionary<EquipmentSlot, int> GetAllEquippedItems()
    {
        return new Dictionary<EquipmentSlot, int>(equippedItems);
    }

    /// <summary>
    /// Kiểm tra item đang equip có từ wallet không
    /// </summary>
    public bool IsEquippedItemFromWallet(EquipmentSlot slot)
    {
        if (equippedItemsFromWallet.TryGetValue(slot, out bool isFromWallet))
        {
            return isFromWallet;
        }
        return false;
    }

    /// <summary>
    /// Lấy tất cả equipped items với flag isFromWallet
    /// </summary>
    public Dictionary<EquipmentSlot, bool> GetAllEquippedItemsFromWallet()
    {
        return new Dictionary<EquipmentSlot, bool>(equippedItemsFromWallet);
    }

    /// <summary>
    /// Load equipped items từ JSON
    /// </summary>
    private void LoadEquippedItemsFromJson(string json)
    {
        try
        {
            EquippedItemsData data = JsonUtility.FromJson<EquippedItemsData>(json);
            equippedItems.Clear();

            if (data.items != null)
            {
                foreach (var item in data.items)
                {
                    if (System.Enum.TryParse<EquipmentSlot>(item.slot, out EquipmentSlot slot))
                    {
                        equippedItems[slot] = item.variantId;
                        // Load flag isFromWallet (mặc định false nếu không có trong data cũ)
                        equippedItemsFromWallet[slot] = item.isFromWallet;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to parse equipped items JSON: {e.Message}");
            equippedItems.Clear();
        }
    }

    /// <summary>
    /// Lưu equipped items lên PlayFab
    /// </summary>
    private void SaveEquippedItemsToPlayFab()
    {
        if (PlayerDataManager.Instance == null || string.IsNullOrEmpty(PlayerDataManager.Instance.Data?.userId))
        {
            Debug.LogWarning("[InventoryManager] Cannot save equipped items to PlayFab: Player not logged in");
            return;
        }

        // Convert equipped items thành JSON
        EquippedItemsData data = new EquippedItemsData
        {
            items = new List<EquippedItemData>()
        };

        foreach (var kvp in equippedItems)
        {
            bool isFromWallet = equippedItemsFromWallet.ContainsKey(kvp.Key) && equippedItemsFromWallet[kvp.Key];
            data.items.Add(new EquippedItemData
            {
                slot = kvp.Key.ToString(),
                variantId = kvp.Value,
                isFromWallet = isFromWallet
            });
        }

        string equippedItemsJson = JsonUtility.ToJson(data);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { EQUIPPED_ITEMS_KEY, equippedItemsJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            OnEquippedItemsSaved,
            OnEquippedItemsSaveFailed
        );
    }

    private void OnEquippedItemsSaved(UpdateUserDataResult result)
    {
        Debug.Log("[InventoryManager] Equipped items saved to PlayFab successfully");
    }

    private void OnEquippedItemsSaveFailed(PlayFabError error)
    {
        Debug.LogError($"[InventoryManager] Failed to save equipped items to PlayFab: {error.ErrorMessage}");
    }

    [System.Serializable]
    public class InventoryData
    {
        public List<InventoryItem> items;
    }

    [System.Serializable]
    public class EquippedItemsData
    {
        public List<EquippedItemData> items;
    }

    [System.Serializable]
    public class EquippedItemData
    {
        public string slot; // EquipmentSlot as string
        public int variantId;
        public bool isFromWallet; // Flag để track item có từ wallet không
    }
}

