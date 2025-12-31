using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.DataModels;
using PlayFab.ClientModels;
using System;

/// <summary>
/// Manager để lưu và load marketplace listings từ PlayFab
/// Sử dụng EntityObjects để lưu global marketplace data
/// </summary>
public class MarketplacePlayFabManager : MonoBehaviour
{
    public static MarketplacePlayFabManager Instance { get; private set; }

    private const string MARKETPLACE_OBJECT_KEY = "MarketplaceListings"; // Key trong EntityObjects
    private Dictionary<string, MarketplaceListingData> cachedListings = new Dictionary<string, MarketplaceListingData>(); // Key: tokenId

    // Event để UI có thể subscribe
    public System.Action OnMarketplaceDataLoaded;
    public System.Action OnMarketplaceDataUpdated;

    [System.Serializable]
    public class MarketplaceListingData
    {
        public string tokenId;
        public string sellerAddress;
        public string sellerPlayFabId; // PlayFab ID của seller
        public string price; // Price in wei (string để tránh overflow)
        public float priceInGTK; // Price converted to GTK
        public int itemID;
        public long timestamp; // Unix timestamp khi list
        public string transactionHash; // Transaction hash từ blockchain (nếu có)
        public bool isActive; // Listing còn active không (chưa bán/cancel)
    }

    [System.Serializable]
    public class MarketplaceListingsContainer
    {
        public List<MarketplaceListingData> listings = new List<MarketplaceListingData>();
        public long lastUpdated; // Timestamp của lần update cuối
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

    [Header("Settings")]
    [SerializeField] private bool useTitleData = true; // Dùng Title Data thay vì User Data

    /// <summary>
    /// Load tất cả marketplace listings từ PlayFab Title Data
    /// Title Data là global data, tất cả users đều đọc được
    /// </summary>
    public void LoadMarketplaceListings(System.Action<bool> onComplete = null)
    {
        if (useTitleData)
        {
            // Dùng Title Data (global, read-only từ client)
            var request = new GetTitleDataRequest
            {
                Keys = new List<string> { MARKETPLACE_OBJECT_KEY }
            };

            PlayFabClientAPI.GetTitleData(
                request,
                (result) =>
                {
                    if (result.Data != null && result.Data.ContainsKey(MARKETPLACE_OBJECT_KEY))
                    {
                        string listingsJson = result.Data[MARKETPLACE_OBJECT_KEY];
                        ParseListingsFromJson(listingsJson);
                        onComplete?.Invoke(true);
                        OnMarketplaceDataLoaded?.Invoke();
                    }
                    else
                    {
                        cachedListings.Clear();
                        Debug.Log("[MarketplacePlayFabManager] Không tìm thấy marketplace listings trong Title Data");
                        onComplete?.Invoke(false);
                    }
                },
                (error) =>
                {
                    Debug.LogError($"[MarketplacePlayFabManager] Lỗi load listings từ Title Data: {error.GenerateErrorReport()}");
                    onComplete?.Invoke(false);
                }
            );
        }
        else
        {
            // Fallback: Dùng User Data (legacy)
            var request = new GetUserDataRequest
            {
                Keys = new List<string> { MARKETPLACE_OBJECT_KEY }
            };

            PlayFabClientAPI.GetUserData(
                request,
                (result) =>
                {
                    if (result.Data != null && result.Data.ContainsKey(MARKETPLACE_OBJECT_KEY))
                    {
                        string listingsJson = result.Data[MARKETPLACE_OBJECT_KEY].Value;
                        ParseListingsFromJson(listingsJson);
                        onComplete?.Invoke(true);
                        OnMarketplaceDataLoaded?.Invoke();
                    }
                    else
                    {
                        cachedListings.Clear();
                        Debug.Log("[MarketplacePlayFabManager] Không tìm thấy marketplace listings trong PlayFab");
                        onComplete?.Invoke(false);
                    }
                },
                (error) =>
                {
                    Debug.LogError($"[MarketplacePlayFabManager] Lỗi load listings từ PlayFab: {error.GenerateErrorReport()}");
                    onComplete?.Invoke(false);
                }
            );
        }
    }

    /// <summary>
    /// Parse listings từ JSON
    /// </summary>
    private void ParseListingsFromJson(string json)
    {
        try
        {
            MarketplaceListingsContainer container = JsonUtility.FromJson<MarketplaceListingsContainer>(json);
            
            // Không clear cachedListings ngay, mà merge với listings mới
            // Chỉ clear những listings không còn trong container mới
            if (container != null && container.listings != null)
            {
                // Tạo set các tokenIds trong container mới
                HashSet<string> newTokenIds = new HashSet<string>();
                foreach (var listing in container.listings)
                {
                    if (listing != null && listing.isActive && !string.IsNullOrEmpty(listing.tokenId))
                    {
                        newTokenIds.Add(listing.tokenId);
                        // Update hoặc add listing mới
                        cachedListings[listing.tokenId] = listing;
                    }
                }

                // Remove những listings không còn trong container mới (nếu cần)
                // Nhưng giữ lại những listings mới được add (chưa có trong container)
                // Chỉ remove nếu listing không active và không có trong container mới
                List<string> toRemove = new List<string>();
                foreach (var kvp in cachedListings)
                {
                    if (!newTokenIds.Contains(kvp.Key) && !kvp.Value.isActive)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (string tokenId in toRemove)
                {
                    cachedListings.Remove(tokenId);
                }
            }
            else
            {
                // Nếu container null hoặc listings null, chỉ clear nếu không có listings mới được add
                // Giữ lại cachedListings nếu có (có thể là listings mới chưa được save)
                Debug.LogWarning("[MarketplacePlayFabManager] Container hoặc listings null, giữ lại cachedListings hiện tại");
            }

            Debug.Log($"[MarketplacePlayFabManager] Đã load/merge {cachedListings.Count} active listings từ PlayFab");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MarketplacePlayFabManager] Lỗi parse listings JSON: {e.Message}");
            // Không clear cachedListings nếu parse lỗi, giữ lại data hiện tại
            Debug.LogWarning("[MarketplacePlayFabManager] Giữ lại cachedListings hiện tại do lỗi parse");
        }
    }

    /// <summary>
    /// Thêm listing mới vào PlayFab (khi user list item)
    /// </summary>
    public void AddListing(string tokenId, string sellerAddress, string price, int itemID, string transactionHash = "")
    {
        // Đảm bảo đã load listings từ PlayFab trước (nếu chưa load)
        // Nếu cachedListings trống, có thể chưa load hoặc đã bị clear
        if (cachedListings.Count == 0)
        {
            Debug.LogWarning("[MarketplacePlayFabManager] cachedListings đang trống! Đang load từ PlayFab trước...");
            // Load listings từ PlayFab trước (synchronous wait - có thể cải thiện sau)
            LoadMarketplaceListings((success) =>
            {
                if (success)
                {
                    // Sau khi load xong, thêm listing mới
                    AddListingAfterLoad(tokenId, sellerAddress, price, itemID, transactionHash);
                }
                else
                {
                    // Nếu load fail, vẫn thêm listing mới vào cache
                    Debug.LogWarning("[MarketplacePlayFabManager] Không thể load listings từ PlayFab, thêm listing mới vào cache trống");
                    AddListingAfterLoad(tokenId, sellerAddress, price, itemID, transactionHash);
                }
            });
            return;
        }

        AddListingAfterLoad(tokenId, sellerAddress, price, itemID, transactionHash);
    }

    /// <summary>
    /// Thêm listing sau khi đã đảm bảo cachedListings đã được load
    /// </summary>
    private void AddListingAfterLoad(string tokenId, string sellerAddress, string price, int itemID, string transactionHash = "")
    {
        MarketplaceListingData listing = new MarketplaceListingData
        {
            tokenId = tokenId,
            sellerAddress = sellerAddress,
            sellerPlayFabId = PlayerDataManager.Instance?.Data?.userId ?? "",
            price = price,
            priceInGTK = ConvertWeiToGTK(price),
            itemID = itemID,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            transactionHash = transactionHash,
            isActive = true
        };

        cachedListings[tokenId] = listing;
        Debug.Log($"[MarketplacePlayFabManager] Đã thêm listing vào cache: TokenId={tokenId}, Total listings in cache: {cachedListings.Count}");
        SaveListingsToPlayFab();
    }

    /// <summary>
    /// Remove listing (khi user buy hoặc cancel)
    /// </summary>
    public void RemoveListing(string tokenId)
    {
        if (cachedListings.ContainsKey(tokenId))
        {
            cachedListings[tokenId].isActive = false;
            // Hoặc xóa luôn
            cachedListings.Remove(tokenId);
            SaveListingsToPlayFab();
        }
    }

    /// <summary>
    /// Update listing (ví dụ: update price, status)
    /// </summary>
    public void UpdateListing(string tokenId, MarketplaceListingData updatedListing)
    {
        if (cachedListings.ContainsKey(tokenId))
        {
            cachedListings[tokenId] = updatedListing;
            SaveListingsToPlayFab();
        }
    }

    /// <summary>
    /// Save listings lên PlayFab Title Data
    /// Lưu ý: Client không thể write Title Data trực tiếp, cần dùng CloudScript hoặc Server API
    /// </summary>
    private void SaveListingsToPlayFab()
    {
        // Debug: Kiểm tra cachedListings trước khi save
        Debug.Log($"[MarketplacePlayFabManager] Đang save {cachedListings.Count} listings lên PlayFab...");
        
        MarketplaceListingsContainer container = new MarketplaceListingsContainer
        {
            listings = new List<MarketplaceListingData>(cachedListings.Values),
            lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string listingsJson = JsonUtility.ToJson(container);
        
        // Debug: Log JSON để kiểm tra
        Debug.Log($"[MarketplacePlayFabManager] JSON sẽ được lưu: {listingsJson}");
        Debug.Log($"[MarketplacePlayFabManager] Số listings trong container: {container.listings.Count}");

        if (useTitleData)
        {
            // Dùng CloudScript để update Title Data (vì client không thể write Title Data trực tiếp)
            ExecuteCloudScriptToUpdateTitleData(listingsJson);
        }
        else
        {
            // Fallback: Dùng User Data (legacy)
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { MARKETPLACE_OBJECT_KEY, listingsJson }
                }
            };

            PlayFabClientAPI.UpdateUserData(
                request,
                (result) =>
                {
                    Debug.Log("[MarketplacePlayFabManager] Đã lưu listings lên PlayFab User Data");
                    OnMarketplaceDataUpdated?.Invoke();
                },
                (error) =>
                {
                    Debug.LogError($"[MarketplacePlayFabManager] Lỗi lưu listings lên PlayFab: {error.GenerateErrorReport()}");
                }
            );
        }
    }

    /// <summary>
    /// Execute CloudScript để update Title Data
    /// CloudScript function: "UpdateMarketplaceListings"
    /// </summary>
    private void ExecuteCloudScriptToUpdateTitleData(string listingsJson)
    {
        // Tạo FunctionParameter object
        var functionParams = new Dictionary<string, object>
        {
            { "key", MARKETPLACE_OBJECT_KEY },
            { "value", listingsJson }
        };

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UpdateMarketplaceListings",
            FunctionParameter = functionParams
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            (result) =>
            {
                if (result.Error != null)
                {
                    Debug.LogError($"[MarketplacePlayFabManager] CloudScript error: {result.Error.Error}");
                    Debug.LogError($"Error Message: {result.Error.Message}");
                    Debug.LogWarning("[MarketplacePlayFabManager] Fallback: Thử lưu vào User Data...");
                    // Fallback: Lưu vào User Data nếu CloudScript fail
                    SaveToUserDataFallback(listingsJson);
                }
                else
                {
                    Debug.Log("[MarketplacePlayFabManager] Đã lưu listings lên Title Data qua CloudScript");
                    if (result.FunctionResult != null)
                    {
                        Debug.Log($"[MarketplacePlayFabManager] CloudScript result: {result.FunctionResult}");
                    }
                    OnMarketplaceDataUpdated?.Invoke();
                }
            },
            (error) =>
            {
                Debug.LogError($"[MarketplacePlayFabManager] Lỗi execute CloudScript: {error.GenerateErrorReport()}");
                Debug.LogWarning("[MarketplacePlayFabManager] Fallback: Thử lưu vào User Data...");
                // Fallback: Lưu vào User Data nếu CloudScript fail
                SaveToUserDataFallback(listingsJson);
            }
        );
    }

    /// <summary>
    /// Fallback: Lưu vào User Data nếu CloudScript không available
    /// </summary>
    private void SaveToUserDataFallback(string listingsJson)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { MARKETPLACE_OBJECT_KEY, listingsJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            (result) =>
            {
                Debug.Log("[MarketplacePlayFabManager] Đã lưu listings lên User Data (fallback)");
                OnMarketplaceDataUpdated?.Invoke();
            },
            (error) =>
            {
                Debug.LogError($"[MarketplacePlayFabManager] Lỗi lưu listings lên User Data: {error.GenerateErrorReport()}");
            }
        );
    }

    /// <summary>
    /// Lấy tất cả active listings
    /// </summary>
    public Dictionary<string, MarketplaceListingData> GetAllActiveListings()
    {
        Dictionary<string, MarketplaceListingData> activeListings = new Dictionary<string, MarketplaceListingData>();
        
        foreach (var kvp in cachedListings)
        {
            if (kvp.Value != null && kvp.Value.isActive)
            {
                activeListings[kvp.Key] = kvp.Value;
            }
        }
        
        return activeListings;
    }

    /// <summary>
    /// Lấy listing theo tokenId
    /// </summary>
    public MarketplaceListingData GetListing(string tokenId)
    {
        if (cachedListings.ContainsKey(tokenId) && cachedListings[tokenId].isActive)
        {
            return cachedListings[tokenId];
        }
        return null;
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
            Debug.LogWarning($"[MarketplacePlayFabManager] Lỗi convert wei to GTK: {e.Message}");
            return 0f;
        }
    }

    /// <summary>
    /// Sync với blockchain để verify listings (optional, có thể chạy background)
    /// </summary>
    public void SyncWithBlockchain(System.Action<int> onComplete = null)
    {
        // Query blockchain để verify listings còn active không
        // Nếu listing không còn trên blockchain, mark as inactive
        // Có thể chạy background job để sync định kỳ
        
        Debug.Log("[MarketplacePlayFabManager] Sync với blockchain...");
        // TODO: Implement blockchain sync logic
        onComplete?.Invoke(0);
    }
}

