using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Component để hiển thị một item trong inventory slot
/// </summary>
public class InventoryItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage; // Image để hiển thị icon item
    [SerializeField] private TextMeshProUGUI quantityText; // Text để hiển thị số lượng (optional)
    [SerializeField] private Button button; // Button để click (optional, có thể dùng IPointerClickHandler)

    private int itemID;
    private InventoryItem inventoryItem;
    private ItemData itemData;

    private void Awake()
    {
        // Tự động tìm các component nếu chưa được assign
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
                // Nếu không tìm thấy, lấy Image component đầu tiên
                iconImage = GetComponentInChildren<Image>();
            }
        }

        if (quantityText == null)
        {
            // Tìm TextMeshProUGUI component trong children
            quantityText = GetComponentInChildren<TextMeshProUGUI>();
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

    /// <summary>
    /// Setup item data cho slot này
    /// </summary>
    public void SetupItem(int id, InventoryItem invItem, ItemData data)
    {
        itemID = id;
        inventoryItem = invItem;
        itemData = data;

        UpdateUI();
    }

    /// <summary>
    /// Cập nhật UI hiển thị
    /// </summary>
    private void UpdateUI()
    {
        // Update icon
        if (iconImage != null && itemData != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        // Update quantity text
        if (quantityText != null && inventoryItem != null)
        {
            if (inventoryItem.quantity > 1)
            {
                quantityText.text = inventoryItem.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Refresh UI khi quantity thay đổi
    /// </summary>
    public void RefreshQuantity()
    {
        if (InventoryManager.Instance != null)
        {
            int newQuantity = InventoryManager.Instance.GetItemQuantity(itemID);
            if (inventoryItem != null)
            {
                inventoryItem.quantity = newQuantity;
            }

            UpdateUI();
        }
    }

    /// <summary>
    /// Xử lý khi click vào item slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Gọi InventoryUI để hiển thị info panel
        InventoryUI inventoryUI = FindAnyObjectByType<InventoryUI>();
        if (inventoryUI != null && itemData != null)
        {
            inventoryUI.ShowItemInfo(itemData);
        }
    }
}

