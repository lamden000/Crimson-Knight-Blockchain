using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI component cho một equipment slot
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage; // Image để hiển thị icon item (child object)
    [SerializeField] private TextMeshProUGUI slotNameText; // Text hiển thị tên slot (optional)
    [SerializeField] private GameObject emptyIndicator; // Hiển thị khi slot trống (optional)

    private EquipmentSlot slot;
    private EquippingUI equippingUI;
    private EquipmentItem currentItem;

    private void Awake()
    {
        // Tự động tìm iconImage trong children nếu chưa được assign
        if (iconImage == null)
        {
            // Tìm Image component trong children (có thể có tên "Icon", "ItemIcon", etc.)
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform == null)
            {
                iconTransform = transform.Find("ItemIcon");
            }
            if (iconTransform == null)
            {
                // Tìm Image component đầu tiên trong children
                iconImage = GetComponentInChildren<Image>();
            }
            else
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
        }

        // Tự động tìm slotNameText nếu chưa được assign
        if (slotNameText == null)
        {
            Transform nameTransform = transform.Find("SlotNameText");
            if (nameTransform != null)
            {
                slotNameText = nameTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Tự động tìm emptyIndicator nếu chưa được assign
        if (emptyIndicator == null)
        {
            Transform emptyTransform = transform.Find("EmptyIndicator");
            if (emptyTransform != null)
            {
                emptyIndicator = emptyTransform.gameObject;
            }
        }
    }

    /// <summary>
    /// Setup slot với slot type và equipping UI reference
    /// </summary>
    public void SetupSlot(EquipmentSlot slotType, EquippingUI ui)
    {
        slot = slotType;
        equippingUI = ui;

        // Setup slot name
        if (slotNameText != null)
        {
            slotNameText.text = slotType.ToString();
        }

        ClearSlot();
    }

    /// <summary>
    /// Set equipped item vào slot
    /// </summary>
    public void SetEquippedItem(EquipmentItem item)
    {
        currentItem = item;

        if (iconImage != null)
        {
            if (item != null && item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
            else
            {
                // Nếu item hoặc icon null, set sprite = null
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        // Ẩn empty indicator nếu có
        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Clear slot (khi unequip hoặc slot trống)
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;

        if (iconImage != null)
        {
            // Set sprite = null khi slot trống
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        // Hiển thị empty indicator nếu có
        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// Xử lý khi click vào slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (equippingUI != null)
        {
            equippingUI.OnSlotClicked(slot);
        }
    }

    /// <summary>
    /// Lấy equipment item hiện tại trong slot
    /// </summary>
    public EquipmentItem GetCurrentItem()
    {
        return currentItem;
    }

    /// <summary>
    /// Kiểm tra slot có trống không
    /// </summary>
    public bool IsEmpty()
    {
        return currentItem == null;
    }
}

