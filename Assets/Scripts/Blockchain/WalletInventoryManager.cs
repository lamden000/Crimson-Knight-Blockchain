using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

/// <summary>
/// Manager để quản lý NFT từ blockchain (wallet inventory)
/// Sử dụng Alchemy API để đọc NFT từ ví của người chơi
/// </summary>
public class WalletInventoryManager : MonoBehaviour
{
    public static WalletInventoryManager Instance { get; private set; }

    [Header("Alchemy Settings")]
    [SerializeField] private string alchemyApiKey = "eQMwPXs4A8OF-f9jmKLFD";
    [SerializeField] private string alchemyBaseUrl = "https://polygon-amoy.g.alchemy.com/v2";
    [SerializeField] private string rareItemContractAddress = "0x02DF0ccd422e6126C0Fd30a203B950eB0015d08A";

    [Header("Settings")]
    [SerializeField] private float refreshCooldown = 5f; // Cooldown giữa các lần refresh (giây)

    private Dictionary<int, WalletNFT> walletNFTs = new Dictionary<int, WalletNFT>();
    private bool isRefreshing = false;
    private float lastRefreshTime = 0f;

    // Event để UI có thể subscribe
    public System.Action OnWalletInventoryLoaded;
    public System.Action OnWalletInventoryRefreshed;

    [System.Serializable]
    public class WalletNFT
    {
        public string tokenId;
        public string tokenURI;
        public string metadataURL;
        public int itemID; // Mapped từ metadata
        public ItemData itemData; // Reference tới ItemData trong game
        public string contractAddress;
        public string ownerAddress;
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

    /// <summary>
    /// Refresh wallet inventory từ blockchain
    /// </summary>
    public void RefreshWalletInventory()
    {
        // Kiểm tra cooldown
        if (Time.time - lastRefreshTime < refreshCooldown)
        {
            Debug.LogWarning($"[WalletInventoryManager] Vui lòng đợi {refreshCooldown - (Time.time - lastRefreshTime):F1} giây trước khi refresh lại!");
            return;
        }

        if (isRefreshing)
        {
            Debug.LogWarning("[WalletInventoryManager] Đang refresh, vui lòng đợi...");
            return;
        }

        // Lấy wallet address từ InventoryManager
        string walletAddress = InventoryManager.Instance?.GetWalletAddress();
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogError("[WalletInventoryManager] Wallet address chưa được set! Vui lòng liên kết ví trước.");
            return;
        }

        StartCoroutine(FetchNFTsFromAlchemy(walletAddress));
    }

    /// <summary>
    /// Fetch NFT từ Alchemy API
    /// </summary>
    private IEnumerator FetchNFTsFromAlchemy(string walletAddress)
    {
        isRefreshing = true;
        lastRefreshTime = Time.time;

        Debug.Log($"[WalletInventoryManager] Đang fetch NFT từ Alchemy cho địa chỉ: {walletAddress}");

        // Alchemy API endpoint để get NFTs
        string url = $"{alchemyBaseUrl}/{alchemyApiKey}/getNFTs?owner={walletAddress}&contractAddresses[]={rareItemContractAddress}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"[WalletInventoryManager] Alchemy API response length: {jsonResponse.Length}");

                    // Parse JSON response từ Alchemy
                    AlchemyNFTResponse response = JsonUtility.FromJson<AlchemyNFTResponse>(jsonResponse);
                    
                    if (response != null && response.ownedNfts != null && response.ownedNfts.Length > 0)
                    {
                        List<AlchemyNFT> nfts = AlchemyNFT.FromWrapper(response.ownedNfts);
                        ProcessNFTs(nfts, walletAddress);
                    }
                    else
                    {
                        Debug.LogWarning("[WalletInventoryManager] Không tìm thấy NFT nào trong response");
                        walletNFTs.Clear();
                        OnWalletInventoryRefreshed?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WalletInventoryManager] Lỗi parse JSON: {e.Message}");
                    Debug.LogError($"Stack trace: {e.StackTrace}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"[WalletInventoryManager] Lỗi khi fetch NFT: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }

        isRefreshing = false;
    }

    /// <summary>
    /// Xử lý danh sách NFT từ Alchemy
    /// </summary>
    private void ProcessNFTs(List<AlchemyNFT> nfts, string walletAddress)
    {
        walletNFTs.Clear();

        foreach (var nft in nfts)
        {
            if (nft == null) continue;

            WalletNFT walletNFT = new WalletNFT
            {
                tokenId = nft.id?.tokenId,
                tokenURI = nft.tokenUri?.raw ?? nft.tokenUri?.gateway ?? "",
                contractAddress = nft.contract?.address ?? rareItemContractAddress,
                ownerAddress = walletAddress
            };

            // Lấy metadata URL
            if (!string.IsNullOrEmpty(walletNFT.tokenURI))
            {
                walletNFT.metadataURL = walletNFT.tokenURI;
            }

            // Map metadata URL về itemID trong game
            walletNFT.itemID = MapMetadataToItemID(walletNFT.metadataURL);
            
            if (walletNFT.itemID > 0)
            {
                // Lấy ItemData từ ItemDatabase
                if (ItemDatabase.Instance != null)
                {
                    walletNFT.itemData = ItemDatabase.Instance.GetItemByID(walletNFT.itemID);
                }

                // Parse tokenId từ hex string (0x...) thành int
                int tokenIdInt = 0;
                try
                {
                    if (!string.IsNullOrEmpty(walletNFT.tokenId))
                    {
                        // Remove "0x" prefix nếu có
                        string tokenIdStr = walletNFT.tokenId;
                        if (tokenIdStr.StartsWith("0x") || tokenIdStr.StartsWith("0X"))
                        {
                            tokenIdStr = tokenIdStr.Substring(2);
                        }
                        
                        // Parse hex string
                        tokenIdInt = int.Parse(tokenIdStr, System.Globalization.NumberStyles.HexNumber);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WalletInventoryManager] Không thể parse tokenId '{walletNFT.tokenId}': {e.Message}");
                    // Dùng index làm tokenId nếu parse fail
                    tokenIdInt = walletNFTs.Count + 1;
                }

                walletNFTs[tokenIdInt] = walletNFT;
            }
            else
            {
                Debug.LogWarning($"[WalletInventoryManager] Không thể map metadata URL: {walletNFT.metadataURL}");
            }
        }

        Debug.Log($"[WalletInventoryManager] Đã load {walletNFTs.Count} NFT từ wallet");
        OnWalletInventoryRefreshed?.Invoke();
    }

    /// <summary>
    /// Map metadata URL về itemID trong game
    /// Logic: Tìm ItemData có metadataCID khớp với metadataURL
    /// </summary>
    private int MapMetadataToItemID(string metadataURL)
    {
        if (string.IsNullOrEmpty(metadataURL) || ItemDatabase.Instance == null)
        {
            return 0;
        }

        // Normalize metadata URL (có thể là ipfs://, https://, hoặc chỉ CID)
        string normalizedURL = metadataURL.Trim();
        
        // Nếu là IPFS, extract CID
        string cid = "";
        if (normalizedURL.StartsWith("ipfs://"))
        {
            cid = normalizedURL.Substring(7); // Bỏ "ipfs://"
        }
        else if (normalizedURL.StartsWith("https://ipfs.io/ipfs/") || normalizedURL.StartsWith("https://gateway.pinata.cloud/ipfs/"))
        {
            // Extract CID từ URL
            int ipfsIndex = normalizedURL.IndexOf("/ipfs/");
            if (ipfsIndex >= 0)
            {
                cid = normalizedURL.Substring(ipfsIndex + 6);
                // Remove trailing path nếu có
                int slashIndex = cid.IndexOf('/');
                if (slashIndex > 0)
                {
                    cid = cid.Substring(0, slashIndex);
                }
            }
        }
        else if (!normalizedURL.Contains("/") && !normalizedURL.Contains(":"))
        {
            // Có thể là CID trực tiếp
            cid = normalizedURL;
        }

        // Tìm ItemData có metadataCID khớp
        if (ItemDatabase.Instance != null && ItemDatabase.Instance.allItems != null)
        {
            foreach (var itemData in ItemDatabase.Instance.allItems)
            {
                if (itemData == null) continue;

                string itemCID = itemData.metadataCID;
                if (string.IsNullOrEmpty(itemCID)) continue;

                // Normalize item CID
                if (itemCID.StartsWith("ipfs://"))
                {
                    itemCID = itemCID.Substring(7);
                }

                // So sánh CID
                if (itemCID.Equals(cid, StringComparison.OrdinalIgnoreCase))
                {
                    return itemData.itemID;
                }

                // Hoặc so sánh toàn bộ URL nếu khớp
                if (itemData.metadataCID.Equals(normalizedURL, StringComparison.OrdinalIgnoreCase) ||
                    itemData.metadataCID.Equals(metadataURL, StringComparison.OrdinalIgnoreCase))
                {
                    return itemData.itemID;
                }
            }
        }

        return 0; // Không tìm thấy
    }

    /// <summary>
    /// Lấy tất cả NFT từ wallet
    /// </summary>
    public Dictionary<int, WalletNFT> GetAllWalletNFTs()
    {
        return new Dictionary<int, WalletNFT>(walletNFTs);
    }

    /// <summary>
    /// Lấy số lượng NFT của một itemID
    /// </summary>
    public int GetNFTCountByItemID(int itemID)
    {
        int count = 0;
        foreach (var nft in walletNFTs.Values)
        {
            if (nft.itemID == itemID)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Kiểm tra có đang refresh không
    /// </summary>
    public bool IsRefreshing()
    {
        return isRefreshing;
    }

    // Alchemy API Response Models
    [System.Serializable]
    public class AlchemyNFTResponse
    {
        public AlchemyNFTWrapper[] ownedNfts;
        public string pageKey;
        public int totalCount;
    }

    [System.Serializable]
    public class AlchemyNFTWrapper
    {
        public AlchemyContract contract;
        public AlchemyTokenId id;
        public AlchemyTokenUri tokenUri;
        public string title;
        public string description;
        public AlchemyMetadata metadata;
    }

    // Wrapper để convert array thành List
    public class AlchemyNFT
    {
        public AlchemyContract contract;
        public AlchemyTokenId id;
        public AlchemyTokenUri tokenUri;
        public string title;
        public string description;
        public AlchemyMetadata metadata;

        public static List<AlchemyNFT> FromWrapper(AlchemyNFTWrapper[] wrappers)
        {
            List<AlchemyNFT> nfts = new List<AlchemyNFT>();
            if (wrappers != null)
            {
                foreach (var wrapper in wrappers)
                {
                    nfts.Add(new AlchemyNFT
                    {
                        contract = wrapper.contract,
                        id = wrapper.id,
                        tokenUri = wrapper.tokenUri,
                        title = wrapper.title,
                        description = wrapper.description,
                        metadata = wrapper.metadata
                    });
                }
            }
            return nfts;
        }
    }

    [System.Serializable]
    public class AlchemyContract
    {
        public string address;
    }

    [System.Serializable]
    public class AlchemyTokenId
    {
        public string tokenId;
    }

    [System.Serializable]
    public class AlchemyTokenUri
    {
        public string raw;
        public string gateway;
    }

    [System.Serializable]
    public class AlchemyMetadata
    {
        public string name;
        public string description;
        public string image;
    }
}

