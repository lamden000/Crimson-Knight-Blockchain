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
    private const string INVENTORY_KEY = "PlayerInventory";
    private const string WALLET_ADDRESS_KEY = "WalletAddress";

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
        // Load cả inventory và wallet address
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { INVENTORY_KEY, WALLET_ADDRESS_KEY }
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

    [System.Serializable]
    public class InventoryData
    {
        public List<InventoryItem> items;
    }
}

