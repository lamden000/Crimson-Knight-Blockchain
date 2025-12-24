using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialog để withdraw item ra blockchain
/// </summary>
public class WithdrawDialog : DialogBase
{
    [Header("UI References")]
    [SerializeField] private Slider quantitySlider; // Slider để chọn số lượng
    [SerializeField] private TextMeshProUGUI minText; // Text hiển thị min (luôn = 1)
    [SerializeField] private TextMeshProUGUI maxText; // Text hiển thị max (số lượng item)
    [SerializeField] private TextMeshProUGUI currentText; // Text hiển thị giá trị slider hiện tại
    [SerializeField] private TMP_InputField walletAddressInput; // Input field để nhập địa chỉ ví
    [SerializeField] private Button closeButton; // Button để đóng dialog
    [SerializeField] private Button confirmButton; // Button để confirm withdraw

    private ItemData itemData;
    private int maxQuantity;
    private Action<int, int, string> onConfirmCallback; // itemID, quantity, walletAddress
    private bool isWalletAddressLocked = false; // Địa chỉ ví đã được lưu chưa

    private void Awake()
    {
        // Tự động tìm các component nếu chưa được assign
        if (quantitySlider == null)
        {
            quantitySlider = GetComponentInChildren<Slider>();
        }

        if (minText == null)
        {
            Transform minTransform = transform.Find("MinText");
            if (minTransform != null)
            {
                minText = minTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (maxText == null)
        {
            Transform maxTransform = transform.Find("MaxText");
            if (maxTransform != null)
            {
                maxText = maxTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (currentText == null)
        {
            Transform currentTransform = transform.Find("CurrentText");
            if (currentTransform != null)
            {
                currentText = currentTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (walletAddressInput == null)
        {
            walletAddressInput = GetComponentInChildren<TMP_InputField>();
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
        // Setup slider listener
        if (quantitySlider != null)
        {
            quantitySlider.onValueChanged.RemoveAllListeners();
            quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

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
    }

    /// <summary>
    /// Setup dialog với item data
    /// </summary>
    public void Setup(ItemData data, int maxQty, Action<int, int, string> onConfirm)
    {
        itemData = data;
        maxQuantity = maxQty;
        onConfirmCallback = onConfirm;

        // Setup slider
        if (quantitySlider != null)
        {
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = maxQuantity;
            quantitySlider.value = 1;
            quantitySlider.wholeNumbers = true; // Chỉ cho phép số nguyên
        }

        // Update texts
        if (minText != null)
        {
            minText.text = "1";
        }

        if (maxText != null)
        {
            maxText.text = maxQuantity.ToString();
        }

        UpdateCurrentText(1);

        // Load wallet address từ PlayFab
        LoadWalletAddress();
    }

    /// <summary>
    /// Load wallet address từ PlayFab
    /// </summary>
    private void LoadWalletAddress()
    {
        if (InventoryManager.Instance != null)
        {
            string walletAddress = InventoryManager.Instance.GetWalletAddress();
            
            if (!string.IsNullOrEmpty(walletAddress))
            {
                // Đã có wallet address, set readonly
                isWalletAddressLocked = true;
                if (walletAddressInput != null)
                {
                    walletAddressInput.text = walletAddress;
                    walletAddressInput.readOnly = true;
                    walletAddressInput.interactable = false;
                }
            }
            else
            {
                // Chưa có wallet address, cho phép nhập
                isWalletAddressLocked = false;
                if (walletAddressInput != null)
                {
                    walletAddressInput.text = "";
                    walletAddressInput.readOnly = false;
                    walletAddressInput.interactable = true;
                }
            }
        }
    }

    /// <summary>
    /// Xử lý khi slider value thay đổi
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        UpdateCurrentText(intValue);
    }

    /// <summary>
    /// Cập nhật text hiển thị giá trị hiện tại
    /// </summary>
    private void UpdateCurrentText(int value)
    {
        if (currentText != null)
        {
            currentText.text = value.ToString();
        }
    }

    /// <summary>
    /// Xử lý khi click confirm button
    /// </summary>
    private void OnConfirmClicked()
    {
        if (itemData == null)
        {
            Debug.LogError("[WithdrawDialog] ItemData is null!");
            return;
        }

        int quantity = Mathf.RoundToInt(quantitySlider != null ? quantitySlider.value : 1);
        string walletAddress = walletAddressInput != null ? walletAddressInput.text : "";

        // Validate wallet address
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[WithdrawDialog] Wallet address is empty!");
            // Có thể hiển thị error message cho user
            return;
        }

        // Nếu chưa có wallet address, lưu vào PlayFab
        if (!isWalletAddressLocked && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SaveWalletAddress(walletAddress);
            isWalletAddressLocked = true;
            
            // Lock input field
            if (walletAddressInput != null)
            {
                walletAddressInput.readOnly = true;
                walletAddressInput.interactable = false;
            }
        }

        // Gọi callback
        onConfirmCallback?.Invoke(itemData.itemID, quantity, walletAddress);

        // Đóng dialog
        Close();
    }

    public override void Show()
    {
        base.Show();
        // Reset slider về 1 khi show lại
        if (quantitySlider != null)
        {
            quantitySlider.value = 1;
        }
    }
}

