using UnityEngine;
using System.IO;

/// <summary>
/// Manager để xử lý marketplace operations (bán và mua items)
/// Mở trình duyệt với trang HTML để người dùng ký giao dịch bằng MetaMask
/// </summary>
public class MarketplaceManager : MonoBehaviour
{
    public static MarketplaceManager Instance;

    [Header("Settings")]
    [SerializeField] private string marketplaceContractAddress = ""; // Địa chỉ Marketplace contract
    [SerializeField] private string nftContractAddress = ""; // Địa chỉ NFT contract (RareItem)
    [SerializeField] private string gameTokenContractAddress = ""; // Địa chỉ GameToken contract (để thanh toán)
    [SerializeField] private string marketplaceWebPath = "BlockchainWeb/sell-item.html"; // Đường dẫn tới file HTML bán
    [SerializeField] private string buyItemWebPath = "BlockchainWeb/buy-item.html"; // Đường dẫn tới file HTML mua
    [SerializeField] private bool useLocalhost = true; // Sử dụng localhost:8000 thay vì file://
    [SerializeField] private int localhostPort = 8000; // Port cho localhost server

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
    /// Mở trang bán item trong trình duyệt
    /// </summary>
    /// <param name="tokenId">Token ID của NFT muốn bán</param>
    /// <param name="price">Giá bán (trong game token)</param>
    public void OpenSellItemPage(string tokenId, string price)
    {
        string htmlPath = GetSellItemHTMLPath();
        
        if (string.IsNullOrEmpty(htmlPath))
        {
            Debug.LogError("[MarketplaceManager] Không tìm thấy file sell-item.html!");
            return;
        }

        // Validate contract addresses
        if (string.IsNullOrEmpty(marketplaceContractAddress))
        {
            Debug.LogError("[MarketplaceManager] Marketplace Contract Address chưa được set!");
            return;
        }

        if (string.IsNullOrEmpty(nftContractAddress))
        {
            Debug.LogError("[MarketplaceManager] NFT Contract Address chưa được set!");
            return;
        }

        if (string.IsNullOrEmpty(gameTokenContractAddress))
        {
            Debug.LogError("[MarketplaceManager] GameToken Contract Address chưa được set!");
            return;
        }

        // Tạo URL
        string url;
        if (useLocalhost)
        {
            url = $"http://localhost:{localhostPort}/sell-item.html";
            Debug.Log($"[MarketplaceManager] Sử dụng localhost. Đảm bảo HTTP server đang chạy tại port {localhostPort}!");
        }
        else
        {
            string normalizedPath = htmlPath.Replace('\\', '/');
            url = $"file:///{normalizedPath}";
            Debug.LogWarning($"[MarketplaceManager] Đang dùng file:// - MetaMask có thể không hoạt động trên Chrome!");
        }

        // Thêm parameters với URL encoding để tránh lỗi
        url += $"?marketplace={UnityEngine.Networking.UnityWebRequest.EscapeURL(marketplaceContractAddress)}&nft={UnityEngine.Networking.UnityWebRequest.EscapeURL(nftContractAddress)}&token={UnityEngine.Networking.UnityWebRequest.EscapeURL(gameTokenContractAddress)}&tokenId={UnityEngine.Networking.UnityWebRequest.EscapeURL(tokenId)}&price={UnityEngine.Networking.UnityWebRequest.EscapeURL(price)}";

        Debug.Log($"[MarketplaceManager] Mở trình duyệt để bán item: {url}");

        // Mở trình duyệt
        Application.OpenURL(url);
    }

    /// <summary>
    /// Mở trang mua item trong trình duyệt
    /// </summary>
    /// <param name="tokenId">Token ID của NFT muốn mua</param>
    /// <param name="price">Giá mua (trong game token)</param>
    public void OpenBuyItemPage(string tokenId, string price)
    {
        string htmlPath = GetBuyItemHTMLPath();
        
        if (string.IsNullOrEmpty(htmlPath))
        {
            Debug.LogError("[MarketplaceManager] Không tìm thấy file buy-item.html!");
            return;
        }

        // Validate contract addresses
        if (string.IsNullOrEmpty(marketplaceContractAddress))
        {
            Debug.LogError("[MarketplaceManager] Marketplace Contract Address chưa được set!");
            return;
        }

        if (string.IsNullOrEmpty(nftContractAddress))
        {
            Debug.LogError("[MarketplaceManager] NFT Contract Address chưa được set!");
            return;
        }

        if (string.IsNullOrEmpty(gameTokenContractAddress))
        {
            Debug.LogError("[MarketplaceManager] GameToken Contract Address chưa được set!");
            return;
        }

        // Tạo URL
        string url;
        if (useLocalhost)
        {
            url = $"http://localhost:{localhostPort}/buy-item.html";
            Debug.Log($"[MarketplaceManager] Sử dụng localhost. Đảm bảo HTTP server đang chạy tại port {localhostPort}!");
        }
        else
        {
            string normalizedPath = htmlPath.Replace('\\', '/');
            url = $"file:///{normalizedPath}";
            Debug.LogWarning($"[MarketplaceManager] Đang dùng file:// - MetaMask có thể không hoạt động trên Chrome!");
        }

        // Thêm parameters với URL encoding để tránh lỗi
        url += $"?marketplace={UnityEngine.Networking.UnityWebRequest.EscapeURL(marketplaceContractAddress)}&nft={UnityEngine.Networking.UnityWebRequest.EscapeURL(nftContractAddress)}&token={UnityEngine.Networking.UnityWebRequest.EscapeURL(gameTokenContractAddress)}&tokenId={UnityEngine.Networking.UnityWebRequest.EscapeURL(tokenId)}&price={UnityEngine.Networking.UnityWebRequest.EscapeURL(price)}";

        Debug.Log($"[MarketplaceManager] Mở trình duyệt để mua item: {url}");

        // Mở trình duyệt
        Application.OpenURL(url);
    }

    /// <summary>
    /// Lấy đường dẫn tuyệt đối tới file sell-item.html
    /// </summary>
    private string GetSellItemHTMLPath()
    {
        string fileName = Path.GetFileName(marketplaceWebPath); // "sell-item.html"
        string folderName = Path.GetDirectoryName(marketplaceWebPath).Replace('\\', '/'); // "MarketplaceWeb"
        
        string[] possiblePaths = {
            Path.Combine(Application.streamingAssetsPath, folderName, fileName),
            Path.Combine(Application.dataPath, "..", folderName, fileName),
            Path.Combine(Application.dataPath, marketplaceWebPath),
            Path.Combine(Application.dataPath, "..", marketplaceWebPath),
            Path.Combine(Directory.GetCurrentDirectory(), marketplaceWebPath),
            Path.Combine(Directory.GetCurrentDirectory(), folderName, fileName),
            Path.Combine(Path.GetDirectoryName(Application.dataPath), folderName, fileName)
        };

        foreach (string path in possiblePaths)
        {
            string normalizedPath = Path.GetFullPath(path);
            if (File.Exists(normalizedPath))
            {
                Debug.Log($"[MarketplaceManager] Tìm thấy sell-item.html tại: {normalizedPath}");
                return normalizedPath;
            }
        }

        Debug.LogWarning($"[MarketplaceManager] Không tìm thấy file sell-item.html!");
        return null;
    }

    /// <summary>
    /// Lấy đường dẫn tuyệt đối tới file buy-item.html
    /// </summary>
    private string GetBuyItemHTMLPath()
    {
        string fileName = Path.GetFileName(buyItemWebPath); // "buy-item.html"
        string folderName = Path.GetDirectoryName(buyItemWebPath).Replace('\\', '/'); // "MarketplaceWeb"
        
        string[] possiblePaths = {
            Path.Combine(Application.streamingAssetsPath, folderName, fileName),
            Path.Combine(Application.dataPath, "..", folderName, fileName),
            Path.Combine(Application.dataPath, buyItemWebPath),
            Path.Combine(Application.dataPath, "..", buyItemWebPath),
            Path.Combine(Directory.GetCurrentDirectory(), buyItemWebPath),
            Path.Combine(Directory.GetCurrentDirectory(), folderName, fileName),
            Path.Combine(Path.GetDirectoryName(Application.dataPath), folderName, fileName)
        };

        foreach (string path in possiblePaths)
        {
            string normalizedPath = Path.GetFullPath(path);
            if (File.Exists(normalizedPath))
            {
                Debug.Log($"[MarketplaceManager] Tìm thấy buy-item.html tại: {normalizedPath}");
                return normalizedPath;
            }
        }

        Debug.LogWarning($"[MarketplaceManager] Không tìm thấy file buy-item.html!");
        return null;
    }

    /// <summary>
    /// Set marketplace contract address
    /// </summary>
    public void SetMarketplaceContractAddress(string address)
    {
        marketplaceContractAddress = address;
        Debug.Log($"[MarketplaceManager] Đã set marketplace contract address: {address}");
    }

    /// <summary>
    /// Set NFT contract address
    /// </summary>
    public void SetNFTContractAddress(string address)
    {
        nftContractAddress = address;
        Debug.Log($"[MarketplaceManager] Đã set NFT contract address: {address}");
    }

    /// <summary>
    /// Set game token contract address
    /// </summary>
    public void SetGameTokenContractAddress(string address)
    {
        gameTokenContractAddress = address;
        Debug.Log($"[MarketplaceManager] Đã set game token contract address: {address}");
    }

    /// <summary>
    /// Callback khi user list item thành công trên blockchain
    /// Có thể gọi từ HTML page hoặc sau khi verify transaction
    /// </summary>
    public void OnItemListedSuccess(string tokenId, string sellerAddress, string price, int itemID, string transactionHash = "")
    {
        // Lưu listing vào PlayFab
        if (MarketplacePlayFabManager.Instance != null)
        {
            MarketplacePlayFabManager.Instance.AddListing(tokenId, sellerAddress, price, itemID, transactionHash);
            Debug.Log($"[MarketplaceManager] Đã lưu listing vào PlayFab: TokenId={tokenId}, Price={price}");
        }
        else
        {
            Debug.LogWarning("[MarketplaceManager] MarketplacePlayFabManager chưa được khởi tạo!");
        }
    }

    /// <summary>
    /// Callback khi user buy item thành công
    /// </summary>
    public void OnItemBoughtSuccess(string tokenId)
    {
        // Remove listing khỏi PlayFab
        if (MarketplacePlayFabManager.Instance != null)
        {
            MarketplacePlayFabManager.Instance.RemoveListing(tokenId);
            Debug.Log($"[MarketplaceManager] Đã remove listing khỏi PlayFab: TokenId={tokenId}");
        }
    }

    /// <summary>
    /// Callback khi user cancel listing thành công
    /// </summary>
    public void OnListingCancelledSuccess(string tokenId)
    {
        // Remove listing khỏi PlayFab
        if (MarketplacePlayFabManager.Instance != null)
        {
            MarketplacePlayFabManager.Instance.RemoveListing(tokenId);
            Debug.Log($"[MarketplaceManager] Đã cancel listing trong PlayFab: TokenId={tokenId}");
        }
    }
}

