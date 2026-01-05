using UnityEngine;
using System.IO;

/// <summary>
/// Manager để xử lý withdraw item từ game lên blockchain
/// Mở trình duyệt với trang HTML để người dùng ký giao dịch bằng MetaMask
/// </summary>
public class WithdrawManager : MonoBehaviour
{
    public static WithdrawManager Instance;

    [Header("Settings")]
    [SerializeField] private string defaultContractAddress = ""; // Địa chỉ contract mặc định (fallback nếu ItemData không có)
    [SerializeField] private string gameTokenContractAddress = ""; // Địa chỉ contract GameToken (SpiritShard/GTK)
    [SerializeField] private string withdrawWebPath = "BlockchainWeb/index.html"; // Đường dẫn tới file HTML
    [SerializeField] private string withdrawCoinWebPath = "BlockchainWeb/withdraw-coin.html"; // Đường dẫn tới file withdraw coin HTML
    [SerializeField] private string linkWalletWebPath = "BlockchainWeb/link-wallet.html"; // Đường dẫn tới file link wallet HTML
    [SerializeField] private bool useLocalhost = true; // Sử dụng localhost:8000 thay vì file:// (khuyến nghị)
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
    /// Mở trang withdraw trong trình duyệt
    /// </summary>
    /// <param name="itemID">ID của item muốn withdraw</param>
    /// <param name="tokenURI">Token URI cho NFT (metadata)</param>
    /// <param name="contractAddress">Contract address (optional, sẽ dùng default nếu không có)</param>
    public void OpenWithdrawPage(int itemID, string tokenURI = "", string contractAddress = "")
    {
        // Lấy đường dẫn tuyệt đối tới file HTML
        string htmlPath = GetHTMLPath();
        
        if (string.IsNullOrEmpty(htmlPath))
        {
            Debug.LogError("[WithdrawManager] Không tìm thấy file HTML!");
            return;
        }

        // Lấy contract address (ưu tiên parameter, sau đó default)
        string contractAddr = contractAddress;
        if (string.IsNullOrEmpty(contractAddr))
        {
            contractAddr = defaultContractAddress;
        }

        // Tạo URL - ưu tiên localhost nếu được bật
        string url;
        if (useLocalhost)
        {
            // Sử dụng localhost (khuyến nghị - MetaMask hoạt động tốt hơn)
            url = $"http://localhost:{localhostPort}/index.html";
            Debug.Log($"[WithdrawManager] Sử dụng localhost. Đảm bảo HTTP server đang chạy tại port {localhostPort}!");
            Debug.Log($"[WithdrawManager] Chạy: cd WithdrawWeb && python -m http.server {localhostPort}");
        }
        else
        {
            // Sử dụng file:// (có thể không hoạt động với MetaMask trên Chrome)
            string normalizedPath = htmlPath.Replace('\\', '/');
            url = $"file:///{normalizedPath}";
            Debug.LogWarning($"[WithdrawManager] Đang dùng file:// - MetaMask có thể không hoạt động trên Chrome!");
        }

        // Thêm parameters
        if (!string.IsNullOrEmpty(contractAddr) || !string.IsNullOrEmpty(tokenURI))
        {
            url += "?";
            bool firstParam = true;
            
            if (!string.IsNullOrEmpty(contractAddr))
            {
                url += $"contract={contractAddr}";
                firstParam = false;
            }
            
            if (!string.IsNullOrEmpty(tokenURI))
            {
                if (!firstParam) url += "&";
                url += $"uri={UnityEngine.Networking.UnityWebRequest.EscapeURL(tokenURI)}";
            }
        }

        Debug.Log($"[WithdrawManager] Mở trình duyệt: {url}");

        // Mở trình duyệt
        Application.OpenURL(url);
    }

    /// <summary>
    /// Withdraw một item từ inventory lên blockchain (sử dụng ItemData)
    /// </summary>
    /// <param name="itemData">ItemData của item muốn withdraw</param>
    public void WithdrawItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("[WithdrawManager] ItemData is null!");
            return;
        }

        // Kiểm tra item có thể withdraw không
        if (!itemData.withdrawable)
        {
            Debug.LogWarning($"[WithdrawManager] Item '{itemData.itemName}' (ID: {itemData.itemID}) không thể withdraw!");
            return;
        }

        // Xử lý riêng cho CoinItem (currency)
        if (itemData is CoinItem coinItem)
        {
            WithdrawCoin(coinItem);
            return;
        }

        // Lấy contract address từ ItemData, fallback về default nếu không có
        string contractAddr = itemData.nftContractAddress;
        if (string.IsNullOrEmpty(contractAddr))
        {
            contractAddr = defaultContractAddress;
        }

        if (string.IsNullOrEmpty(contractAddr))
        {
            Debug.LogError($"[WithdrawManager] Contract Address chưa được set cho item '{itemData.itemName}' (ID: {itemData.itemID})!");
            return;
        }

        // Lấy token URI từ metadataCID
        string tokenURI = "";
        if (!string.IsNullOrEmpty(itemData.metadataCID))
        {
            // Nếu CID chưa có prefix, thêm ipfs://
            if (itemData.metadataCID.StartsWith("ipfs://") || itemData.metadataCID.StartsWith("http://") || itemData.metadataCID.StartsWith("https://"))
            {
                tokenURI = itemData.metadataCID;
            }
            else
            {
                tokenURI = $"ipfs://{itemData.metadataCID}";
            }
        }
        else
        {
            Debug.LogWarning($"[WithdrawManager] MetadataCID chưa được set cho item '{itemData.itemName}' (ID: {itemData.itemID})!");
            // Fallback: tạo từ itemID
            tokenURI = $"ipfs://QmPlaceholder/{itemData.itemID}";
        }

        Debug.Log($"[WithdrawManager] Withdraw item: {itemData.itemName} (ID: {itemData.itemID})");
        Debug.Log($"[WithdrawManager] Contract: {contractAddr}, TokenURI: {tokenURI}");

        // Mở trang withdraw với contract address và token URI
        OpenWithdrawPage(itemData.itemID, tokenURI, contractAddr);
    }

    /// <summary>
    /// Withdraw một item từ inventory lên blockchain (sử dụng itemID)
    /// </summary>
    /// <param name="itemID">ID của item trong game</param>
    public void WithdrawItem(int itemID)
    {
        // Lấy ItemData từ ItemDatabase
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[WithdrawManager] ItemDatabase.Instance is null!");
            return;
        }

        ItemData itemData = ItemDatabase.Instance.GetItemByID(itemID);
        if (itemData == null)
        {
            Debug.LogError($"[WithdrawManager] Không tìm thấy ItemData với ID: {itemID}");
            return;
        }

        // Gọi method với ItemData
        WithdrawItem(itemData);
    }

    /// <summary>
    /// Withdraw với thông tin tùy chỉnh (legacy method, giữ lại để tương thích)
    /// </summary>
    /// <param name="itemID">ID của item trong game</param>
    /// <param name="tokenURI">Token URI cho NFT</param>
    /// <param name="contractAddress">Contract address (optional, sẽ dùng default nếu không có)</param>
    public void WithdrawItem(int itemID, string tokenURI, string contractAddress = "")
    {
        string contractAddr = contractAddress;
        if (string.IsNullOrEmpty(contractAddr))
        {
            contractAddr = defaultContractAddress;
        }

        if (string.IsNullOrEmpty(contractAddr))
        {
            Debug.LogError("[WithdrawManager] Contract Address chưa được set!");
            return;
        }

        if (string.IsNullOrEmpty(tokenURI))
        {
            Debug.LogError("[WithdrawManager] Token URI chưa được set!");
            return;
        }

        Debug.Log($"[WithdrawManager] Withdraw item ID: {itemID}, TokenURI: {tokenURI}");

        // Mở trang withdraw
        OpenWithdrawPage(itemID, tokenURI, contractAddr);
    }

    /// <summary>
    /// Lấy đường dẫn tuyệt đối tới file HTML
    /// </summary>
    private string GetHTMLPath()
    {
        // Tìm file trong nhiều vị trí có thể
        string fileName = Path.GetFileName(withdrawWebPath); // "index.html"
        string folderName = Path.GetDirectoryName(withdrawWebPath).Replace('\\', '/'); // "BlockchainWeb"
        
        // Ưu tiên tìm trong StreamingAssets trước (sau khi build)
        string[] possiblePaths = {
            // 1. StreamingAssets/BlockchainWeb/ (sau khi build - ưu tiên cao nhất)
            Path.Combine(Application.streamingAssetsPath, folderName, fileName),
            // 2. Build folder root/BlockchainWeb/ (nếu copy vào root build folder)
            Path.Combine(Path.GetDirectoryName(Application.dataPath), folderName, fileName),
            // 3. Application.dataPath/../BlockchainWeb/ (build folder)
            Path.Combine(Application.dataPath, "..", folderName, fileName),
            // 4. Trong editor: Assets/BlockchainWeb/
            Path.Combine(Application.dataPath, withdrawWebPath),
            Path.Combine(Application.dataPath, "..", withdrawWebPath),
            // 5. Current directory
            Path.Combine(Directory.GetCurrentDirectory(), withdrawWebPath),
            Path.Combine(Directory.GetCurrentDirectory(), folderName, fileName),
            // 6. Executable directory (cho standalone builds)
            Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), folderName, fileName),
            // 7. Application.persistentDataPath (fallback)
            Path.Combine(Application.persistentDataPath, folderName, fileName)
        };

        foreach (string path in possiblePaths)
        {
            try
            {
                string normalizedPath = Path.GetFullPath(path); // Normalize path
                if (File.Exists(normalizedPath))
                {
                    Debug.Log($"[WithdrawManager] ✅ Tìm thấy {fileName} tại: {normalizedPath}");
                    return normalizedPath;
                }
            }
            catch (System.Exception e)
            {
                // Bỏ qua path không hợp lệ
                Debug.LogWarning($"[WithdrawManager] Path không hợp lệ: {path} - {e.Message}");
            }
        }

        Debug.LogError($"[WithdrawManager] ❌ Không tìm thấy file {fileName} tại các đường dẫn:");
        foreach (string path in possiblePaths)
        {
            try
            {
                Debug.LogError($"  - {Path.GetFullPath(path)}");
            }
            catch { }
        }
        Debug.LogError($"[WithdrawManager] Application.dataPath: {Application.dataPath}");
        Debug.LogError($"[WithdrawManager] Application.streamingAssetsPath: {Application.streamingAssetsPath}");
        Debug.LogError($"[WithdrawManager] Directory.GetCurrentDirectory(): {Directory.GetCurrentDirectory()}");
        Debug.LogError($"[WithdrawManager] ⚠️ Vui lòng đảm bảo copy BlockchainWeb folder vào StreamingAssets hoặc build folder!");

        return null;
    }

    /// <summary>
    /// Mở trang link wallet trong trình duyệt
    /// </summary>
    public void OpenLinkWalletPage()
    {
        // Lấy đường dẫn tuyệt đối tới file HTML
        string htmlPath = GetLinkWalletHTMLPath();
        
        if (string.IsNullOrEmpty(htmlPath))
        {
            Debug.LogError("[WithdrawManager] Không tìm thấy file link-wallet.html!");
            return;
        }

        // Tạo URL - ưu tiên localhost nếu được bật
        string url;
        if (useLocalhost)
        {
            // Sử dụng localhost (khuyến nghị - MetaMask hoạt động tốt hơn)
            url = $"http://localhost:{localhostPort}/link-wallet.html";
            Debug.Log($"[WithdrawManager] Sử dụng localhost. Đảm bảo HTTP server đang chạy tại port {localhostPort}!");
            Debug.Log($"[WithdrawManager] Chạy: cd WithdrawWeb && python -m http.server {localhostPort}");
        }
        else
        {
            // Sử dụng file:// (có thể không hoạt động với MetaMask trên Chrome)
            string normalizedPath = htmlPath.Replace('\\', '/');
            url = $"file:///{normalizedPath}";
            Debug.LogWarning($"[WithdrawManager] Đang dùng file:// - MetaMask có thể không hoạt động trên Chrome!");
        }

        Debug.Log($"[WithdrawManager] Mở trình duyệt để link wallet: {url}");

        // Mở trình duyệt
        Application.OpenURL(url);
    }

    /// <summary>
    /// Helper method để tìm file HTML (dùng chung cho tất cả file HTML)
    /// </summary>
    private string GetHTMLPathByRelativePath(string relativePath)
    {
        string fileName = Path.GetFileName(relativePath);
        string folderName = Path.GetDirectoryName(relativePath).Replace('\\', '/'); // "BlockchainWeb"
        
        // Ưu tiên tìm trong StreamingAssets trước (sau khi build)
        string[] possiblePaths = {
            // 1. StreamingAssets/BlockchainWeb/ (sau khi build - ưu tiên cao nhất)
            Path.Combine(Application.streamingAssetsPath, folderName, fileName),
            // 2. Build folder root/BlockchainWeb/ (nếu copy vào root build folder)
            Path.Combine(Path.GetDirectoryName(Application.dataPath), folderName, fileName),
            // 3. Application.dataPath/../BlockchainWeb/ (build folder)
            Path.Combine(Application.dataPath, "..", folderName, fileName),
            // 4. Trong editor: Assets/BlockchainWeb/
            Path.Combine(Application.dataPath, relativePath),
            Path.Combine(Application.dataPath, "..", relativePath),
            // 5. Current directory
            Path.Combine(Directory.GetCurrentDirectory(), relativePath),
            Path.Combine(Directory.GetCurrentDirectory(), folderName, fileName),
            // 6. Executable directory (cho standalone builds)
            Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), folderName, fileName),
            // 7. Application.persistentDataPath (fallback)
            Path.Combine(Application.persistentDataPath, folderName, fileName)
        };

        foreach (string path in possiblePaths)
        {
            try
            {
                string normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    Debug.Log($"[WithdrawManager] ✅ Tìm thấy {fileName} tại: {normalizedPath}");
                    return normalizedPath;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WithdrawManager] Path không hợp lệ: {path} - {e.Message}");
            }
        }

        Debug.LogError($"[WithdrawManager] ❌ Không tìm thấy file {fileName}!");
        return null;
    }

    /// <summary>
    /// Lấy đường dẫn tuyệt đối tới file link-wallet.html
    /// </summary>
    private string GetLinkWalletHTMLPath()
    {
        return GetHTMLPathByRelativePath(linkWalletWebPath);
    }

    /// <summary>
    /// Withdraw coin để mint game token
    /// </summary>
    /// <param name="coinItem">CoinItem cần withdraw</param>
    /// <param name="quantity">Số lượng coin muốn withdraw (nếu <= 0 thì lấy tất cả từ inventory)</param>
    public void WithdrawCoin(CoinItem coinItem, int quantity = 0)
    {
        if (coinItem == null)
        {
            Debug.LogError("[WithdrawManager] CoinItem is null!");
            return;
        }

        // Kiểm tra contract address
        if (string.IsNullOrEmpty(gameTokenContractAddress))
        {
            Debug.LogError("[WithdrawManager] GameToken Contract Address chưa được set!");
            return;
        }

        // Lấy số lượng coin từ inventory nếu quantity không được chỉ định
        int coinQuantity = quantity > 0 ? quantity : (InventoryManager.Instance?.GetItemQuantity(coinItem.itemID) ?? 0);
        if (coinQuantity <= 0)
        {
            Debug.LogWarning($"[WithdrawManager] Không có coin để withdraw! (ItemID: {coinItem.itemID})");
            return;
        }

        // Tính tổng coin value (coinQuantity * coinValue)
        int totalCoinValue = coinQuantity * coinItem.coinValue;
        
        Debug.Log($"[WithdrawManager] Withdraw coin: {coinQuantity}x {coinItem.itemName} (coinValue: {coinItem.coinValue}, Total value: {totalCoinValue})");
        Debug.Log($"[WithdrawManager] Sẽ mint {totalCoinValue} GTK tokens");

        // Mở trang withdraw coin
        OpenWithdrawCoinPage(totalCoinValue, gameTokenContractAddress);
    }

    /// <summary>
    /// Mở trang withdraw coin trong trình duyệt
    /// </summary>
    /// <param name="coinAmount">Tổng số coin value (sẽ mint số lượng token tương ứng)</param>
    /// <param name="contractAddress">Contract address của GameToken</param>
    public void OpenWithdrawCoinPage(int coinAmount, string contractAddress = "")
    {
        // Lấy đường dẫn tuyệt đối tới file HTML
        string htmlPath = GetWithdrawCoinHTMLPath();
        
        if (string.IsNullOrEmpty(htmlPath))
        {
            Debug.LogError("[WithdrawManager] Không tìm thấy file withdraw-coin.html!");
            return;
        }

        // Lấy contract address
        string contractAddr = contractAddress;
        if (string.IsNullOrEmpty(contractAddr))
        {
            contractAddr = gameTokenContractAddress;
        }

        if (string.IsNullOrEmpty(contractAddr))
        {
            Debug.LogError("[WithdrawManager] GameToken Contract Address chưa được set!");
            return;
        }

        // Tạo URL - ưu tiên localhost nếu được bật
        string url;
        if (useLocalhost)
        {
            url = $"http://localhost:{localhostPort}/withdraw-coin.html";
            Debug.Log($"[WithdrawManager] Sử dụng localhost. Đảm bảo HTTP server đang chạy tại port {localhostPort}!");
        }
        else
        {
            string normalizedPath = htmlPath.Replace('\\', '/');
            url = $"file:///{normalizedPath}";
            Debug.LogWarning($"[WithdrawManager] Đang dùng file:// - MetaMask có thể không hoạt động trên Chrome!");
        }

        // Thêm parameters
        url += $"?contract={contractAddr}&amount={coinAmount}";

        Debug.Log($"[WithdrawManager] Mở trình duyệt để withdraw coin: {url}");

        // Mở trình duyệt
        Application.OpenURL(url);
    }

    /// <summary>
    /// Lấy đường dẫn tuyệt đối tới file withdraw-coin.html
    /// </summary>
    private string GetWithdrawCoinHTMLPath()
    {
        return GetHTMLPathByRelativePath(withdrawCoinWebPath);
    }

    /// <summary>
    /// Set default contract address (có thể gọi từ code hoặc Inspector)
    /// </summary>
    public void SetDefaultContractAddress(string address)
    {
        defaultContractAddress = address;
        Debug.Log($"[WithdrawManager] Đã set default contract address: {address}");
    }

    /// <summary>
    /// Set game token contract address (có thể gọi từ code hoặc Inspector)
    /// </summary>
    public void SetGameTokenContractAddress(string address)
    {
        gameTokenContractAddress = address;
        Debug.Log($"[WithdrawManager] Đã set game token contract address: {address}");
        
        // Đồng bộ với GameTokenBalanceManager nếu có
        if (GameTokenBalanceManager.Instance != null)
        {
            GameTokenBalanceManager.Instance.SetGameTokenContractAddress(address);
        }
    }

    /// <summary>
    /// Lấy game token contract address (public getter)
    /// </summary>
    public string GetGameTokenContractAddress()
    {
        return gameTokenContractAddress;
    }

    /// <summary>
    /// Test function - có thể gọi từ Inspector hoặc code
    /// </summary>
    [ContextMenu("Test Open Withdraw Page")]
    public void TestOpenWithdrawPage()
    {
        // Test với itemID (sẽ tự động lấy từ ItemDatabase)
        if (ItemDatabase.Instance != null)
        {
            ItemData testItem = ItemDatabase.Instance.GetItemByID(1);
            if (testItem != null)
            {
                WithdrawItem(testItem);
            }
            else
            {
                OpenWithdrawPage(1, "ipfs://QmTest123", defaultContractAddress);
            }
        }
        else
        {
            OpenWithdrawPage(1, "ipfs://QmTest123", defaultContractAddress);
        }
    }

}

