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
    
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I; // Phím để bật/tắt inventory

    private bool isInventoryOpen = false;
    private Dictionary<int, GameObject> itemSlotInstances = new Dictionary<int, GameObject>();

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

        // Đăng ký event từ InventoryManager
        StartCoroutine(WaitForInventoryManager());
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

        // Refresh UI khi mở inventory
        if (isInventoryOpen)
        {
            RefreshInventoryUI();
        }
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
}

