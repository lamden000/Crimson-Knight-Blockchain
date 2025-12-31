using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

/// <summary>
/// Manager để fetch danh sách items đang được bán từ marketplace contract
/// Sử dụng Alchemy API để query contract events hoặc direct contract calls
/// </summary>
public class MarketplaceDataManager : MonoBehaviour
{
    public static MarketplaceDataManager Instance { get; private set; }

    [Header("Alchemy Settings")]
    [SerializeField] private string alchemyApiKey = "eQMwPXs4A8OF-f9jmKLFD";
    [SerializeField] private string alchemyBaseUrl = "https://polygon-amoy.g.alchemy.com/v2";
    
    [Header("Contract Settings")]
    [SerializeField] private string marketplaceContractAddress = ""; // Địa chỉ Marketplace contract
    [SerializeField] private string nftContractAddress = ""; // Địa chỉ NFT contract (RareItem)
    
    [Header("Settings")]
    [SerializeField] private float refreshCooldown = 5f; // Cooldown giữa các lần refresh (giây)
    
    // Note: Không còn query từ blockchain nữa, chỉ dùng PlayFab để lưu/load listings

    private Dictionary<string, MarketplaceListing> listings = new Dictionary<string, MarketplaceListing>(); // Key: tokenId
    private bool isRefreshing = false;
    private float lastRefreshTime = 0f;

    // Event để UI có thể subscribe
    public System.Action OnMarketplaceDataLoaded;
    public System.Action OnMarketplaceDataRefreshed;

    [System.Serializable]
    public class MarketplaceListing
    {
        public string tokenId;
        public string sellerAddress;
        public string price; // Price in wei (string để tránh overflow)
        public float priceInGTK; // Price converted to GTK (price / 10^18)
        public int itemID; // Mapped từ NFT
        public ItemData itemData; // Reference tới ItemData trong game
    }

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

    private void Start()
    {
        // Đồng bộ contract addresses từ MarketplaceManager nếu có
        StartCoroutine(SyncContractAddressesFromMarketplaceManager());
    }

    /// <summary>
    /// Đồng bộ contract addresses từ MarketplaceManager
    /// </summary>
    private IEnumerator SyncContractAddressesFromMarketplaceManager()
    {
        // Đợi MarketplaceManager được khởi tạo
        while (MarketplaceManager.Instance == null)
        {
            yield return null;
        }

        // Nếu chưa có contract addresses, thử lấy từ MarketplaceManager
        // (MarketplaceManager không có getter methods, nên cần set từ Inspector hoặc code khác)
        // Tạm thời để trống, user sẽ set trong Inspector
    }

    /// <summary>
    /// Refresh marketplace listings từ blockchain
    /// </summary>
    public void RefreshMarketplaceListings()
    {
        // Kiểm tra cooldown
        if (Time.time - lastRefreshTime < refreshCooldown)
        {
            Debug.LogWarning($"[MarketplaceDataManager] Vui lòng đợi {refreshCooldown - (Time.time - lastRefreshTime):F1} giây trước khi refresh lại!");
            return;
        }

        if (isRefreshing)
        {
            Debug.LogWarning("[MarketplaceDataManager] Đang refresh, vui lòng đợi...");
            return;
        }

        if (string.IsNullOrEmpty(marketplaceContractAddress))
        {
            Debug.LogError("[MarketplaceDataManager] Marketplace Contract Address chưa được set!");
            return;
        }

        if (string.IsNullOrEmpty(nftContractAddress))
        {
            Debug.LogError("[MarketplaceDataManager] NFT Contract Address chưa được set!");
            return;
        }

        StartCoroutine(FetchListingsFromBlockchain());
    }

    [Header("Sync Settings")]
    [SerializeField] private bool syncWithBlockchain = true; // Sync với blockchain khi refresh để verify listings
    [SerializeField] private int maxSyncTokenIds = 50; // Số tokenIds tối đa để sync mỗi lần (tránh quá tải)

    /// <summary>
    /// Fetch listings từ PlayFab và sync với blockchain để verify
    /// </summary>
    private IEnumerator FetchListingsFromBlockchain()
    {
        isRefreshing = true;
        lastRefreshTime = Time.time;

        Debug.Log($"[MarketplaceDataManager] Đang fetch listings từ PlayFab...");

        listings.Clear();

        // Load từ PlayFab trước
        if (MarketplacePlayFabManager.Instance != null)
        {
            bool loadComplete = false;
            bool loadSuccess = false;
            
            MarketplacePlayFabManager.Instance.LoadMarketplaceListings((success) =>
            {
                loadSuccess = success;
                loadComplete = true;
            });

            // Đợi load xong (timeout sau 10 giây)
            float timeout = 10f;
            float elapsed = 0f;
            while (!loadComplete && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (loadComplete && loadSuccess)
            {
                // Convert PlayFab listings sang MarketplaceListing
                var playFabListings = MarketplacePlayFabManager.Instance.GetAllActiveListings();
                foreach (var kvp in playFabListings)
                {
                    var playFabListing = kvp.Value;
                    if (playFabListing != null && playFabListing.isActive)
                    {
                        MarketplaceListing listing = new MarketplaceListing
                        {
                            tokenId = playFabListing.tokenId,
                            sellerAddress = playFabListing.sellerAddress,
                            price = playFabListing.price,
                            priceInGTK = playFabListing.priceInGTK,
                            itemID = playFabListing.itemID
                        };

                        // Lấy ItemData
                        if (ItemDatabase.Instance != null)
                        {
                            listing.itemData = ItemDatabase.Instance.GetItemByID(playFabListing.itemID);
                        }

                        listings[playFabListing.tokenId] = listing;
                    }
                }

                Debug.Log($"[MarketplaceDataManager] Đã load {listings.Count} listings từ PlayFab");

                // Sync với blockchain để verify listings còn active không
                if (syncWithBlockchain && listings.Count > 0)
                {
                    Debug.Log($"[MarketplaceDataManager] Đang sync {listings.Count} listings với blockchain...");
                    yield return StartCoroutine(SyncListingsWithBlockchain());
                }
            }
            else
            {
                Debug.LogWarning("[MarketplaceDataManager] Không thể load listings từ PlayFab");
            }
        }
        else
        {
            Debug.LogWarning("[MarketplaceDataManager] MarketplacePlayFabManager chưa được khởi tạo!");
        }

        isRefreshing = false;
        OnMarketplaceDataRefreshed?.Invoke();
    }

    /// <summary>
    /// Sync listings với blockchain: check từng tokenId xem listing còn active không
    /// Nếu không còn trên blockchain (đã bán/cancel), remove khỏi PlayFab
    /// </summary>
    private IEnumerator SyncListingsWithBlockchain()
    {
        if (string.IsNullOrEmpty(marketplaceContractAddress) || !marketplaceContractAddress.StartsWith("0x"))
        {
            Debug.LogWarning("[MarketplaceDataManager] Marketplace contract address không hợp lệ, bỏ qua sync");
            yield break;
        }

        // Lấy danh sách tokenIds từ listings hiện tại
        List<string> tokenIdsToSync = new List<string>(listings.Keys);
        
        // Giới hạn số lượng để tránh quá tải
        if (tokenIdsToSync.Count > maxSyncTokenIds)
        {
            Debug.LogWarning($"[MarketplaceDataManager] Chỉ sync {maxSyncTokenIds}/{tokenIdsToSync.Count} tokenIds để tránh quá tải");
            tokenIdsToSync = tokenIdsToSync.GetRange(0, maxSyncTokenIds);
        }

        int syncedCount = 0;
        int removedCount = 0;

        // Check từng tokenId
        foreach (string tokenId in tokenIdsToSync)
        {
            bool isStillListed = false;
            yield return StartCoroutine(CheckListingStatusOnChain(tokenId, (isListed) =>
            {
                isStillListed = isListed;
            }));

            if (!isStillListed)
            {
                // Listing không còn trên blockchain, remove khỏi PlayFab
                Debug.Log($"[MarketplaceDataManager] Listing {tokenId} không còn trên blockchain, remove khỏi PlayFab");
                MarketplacePlayFabManager.Instance?.RemoveListing(tokenId);
                listings.Remove(tokenId);
                removedCount++;
            }
            else
            {
                syncedCount++;
            }
        }

        Debug.Log($"[MarketplaceDataManager] Sync hoàn tất: {syncedCount} listings còn active, {removedCount} listings đã bị remove");
    }

    /// <summary>
    /// Check xem một tokenId có còn được list trên blockchain không
    /// </summary>
    private IEnumerator CheckListingStatusOnChain(string tokenId, System.Action<bool> onComplete)
    {
        // Convert tokenId sang hex nếu cần
        string tokenIdHex = tokenId;
        if (!tokenIdHex.StartsWith("0x"))
        {
            if (int.TryParse(tokenId, out int tokenIdInt))
            {
                tokenIdHex = "0x" + tokenIdInt.ToString("X");
            }
            else
            {
                onComplete?.Invoke(false);
                yield break;
            }
        }

        string url = $"{alchemyBaseUrl}/{alchemyApiKey}";
        string requestBody = CreateListingQueryRequest(tokenIdHex);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    AlchemyRPCResponse rpcResponse = JsonUtility.FromJson<AlchemyRPCResponse>(jsonResponse);
                    
                    if (rpcResponse != null && rpcResponse.result != null)
                    {
                        // Parse result: (address seller, uint256 price)
                        // Result là hex string: 0x + 64 chars (seller) + 64 chars (price)
                        if (rpcResponse.result.Length >= 130) // 2 + 64 + 64
                        {
                            string sellerHex = "0x" + rpcResponse.result.Substring(2, 64);
                            string priceHex = "0x" + rpcResponse.result.Substring(66, 64);
                            
                            // Check nếu seller không phải zero address và price > 0
                            bool isListed = !IsZeroAddress(sellerHex) && !IsZeroValue(priceHex);
                            onComplete?.Invoke(isListed);
                        }
                        else
                        {
                            onComplete?.Invoke(false);
                        }
                    }
                    else
                    {
                        onComplete?.Invoke(false);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MarketplaceDataManager] Lỗi parse listing status cho tokenId {tokenIdHex}: {e.Message}");
                    onComplete?.Invoke(false);
                }
            }
            else
            {
                Debug.LogWarning($"[MarketplaceDataManager] Lỗi check listing status: {request.error}");
                // Nếu lỗi, giả sử listing vẫn còn (không remove)
                onComplete?.Invoke(true);
            }
        }
    }


    /// <summary>
    /// Tạo JSON-RPC request để query listing
    /// </summary>
    private string CreateListingQueryRequest(string tokenIdHex)
    {
        // Function signature: listings(uint256)
        // Function selector: keccak256("listings(uint256)") -> first 4 bytes
        // Đã tính sẵn: 0x26f4c5e5 (có thể verify bằng web3.js: web3.utils.keccak256("listings(uint256)").substring(0, 10))
        // Parameter: tokenId (padded to 32 bytes = 64 hex chars)
        
        // Pad tokenId to 32 bytes (64 hex chars)
        string cleanTokenId = tokenIdHex.StartsWith("0x") ? tokenIdHex.Substring(2) : tokenIdHex;
        string paddedTokenId = cleanTokenId.PadLeft(64, '0');
        string data = "0x26f4c5e5" + paddedTokenId; // Function selector + parameter
        
        string requestBody = $@"{{
            ""jsonrpc"": ""2.0"",
            ""method"": ""eth_call"",
            ""params"": [
                {{
                    ""to"": ""{marketplaceContractAddress}"",
                    ""data"": ""{data}""
                }},
                ""latest""
            ],
            ""id"": 1
        }}";
        
        return requestBody;
    }

    /// <summary>
    /// Convert wei to GTK (divide by 10^18)
    /// </summary>
    private float ConvertWeiToGTK(string weiHex)
    {
        try
        {
            // Remove 0x prefix
            string cleanHex = weiHex.StartsWith("0x") ? weiHex.Substring(2) : weiHex;
            
            // Parse hex to BigInteger
            System.Numerics.BigInteger wei = System.Numerics.BigInteger.Parse(cleanHex, System.Globalization.NumberStyles.HexNumber);
            
            // Convert to double (wei / 10^18)
            System.Numerics.BigInteger divisor = System.Numerics.BigInteger.Pow(10, 18);
            double gtk = (double)wei / (double)divisor;
            
            return (float)gtk;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MarketplaceDataManager] Lỗi convert wei to GTK: {e.Message}");
            return 0f;
        }
    }

    /// <summary>
    /// Check nếu address là zero address
    /// </summary>
    private bool IsZeroAddress(string address)
    {
        if (string.IsNullOrEmpty(address)) return true;
        string clean = address.StartsWith("0x") ? address.Substring(2) : address;
        return clean.TrimStart('0').Length == 0;
    }

    /// <summary>
    /// Check nếu value là zero
    /// </summary>
    private bool IsZeroValue(string valueHex)
    {
        if (string.IsNullOrEmpty(valueHex)) return true;
        string clean = valueHex.StartsWith("0x") ? valueHex.Substring(2) : valueHex;
        return clean.TrimStart('0').Length == 0;
    }



    /// <summary>
    /// Lấy tất cả listings
    /// </summary>
    public Dictionary<string, MarketplaceListing> GetAllListings()
    {
        return new Dictionary<string, MarketplaceListing>(listings);
    }

    /// <summary>
    /// Kiểm tra có đang refresh không
    /// </summary>
    public bool IsRefreshing()
    {
        return isRefreshing;
    }

    /// <summary>
    /// Set marketplace contract address
    /// </summary>
    public void SetMarketplaceContractAddress(string address)
    {
        marketplaceContractAddress = address;
        Debug.Log($"[MarketplaceDataManager] Đã set marketplace contract address: {address}");
    }

    /// <summary>
    /// Set NFT contract address
    /// </summary>
    public void SetNFTContractAddress(string address)
    {
        nftContractAddress = address;
        Debug.Log($"[MarketplaceDataManager] Đã set NFT contract address: {address}");
    }

    // Alchemy RPC Response Model
    [System.Serializable]
    public class AlchemyRPCResponse
    {
        public string jsonrpc;
        public string result;
        public int id;
        public AlchemyRPCError error;
    }

    [System.Serializable]
    public class AlchemyRPCError
    {
        public int code;
        public string message;
        public object data;
    }

}

