using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialog để nhập giá bán item trên marketplace
/// </summary>
public class SellItemDialog : DialogBase
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI itemNameText; // Text hiển thị tên item
    [SerializeField] private Image itemIconImage; // Image hiển thị icon item
    [SerializeField] private TMP_InputField priceInput; // Input field để nhập giá bán
    [SerializeField] private TextMeshProUGUI tokenIdText; // Text hiển thị token ID
    [SerializeField] private Button closeButton; // Button để đóng dialog
    [SerializeField] private Button confirmButton; // Button để confirm bán

    private ItemData itemData;
    private WalletInventoryManager.WalletNFT walletNFT;
    private Action<string, string> onConfirmCallback; // tokenId, price

    private void Awake()
    {
        // Tự động tìm các component nếu chưa được assign
        if (itemNameText == null)
        {
            Transform nameTransform = transform.Find("ItemNameText");
            if (nameTransform != null)
            {
                itemNameText = nameTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (itemIconImage == null)
        {
            Transform iconTransform = transform.Find("ItemIcon");
            if (iconTransform != null)
            {
                itemIconImage = iconTransform.GetComponent<Image>();
            }
        }

        if (priceInput == null)
        {
            priceInput = GetComponentInChildren<TMP_InputField>();
        }

        if (tokenIdText == null)
        {
            Transform tokenIdTransform = transform.Find("TokenIdText");
            if (tokenIdTransform != null)
            {
                tokenIdText = tokenIdTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (closeButton == null)
        {
            Transform closeTransform = transform.Find("CloseButton");
            if (closeTransform != null)
            {
                closeButton = closeTransform.GetComponent<Button>();
            }
        }

        if (confirmButton == null)
        {
            Transform confirmTransform = transform.Find("ConfirmButton");
            if (confirmTransform != null)
            {
                confirmButton = confirmTransform.GetComponent<Button>();
            }
        }
    }

    private void Start()
    {
        // Setup buttons
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        // Setup price input validation
        if (priceInput != null)
        {
            priceInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            priceInput.onValueChanged.AddListener(OnPriceInputChanged);
        }
    }

    /// <summary>
    /// Setup dialog với item data và wallet NFT
    /// </summary>
    public void Setup(ItemData data, WalletInventoryManager.WalletNFT nft, Action<string, string> onConfirm)
    {
        itemData = data;
        walletNFT = nft;
        onConfirmCallback = onConfirm;

        // Update UI
        if (itemNameText != null && itemData != null)
        {
            itemNameText.text = itemData.itemName ?? "Unknown Item";
        }

        if (itemIconImage != null && itemData != null && itemData.icon != null)
        {
            itemIconImage.sprite = itemData.icon;
            itemIconImage.enabled = true;
        }
        else if (itemIconImage != null)
        {
            itemIconImage.enabled = false;
        }

        if (tokenIdText != null && walletNFT != null && !string.IsNullOrEmpty(walletNFT.tokenId))
        {
            // Convert hex string to int for display
            int tokenIdInt = ConvertHexToInt(walletNFT.tokenId);
            tokenIdText.text = $"Token ID: {tokenIdInt}";
        }
        else if (tokenIdText != null)
        {
            tokenIdText.text = "Token ID: N/A";
        }

        // Reset price input
        if (priceInput != null)
        {
            priceInput.text = "";
        }
    }

    /// <summary>
    /// Xử lý khi price input thay đổi
    /// </summary>
    private void OnPriceInputChanged(string value)
    {
        // Validate price (phải là số nguyên dương)
        if (confirmButton != null)
        {
            int price = 0;
            bool isValid = int.TryParse(value, out price) && price > 0;
            confirmButton.interactable = isValid;
        }
    }

    /// <summary>
    /// Xử lý khi click confirm button
    /// </summary>
    private void OnConfirmClicked()
    {
        if (itemData == null || walletNFT == null || string.IsNullOrEmpty(walletNFT.tokenId))
        {
            Debug.LogError("[SellItemDialog] ItemData, WalletNFT, hoặc tokenId is null!");
            return;
        }

        // Validate price
        string priceText = priceInput != null ? priceInput.text : "";
        if (string.IsNullOrEmpty(priceText))
        {
            Debug.LogWarning("[SellItemDialog] Price is empty!");
            return;
        }

        int price = 0;
        if (!int.TryParse(priceText, out price) || price <= 0)
        {
            Debug.LogWarning("[SellItemDialog] Price phải là số nguyên dương!");
            return;
        }

        // Gọi callback với tokenId và price
        onConfirmCallback?.Invoke(walletNFT.tokenId, price.ToString());

        // Đóng dialog
        Close();
    }

    /// <summary>
    /// Convert hex string to int
    /// </summary>
    private int ConvertHexToInt(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
        {
            return 0;
        }

        try
        {
            // Remove "0x" prefix if present
            string cleanHex = hexString;
            if (cleanHex.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
            {
                cleanHex = cleanHex.Substring(2);
            }

            // Convert hex to int
            return Convert.ToInt32(cleanHex, 16);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SellItemDialog] Không thể convert hex '{hexString}' sang int: {e.Message}");
            // Try parsing as int directly
            if (int.TryParse(hexString, out int result))
            {
                return result;
            }
            return 0;
        }
    }
}

