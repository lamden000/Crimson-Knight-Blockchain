using System;
using System.Collections;
using System.IO;
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
    [SerializeField] private TMP_InputField walletAddressInput; // Input field để hiển thị địa chỉ ví (readonly)
    [SerializeField] private Button linkWalletButton; // Button để link wallet (chỉ hiển thị khi chưa có ví)
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

        if (linkWalletButton == null)
        {
            Transform linkWalletTransform = transform.Find("LinkWalletButton");
            if (linkWalletTransform != null)
            {
                linkWalletButton = linkWalletTransform.GetComponent<Button>();
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
        // Setup slider listener
        if (quantitySlider != null)
        {
            quantitySlider.onValueChanged.RemoveAllListeners();
            quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // Setup buttons
        if (linkWalletButton != null)
        {
            linkWalletButton.onClick.RemoveAllListeners();
            linkWalletButton.onClick.AddListener(OnLinkWalletClicked);
        }

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

        // Load wallet address từ PlayFab và update UI
        LoadWalletAddress();
    }

    /// <summary>
    /// Load wallet address từ PlayFab và update UI
    /// </summary>
    private void LoadWalletAddress()
    {
        if (InventoryManager.Instance != null)
        {
            string walletAddress = InventoryManager.Instance.GetWalletAddress();
            
            if (!string.IsNullOrEmpty(walletAddress))
            {
                // Đã có wallet address
                isWalletAddressLocked = true;
                if (walletAddressInput != null)
                {
                    walletAddressInput.text = walletAddress;
                    walletAddressInput.readOnly = true;
                    walletAddressInput.interactable = false;
                }

                // Ẩn nút Link Wallet (đã có ví rồi)
                if (linkWalletButton != null)
                {
                    linkWalletButton.gameObject.SetActive(false);
                }

                // Enable confirm button
                if (confirmButton != null)
                {
                    confirmButton.interactable = true;
                }
            }
            else
            {
                // Chưa có wallet address
                isWalletAddressLocked = false;
                if (walletAddressInput != null)
                {
                    walletAddressInput.text = "Chưa liên kết ví";
                    walletAddressInput.readOnly = true;
                    walletAddressInput.interactable = false;
                }

                // Hiển thị nút Link Wallet
                if (linkWalletButton != null)
                {
                    linkWalletButton.gameObject.SetActive(true);
                }

                // Disable confirm button (cần link wallet trước)
                if (confirmButton != null)
                {
                    confirmButton.interactable = false;
                }
            }
        }
    }

    /// <summary>
    /// Xử lý khi click Link Wallet button
    /// </summary>
    private void OnLinkWalletClicked()
    {
        // Mở trang link wallet
        if (WithdrawManager.Instance != null)
        {
            WithdrawManager.Instance.OpenLinkWalletPage();
            
            // Bắt đầu check file wallet address
            StartCoroutine(CheckWalletAddressFile());
        }
        else
        {
            Debug.LogError("[WithdrawDialog] WithdrawManager.Instance is null!");
        }
    }

    /// <summary>
    /// Coroutine để check file wallet address sau khi user link wallet
    /// </summary>
    private System.Collections.IEnumerator CheckWalletAddressFile()
    {
        string downloadsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Downloads";
        string walletFilePath = Path.Combine(downloadsPath, "wallet_address.txt");

        float timeout = 60f; // Timeout sau 60 giây
        float elapsed = 0f;
        float checkInterval = 0.5f; // Check mỗi 0.5 giây

        while (elapsed < timeout)
        {
            if (File.Exists(walletFilePath))
            {
                try
                {
                    string walletAddress = File.ReadAllText(walletFilePath).Trim();
                    
                    if (!string.IsNullOrEmpty(walletAddress))
                    {
                        Debug.Log($"[WithdrawDialog] Đã đọc wallet address từ file: {walletAddress}");
                        
                        // Lưu vào PlayFab
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.SaveWalletAddress(walletAddress);
                        }

                        // Xóa file sau khi đọc
                        File.Delete(walletFilePath);

                        // Update UI
                        LoadWalletAddress();

                        yield break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WithdrawDialog] Lỗi đọc file wallet address: {e.Message}");
                }
            }

            yield return new WaitForSeconds(checkInterval);
            elapsed += checkInterval;
        }

        Debug.LogWarning("[WithdrawDialog] Timeout khi chờ file wallet address!");
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
        
        // Lấy wallet address từ PlayFab (đã được lưu khi link wallet)
        string walletAddress = InventoryManager.Instance?.GetWalletAddress() ?? "";

        // Validate wallet address
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("[WithdrawDialog] Wallet address is empty! Vui lòng link wallet trước.");
            return;
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

