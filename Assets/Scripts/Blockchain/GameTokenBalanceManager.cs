using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Numerics;

/// <summary>
/// Manager để quản lý số dư Game Token (GTK) từ blockchain
/// Sử dụng Alchemy API để đọc token balance
/// </summary>
public class GameTokenBalanceManager : MonoBehaviour
{
    public static GameTokenBalanceManager Instance { get; private set; }

    [Header("Alchemy Settings")]
    [SerializeField] private string alchemyApiKey = "eQMwPXs4A8OF-f9jmKLFD";
    [SerializeField] private string alchemyBaseUrl = "https://polygon-amoy.g.alchemy.com/v2";

    [Header("Token Settings")]
    [SerializeField] private string gameTokenContractAddress = ""; // Địa chỉ contract GameToken (SpiritShard/GTK)
    [SerializeField] private int tokenDecimals = 18; // Decimals của token (ERC20 thường là 18)

    private void Start()
    {
        // Tự động lấy contract address từ WithdrawManager nếu có
        StartCoroutine(SyncContractAddressFromWithdrawManager());
    }

    /// <summary>
    /// Đồng bộ contract address từ WithdrawManager
    /// </summary>
    private IEnumerator SyncContractAddressFromWithdrawManager()
    {
        // Đợi WithdrawManager được khởi tạo
        while (WithdrawManager.Instance == null)
        {
            yield return null;
        }

        // Nếu chưa có contract address, thử lấy từ WithdrawManager
        if (string.IsNullOrEmpty(gameTokenContractAddress))
        {
            string address = WithdrawManager.Instance.GetGameTokenContractAddress();
            if (!string.IsNullOrEmpty(address))
            {
                gameTokenContractAddress = address;
                Debug.Log($"[GameTokenBalanceManager] Đã đồng bộ contract address từ WithdrawManager: {address}");
            }
        }
    }

    [Header("Settings")]
    [SerializeField] private float refreshCooldown = 5f; // Cooldown giữa các lần refresh (giây)

    private float currentBalance = 0f; // Số dư token hiện tại (đã convert từ wei)
    private bool isRefreshing = false;
    private float lastRefreshTime = 0f;

    // Event để UI có thể subscribe
    public System.Action<float> OnBalanceUpdated; // balance (float)

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
    /// Refresh token balance từ blockchain
    /// </summary>
    public void RefreshBalance()
    {
        // Kiểm tra cooldown
        if (Time.time - lastRefreshTime < refreshCooldown)
        {
            Debug.LogWarning($"[GameTokenBalanceManager] Vui lòng đợi {refreshCooldown - (Time.time - lastRefreshTime):F1} giây trước khi refresh lại!");
            return;
        }

        if (isRefreshing)
        {
            Debug.LogWarning("[GameTokenBalanceManager] Đang refresh, vui lòng đợi...");
            return;
        }

        // Kiểm tra contract address
        if (string.IsNullOrEmpty(gameTokenContractAddress))
        {
            Debug.LogError("[GameTokenBalanceManager] GameToken Contract Address chưa được set!");
            return;
        }

        // Lấy wallet address từ InventoryManager
        string walletAddress = InventoryManager.Instance?.GetWalletAddress();
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogError("[GameTokenBalanceManager] Wallet address chưa được set! Vui lòng liên kết ví trước.");
            return;
        }

        StartCoroutine(FetchTokenBalance(walletAddress));
    }

    /// <summary>
    /// Fetch token balance từ Alchemy API
    /// </summary>
    private IEnumerator FetchTokenBalance(string walletAddress)
    {
        isRefreshing = true;
        lastRefreshTime = Time.time;

        Debug.Log($"[GameTokenBalanceManager] Đang fetch token balance từ Alchemy cho địa chỉ: {walletAddress}");

        // Alchemy API endpoint để get token balances
        // https://docs.alchemy.com/reference/gettokenbalances
         // Alchemy sử dụng JSON-RPC, endpoint chỉ là base URL + API key
        string url = $"{alchemyBaseUrl}/{alchemyApiKey}";

        // Tạo request body (JSON thủ công vì Unity JsonUtility không hỗ trợ nested arrays tốt)
        // Alchemy API format: {"jsonrpc":"2.0","method":"alchemy_getTokenBalances","params":["0x...", ["0x..."]],"id":1}
        string contractAddressJson = $"[\"{gameTokenContractAddress}\"]";
        string jsonBody = $"{{\"jsonrpc\":\"2.0\",\"method\":\"alchemy_getTokenBalances\",\"params\":[\"{walletAddress}\",{contractAddressJson}],\"id\":1}}";
       

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"[GameTokenBalanceManager] Alchemy API response: {jsonResponse}");

                    // Parse JSON response
                    TokenBalanceResponse response = JsonUtility.FromJson<TokenBalanceResponse>(jsonResponse);

                    if (response != null && response.result != null && response.result.tokenBalances != null && response.result.tokenBalances.Length > 0)
                    {
                        // Lấy balance của token đầu tiên (nếu có nhiều token)
                        string balanceHex = response.result.tokenBalances[0].tokenBalance;
                        
                        // Convert hex string (0x...) thành decimal
                        if (!string.IsNullOrEmpty(balanceHex))
                        {
                            // Remove "0x" prefix
                            string balanceStr = balanceHex;
                            if (balanceStr.StartsWith("0x") || balanceStr.StartsWith("0X"))
                            {
                                balanceStr = balanceStr.Substring(2);
                            }

                            // Parse hex to BigInteger (để tránh overflow với số lớn)
                            BigInteger balanceWei = 0;
                            try
                            {
                                // Parse hex string thành BigInteger
                                balanceWei = ParseHexToBigInteger(balanceStr);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[GameTokenBalanceManager] Lỗi parse hex string '{balanceHex}': {ex.Message}");
                                currentBalance = 0f;
                                OnBalanceUpdated?.Invoke(0f);
                                yield break;
                            }

                            // Convert từ wei sang token (chia cho 10^decimals)
                            // BigInteger không có phép chia trực tiếp với float, cần convert sang double
                            double balanceWeiDouble = (double)balanceWei;
                            double divisor = Math.Pow(10, tokenDecimals);
                            currentBalance = (float)(balanceWeiDouble / divisor);

                            Debug.Log($"[GameTokenBalanceManager] Token balance: {currentBalance} GTK (wei: {balanceWei}, hex: {balanceHex})");
                            
                            // Gọi event
                            OnBalanceUpdated?.Invoke(currentBalance);
                        }
                        else
                        {
                            Debug.LogWarning("[GameTokenBalanceManager] Token balance is empty");
                            currentBalance = 0f;
                            OnBalanceUpdated?.Invoke(0f);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[GameTokenBalanceManager] Không tìm thấy token balance trong response");
                        currentBalance = 0f;
                        OnBalanceUpdated?.Invoke(0f);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameTokenBalanceManager] Lỗi parse JSON: {e.Message}");
                    Debug.LogError($"Stack trace: {e.StackTrace}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"[GameTokenBalanceManager] Lỗi khi fetch token balance: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }

        isRefreshing = false;
    }

    /// <summary>
    /// Parse hex string thành BigInteger (hỗ trợ số lớn không giới hạn)
    /// </summary>
    private BigInteger ParseHexToBigInteger(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return BigInteger.Zero;
        }

        // Remove leading zeros
        hex = hex.TrimStart('0');
        if (string.IsNullOrEmpty(hex))
        {
            return BigInteger.Zero;
        }

        BigInteger result = BigInteger.Zero;
        BigInteger multiplier = BigInteger.One;

        // Parse từ phải sang trái (từ LSB đến MSB)
        for (int i = hex.Length - 1; i >= 0; i--)
        {
            char c = hex[i];
            int digit = 0;
            
            if (c >= '0' && c <= '9')
                digit = c - '0';
            else if (c >= 'A' && c <= 'F')
                digit = c - 'A' + 10;
            else if (c >= 'a' && c <= 'f')
                digit = c - 'a' + 10;
            else
            {
                throw new FormatException($"Invalid hex character: {c}");
            }

            result += digit * multiplier;
            multiplier *= 16;
        }

        return result;
    }

    /// <summary>
    /// Lấy số dư token hiện tại (cached)
    /// </summary>
    public float GetCurrentBalance()
    {
        return currentBalance;
    }

    /// <summary>
    /// Kiểm tra có đang refresh không
    /// </summary>
    public bool IsRefreshing()
    {
        return isRefreshing;
    }

    /// <summary>
    /// Set game token contract address
    /// </summary>
    public void SetGameTokenContractAddress(string address)
    {
        gameTokenContractAddress = address;
        Debug.Log($"[GameTokenBalanceManager] Đã set game token contract address: {address}");
    }

    // Alchemy API Response Models
    [System.Serializable]
    public class TokenBalanceResponse
    {
        public TokenBalanceResult result;
        public int id;
    }

    [System.Serializable]
    public class TokenBalanceResult
    {
        public string address;
        public TokenBalance[] tokenBalances;
    }

    [System.Serializable]
    public class TokenBalance
    {
        public string contractAddress;
        public string tokenBalance;
        public int error;
    }
}

