# Tài liệu Tích hợp Blockchain - Crimson Knight Game

## 📋 Mục lục
1. [Tổng quan Kiến trúc](#tổng-quan-kiến-trúc)
2. [Cách Mint NFT](#cách-mint-nft)
3. [Đọc Dữ liệu từ Blockchain](#đọc-dữ-liệu-từ-blockchain)
   - [Đọc Token Balance](#1-đọc-token-balance-gametokenbalancemanagercs)
   - [Đọc Marketplace Listings](#2-đọc-marketplace-listings-marketplacedatamanagercs)
   - [Đọc NFT từ Wallet](#3-đọc-nft-từ-wallet-wallettinventorymanagercs)
   - [Đọc NFT Metadata từ IPFS](#4-đọc-nft-metadata-từ-ipfs)
4. [Logic Marketplace](#logic-marketplace)
5. [Item System và Blockchain](#item-system-và-blockchain)
6. [Smart Contracts](#smart-contracts)
7. [Code Examples](#code-examples)

---

## Tổng quan Kiến trúc

### Kiến trúc Tổng thể

Game Crimson Knight sử dụng kiến trúc **hybrid** để tích hợp blockchain:

```
Unity Game Client
    ↓
WithdrawManager / MarketplaceManager (C#)
    ↓
Application.OpenURL() → Mở trình duyệt
    ↓
HTML Pages (JavaScript + Web3.js)
    ↓
MetaMask Extension
    ↓
Polygon Amoy Testnet (Blockchain)
```

### Các Component Chính

1. **Unity C# Managers**: Quản lý logic game, mở trình duyệt
2. **HTML Web Pages**: Tương tác trực tiếp với blockchain qua MetaMask
3. **Smart Contracts**: 
   - `RareItem.sol` - ERC721 NFT contract
   - `GameToken.sol` - ERC20 token contract
   - `MarketPlace.sol` - Marketplace contract

### Tại sao dùng HTML Pages?

- Unity WebGL không hỗ trợ trực tiếp MetaMask
- MetaMask chỉ hoạt động trong trình duyệt
- HTML pages cho phép user ký transactions qua MetaMask
- Game mở trình duyệt, user thực hiện transaction, sau đó quay lại game

---

## Cách Mint NFT

### Quy trình Mint NFT (Withdraw Item)

#### 1. Unity Side - WithdrawManager.cs

```csharp
// Khi user click "Withdraw" trong game
public void WithdrawItem(ItemData itemData)
{
    // Kiểm tra item có thể withdraw không
    if (!itemData.withdrawable)
    {
        Debug.LogWarning("Item không thể withdraw!");
        return;
    }

    // Lấy contract address và token URI
    string contractAddr = itemData.nftContractAddress;
    string tokenURI = $"ipfs://{itemData.metadataCID}";

    // Mở trang web withdraw
    OpenWithdrawPage(itemData.itemID, tokenURI, contractAddr);
}

// Tạo URL và mở trình duyệt
public void OpenWithdrawPage(int itemID, string tokenURI, string contractAddress)
{
    // Tạo URL với parameters
    string url = $"http://localhost:8000/index.html?contract={contractAddress}&uri={UnityWebRequest.EscapeURL(tokenURI)}";
    
    // Mở trình duyệt
    Application.OpenURL(url);
}
```

#### 2. HTML Side - index.html (JavaScript + Web3.js)

```javascript
// Kết nối MetaMask
async function connectWallet() {
    // Revoke permissions để force user chọn account
    const permissions = await window.ethereum.request({
        method: 'wallet_getPermissions'
    });
    
    if (permissions && permissions.length > 0) {
        await window.ethereum.request({
            method: 'wallet_revokePermissions',
            params: [{ eth_accounts: {} }]
        });
    }

    // Request accounts
    const accounts = await window.ethereum.request({ 
        method: 'eth_requestAccounts' 
    });
    
    userAccount = accounts[0];
    web3 = new Web3(window.ethereum);
    
    // Khởi tạo contract
    contract = new web3.eth.Contract(RARE_ITEM_ABI, contractAddress);
}

// Mint NFT
async function mintNFT() {
    const tokenURI = document.getElementById('tokenURI').value;
    
    // Estimate gas
    let gasEstimate = await contract.methods.mintForMyself(tokenURI).estimateGas({
        from: userAccount
    });
    gasEstimate = Math.floor(gasEstimate * 1.2); // Thêm 20% buffer
    
    // Prepare transaction với EIP-1559 gas pricing
    let txParams = {
        from: userAccount,
        gas: gasEstimate
    };
    
    // EIP-1559 gas pricing
    const block = await web3.eth.getBlock('latest');
    const baseFee = block.baseFeePerGas || await web3.eth.getGasPrice();
    const maxPriorityFeePerGas = web3.utils.toWei('2', 'gwei');
    const maxFeePerGas = web3.utils.toBN(baseFee)
        .mul(web3.utils.toBN(2))
        .add(web3.utils.toBN(maxPriorityFeePerGas))
        .toString();
    
    txParams.maxFeePerGas = maxFeePerGas;
    txParams.maxPriorityFeePerGas = maxPriorityFeePerGas;
    
    // Gửi transaction
    const tx = await contract.methods.mintForMyself(tokenURI).send(txParams);
    
    console.log('Mint thành công! Transaction:', tx.transactionHash);
}
```

#### 3. Smart Contract Side - RareItem.sol

```solidity
// Hàm mint NFT
function mintForMyself(string memory uri) public {
    uint256 tokenId = nextTokenId;
    nextTokenId++;
    
    _safeMint(msg.sender, tokenId);
    _setTokenURI(tokenId, uri);
    
    emit Minted(msg.sender, tokenId, uri);
}
```

### Flow Diagram

```
User clicks "Withdraw" in game
    ↓
WithdrawManager.WithdrawItem()
    ↓
OpenWithdrawPage() → Opens browser
    ↓
User connects MetaMask
    ↓
User clicks "Mint NFT"
    ↓
JavaScript calls contract.mintForMyself(tokenURI)
    ↓
MetaMask shows transaction popup
    ↓
User confirms transaction
    ↓
Transaction sent to Polygon Amoy
    ↓
NFT minted to user's wallet
```

---

## Đọc Dữ liệu từ Blockchain

### 1. Đọc Token Balance (GameTokenBalanceManager.cs)

Game sử dụng **Alchemy API** để đọc token balance từ blockchain:

```csharp
public class GameTokenBalanceManager : MonoBehaviour
{
    [SerializeField] private string alchemyApiKey = "eQMwPXs4A8OF-f9jmKLFD";
    [SerializeField] private string alchemyBaseUrl = "https://polygon-amoy.g.alchemy.com/v2";
    [SerializeField] private string gameTokenContractAddress = "";
    [SerializeField] private int tokenDecimals = 18;

    // Fetch token balance từ Alchemy API
    private IEnumerator FetchTokenBalance(string walletAddress)
    {
        string url = $"{alchemyBaseUrl}/{alchemyApiKey}";
        
        // Tạo JSON-RPC request
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
                // Parse response
                TokenBalanceResponse response = JsonUtility.FromJson<TokenBalanceResponse>(request.downloadHandler.text);
                
                if (response.result.tokenBalances.Length > 0)
                {
                    string balanceHex = response.result.tokenBalances[0].tokenBalance;
                    
                    // Convert hex to BigInteger
                    BigInteger balanceWei = ParseHexToBigInteger(balanceHex);
                    
                    // Convert từ wei sang token (chia cho 10^decimals)
                    double balanceWeiDouble = (double)balanceWei;
                    double divisor = Math.Pow(10, tokenDecimals);
                    float balance = (float)(balanceWeiDouble / divisor);
                    
                    currentBalance = balance;
                    OnBalanceUpdated?.Invoke(balance);
                }
            }
        }
    }
}
```

### 2. Đọc Marketplace Listings (MarketplaceDataManager.cs)

Game đọc listings từ **PlayFab** (không phải trực tiếp từ blockchain vì lý do performance):

```csharp
public class MarketplaceDataManager : MonoBehaviour
{
    // Load listings từ PlayFab
    public void LoadMarketplaceListings()
    {
        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest(),
            result => {
                if (result.Data != null && result.Data.ContainsKey("MarketplaceListings"))
                {
                    string listingsJson = result.Data["MarketplaceListings"];
                    MarketplaceListingsData data = JsonUtility.FromJson<MarketplaceListingsData>(listingsJson);
                    
                    // Parse và lưu vào dictionary
                    foreach (var listing in data.listings)
                    {
                        listings[listing.tokenId] = listing;
                    }
                }
            },
            error => {
                Debug.LogError("Lỗi load marketplace listings: " + error.ErrorMessage);
            }
        );
    }
}
```

**Lý do dùng PlayFab thay vì đọc trực tiếp từ blockchain:**
- Performance: Đọc từ blockchain chậm và tốn gas
- PlayFab cache listings để query nhanh
- Khi có transaction mới, update PlayFab thông qua CloudScript

### 3. Đọc NFT từ Wallet (WalletInventoryManager.cs)

Game sử dụng **Alchemy API** để đọc tất cả NFT mà người chơi sở hữu trong ví và map về items trong game:

```csharp
public class WalletInventoryManager : MonoBehaviour
{
    [SerializeField] private string alchemyApiKey = "eQMwPXs4A8OF-f9jmKLFD";
    [SerializeField] private string alchemyBaseUrl = "https://polygon-amoy.g.alchemy.com/v2";
    [SerializeField] private string rareItemContractAddress = "0x02DF0ccd422e6126C0Fd30a203B950eB0015d08A";

    private Dictionary<int, WalletNFT> walletNFTs = new Dictionary<int, WalletNFT>();

    // Fetch NFT từ Alchemy API
    private IEnumerator FetchNFTsFromAlchemy(string walletAddress)
    {
        // Alchemy API endpoint để get NFTs của một wallet
        string url = $"{alchemyBaseUrl}/{alchemyApiKey}/getNFTs?owner={walletAddress}&contractAddresses[]={rareItemContractAddress}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                // Parse JSON response từ Alchemy
                AlchemyNFTResponse response = JsonUtility.FromJson<AlchemyNFTResponse>(jsonResponse);
                
                if (response != null && response.ownedNfts != null && response.ownedNfts.Length > 0)
                {
                    List<AlchemyNFT> nfts = AlchemyNFT.FromWrapper(response.ownedNfts);
                    ProcessNFTs(nfts, walletAddress);
                }
            }
        }
    }

    // Xử lý danh sách NFT và map về itemID trong game
    private void ProcessNFTs(List<AlchemyNFT> nfts, string walletAddress)
    {
        walletNFTs.Clear();

        foreach (var nft in nfts)
        {
            WalletNFT walletNFT = new WalletNFT
            {
                tokenId = nft.id?.tokenId,
                tokenURI = nft.tokenUri?.raw ?? nft.tokenUri?.gateway ?? "",
                contractAddress = nft.contract?.address ?? rareItemContractAddress,
                ownerAddress = walletAddress
            };

            // Map metadata URL về itemID trong game
            walletNFT.itemID = MapMetadataToItemID(walletNFT.tokenURI);
            
            if (walletNFT.itemID > 0)
            {
                // Lấy ItemData từ ItemDatabase
                if (ItemDatabase.Instance != null)
                {
                    walletNFT.itemData = ItemDatabase.Instance.GetItemByID(walletNFT.itemID);
                }

                // Parse tokenId từ hex string (0x...) thành int
                int tokenIdInt = ParseHexTokenId(walletNFT.tokenId);
                walletNFTs[tokenIdInt] = walletNFT;
            }
        }

        Debug.Log($"Đã load {walletNFTs.Count} NFT từ wallet");
        OnWalletInventoryRefreshed?.Invoke();
    }

    // Map metadata URL về itemID trong game
    // Logic: Tìm ItemData có metadataCID khớp với metadataURL
    private int MapMetadataToItemID(string metadataURL)
    {
        if (string.IsNullOrEmpty(metadataURL) || ItemDatabase.Instance == null)
        {
            return 0;
        }

        // Normalize metadata URL (có thể là ipfs://, https://, hoặc chỉ CID)
        string normalizedURL = metadataURL.Trim();
        
        // Extract CID từ IPFS URL
        string cid = "";
        if (normalizedURL.StartsWith("ipfs://"))
        {
            cid = normalizedURL.Substring(7); // Bỏ "ipfs://"
        }
        else if (normalizedURL.StartsWith("https://ipfs.io/ipfs/"))
        {
            int ipfsIndex = normalizedURL.IndexOf("/ipfs/");
            if (ipfsIndex >= 0)
            {
                cid = normalizedURL.Substring(ipfsIndex + 6);
            }
        }

        // Tìm ItemData có metadataCID khớp
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
        }

        return 0; // Không tìm thấy
    }
}
```

**Cách hoạt động:**

1. **Fetch từ Alchemy**: Gọi Alchemy API với `getNFTs` endpoint, truyền `owner` (wallet address) và `contractAddresses[]` (RareItem contract)
2. **Parse Response**: Alchemy trả về danh sách NFT với thông tin:
   - `tokenId`: ID của NFT
   - `tokenURI`: URI của metadata (thường là IPFS link)
   - `contract.address`: Địa chỉ contract
3. **Map về Game Items**: 
   - Extract CID từ `tokenURI` (ví dụ: `ipfs://QmXXXXX` → `QmXXXXX`)
   - Tìm `ItemData` trong game có `metadataCID` khớp với CID
   - Map `tokenId` (hex) thành int để lưu vào dictionary
4. **Lưu vào Dictionary**: Lưu tất cả NFT vào `walletNFTs` dictionary với key là `tokenId` (int)

**Sử dụng trong Game:**

```csharp
// Refresh wallet inventory
WalletInventoryManager.Instance.RefreshWalletInventory();

// Lấy tất cả NFT từ wallet
Dictionary<int, WalletNFT> nfts = WalletInventoryManager.Instance.GetAllWalletNFTs();

// Lấy số lượng NFT của một itemID
int count = WalletInventoryManager.Instance.GetNFTCountByItemID(itemID);
```

**Lý do sử dụng Alchemy API:**
- **Performance**: Nhanh hơn nhiều so với query trực tiếp từ blockchain
- **Free tier**: Alchemy cung cấp free tier đủ cho testnet
- **Metadata parsing**: Alchemy tự động parse metadata từ IPFS
- **Filtering**: Có thể filter theo contract address dễ dàng

### 4. Đọc NFT Metadata từ IPFS

```csharp
// Token URI thường là IPFS link: ipfs://QmXXXXX
// Game có thể fetch metadata từ IPFS gateway
private IEnumerator FetchNFTMetadata(string tokenURI)
{
    // Convert IPFS URI sang HTTP gateway
    string httpUrl = tokenURI.Replace("ipfs://", "https://ipfs.io/ipfs/");
    
    using (UnityWebRequest request = UnityWebRequest.Get(httpUrl))
    {
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            NFTMetadata metadata = JsonUtility.FromJson<NFTMetadata>(json);
            // metadata chứa name, description, image, attributes...
        }
    }
}
```

---

## Logic Marketplace

### 1. List Item (Bán Item)

#### Unity Side - MarketplaceManager.cs

```csharp
public void OpenSellItemPage(string tokenId, string price)
{
    // Validate contract addresses
    if (string.IsNullOrEmpty(marketplaceContractAddress) || 
        string.IsNullOrEmpty(nftContractAddress) || 
        string.IsNullOrEmpty(gameTokenContractAddress))
    {
        Debug.LogError("Contract addresses chưa được set!");
        return;
    }

    // Tạo URL với parameters
    string url = $"http://localhost:8000/sell-item.html" +
        $"?marketplace={UnityWebRequest.EscapeURL(marketplaceContractAddress)}" +
        $"&nft={UnityWebRequest.EscapeURL(nftContractAddress)}" +
        $"&token={UnityWebRequest.EscapeURL(gameTokenContractAddress)}" +
        $"&tokenId={UnityWebRequest.EscapeURL(tokenId)}" +
        $"&price={UnityWebRequest.EscapeURL(price)}";
    
    Application.OpenURL(url);
}
```

#### HTML Side - sell-item.html

```javascript
// Quy trình list item:
// 1. Approve NFT cho marketplace
// 2. Approve Token cho marketplace (nếu cần)
// 3. Gọi listItem() trên marketplace contract

async function listItem() {
    const tokenId = BigInt(tokenIdParam);
    const priceInWei = web3.utils.toWei(price, 'ether');
    
    // Step 1: Approve NFT
    await nftContract.methods.approve(marketplaceAddress, tokenId).send({
        from: userAccount,
        gas: 100000
    });
    
    // Step 2: Approve Token (nếu marketplace cần)
    // ...
    
    // Step 3: List item
    const tx = await marketplaceContract.methods.listItem(
        nftAddress,
        tokenId,
        priceInWei
    ).send({
        from: userAccount,
        gas: 200000
    });
    
    console.log('Listed! Transaction:', tx.transactionHash);
}
```

#### Smart Contract - MarketPlace.sol

```solidity
struct Listing {
    address seller;
    address nftContract;
    uint256 tokenId;
    uint256 price;
    bool active;
}

mapping(uint256 => Listing) public listings;

function listItem(
    address nftContract,
    uint256 tokenId,
    uint256 price
) public {
    // Kiểm tra NFT thuộc về seller
    require(
        IERC721(nftContract).ownerOf(tokenId) == msg.sender,
        "Not owner"
    );
    
    // Kiểm tra đã approve cho marketplace chưa
    require(
        IERC721(nftContract).getApproved(tokenId) == address(this),
        "Not approved"
    );
    
    // Tạo listing
    listings[tokenId] = Listing({
        seller: msg.sender,
        nftContract: nftContract,
        tokenId: tokenId,
        price: price,
        active: true
    });
    
    emit ItemListed(tokenId, msg.sender, price);
}
```

### 2. Buy Item (Mua Item)

#### HTML Side - buy-item.html

```javascript
async function buyItem() {
    const tokenId = BigInt(tokenIdParam);
    
    // Step 1: Approve token (nếu chưa approve đủ)
    const allowance = await tokenContract.methods.allowance(
        userAccount, 
        marketplaceAddress
    ).call();
    
    const priceInWei = web3.utils.toWei(price, 'ether');
    const priceBigInt = BigInt(priceInWei.toString());
    const allowanceBigInt = BigInt(allowance.toString());
    
    if (allowanceBigInt < priceBigInt) {
        // Cần approve token
        await tokenContract.methods.approve(
            marketplaceAddress, 
            priceBigInt.toString()
        ).send({
            from: userAccount,
            gas: 100000
        });
    }
    
    // Step 2: Buy item
    const tx = await marketplaceContract.methods.buyItem(tokenId).send({
        from: userAccount,
        gas: 200000
    });
    
    console.log('Bought! Transaction:', tx.transactionHash);
}
```

#### Smart Contract - MarketPlace.sol

```solidity
function buyItem(uint256 tokenId) public {
    Listing storage listing = listings[tokenId];
    
    require(listing.active, "Not for sale");
    
    // Transfer token từ buyer đến seller
    IERC20(gameToken).transferFrom(
        msg.sender,
        listing.seller,
        listing.price
    );
    
    // Transfer NFT từ seller đến buyer
    IERC721(listing.nftContract).transferFrom(
        listing.seller,
        msg.sender,
        tokenId
    );
    
    // Xóa listing
    delete listings[tokenId];
    
    emit ItemBought(tokenId, msg.sender, listing.seller, listing.price);
}
```

### 3. Cancel Listing

```javascript
async function cancelListing() {
    const tokenId = BigInt(tokenIdParam);
    
    const tx = await marketplaceContract.methods.cancelListing(tokenId).send({
        from: userAccount,
        gas: 100000
    });
    
    console.log('Cancelled! Transaction:', tx.transactionHash);
}
```

```solidity
function cancelListing(uint256 tokenId) public {
    Listing storage listing = listings[tokenId];
    
    require(listing.seller == msg.sender, "Not seller");
    require(listing.active, "Not active");
    
    delete listings[tokenId];
    
    emit ListingCancelled(tokenId, msg.sender);
}
```

---

## Item System và Blockchain

### Item Data Structure

```csharp
[System.Serializable]
public class ItemData
{
    public int itemID;
    public string itemName;
    public bool withdrawable;              // Có thể withdraw lên blockchain không
    public string nftContractAddress;      // Địa chỉ NFT contract
    public string metadataCID;             // IPFS CID cho metadata
    // ... other fields
}
```

### Withdraw Flow

```
1. User có item trong inventory (lưu trong PlayFab)
2. User click "Withdraw"
3. Game kiểm tra item.withdrawable == true
4. Game lấy metadataCID và nftContractAddress
5. Game mở trang web withdraw với tokenURI = ipfs://{metadataCID}
6. User mint NFT trên blockchain
7. Item vẫn còn trong game inventory (không tự động xóa)
```

### Link Wallet với Game Account

```csharp
// InventoryManager.cs
public void LinkWallet(string walletAddress)
{
    // Lưu wallet address vào PlayFab User Data
    var request = new UpdateUserDataRequest
    {
        Data = new Dictionary<string, string>
        {
            { "WalletAddress", walletAddress }
        }
    };
    
    PlayFabClientAPI.UpdateUserData(request,
        result => {
            Debug.Log("Wallet linked successfully!");
        },
        error => {
            Debug.LogError("Failed to link wallet: " + error.ErrorMessage);
        }
    );
}
```

---

## Smart Contracts

### 1. RareItem.sol (ERC721 NFT)

```solidity
contract RareItem is ERC721URIStorage {
    uint256 public nextTokenId;
    address public admin;
    
    constructor() ERC721("RareItem", "RARE") {
        admin = msg.sender;
        nextTokenId = 1;
    }
    
    // Mint NFT cho chính mình
    function mintForMyself(string memory uri) public {
        uint256 tokenId = nextTokenId;
        nextTokenId++;
        
        _safeMint(msg.sender, tokenId);
        _setTokenURI(tokenId, uri);
        
        emit Minted(msg.sender, tokenId, uri);
    }
    
    // Admin mint (cho game)
    function adminMint(address to, string memory uri) public {
        require(msg.sender == admin, "Not admin");
        uint256 tokenId = nextTokenId;
        nextTokenId++;
        
        _safeMint(to, tokenId);
        _setTokenURI(tokenId, uri);
    }
}
```

### 2. GameToken.sol (ERC20)

```solidity
contract GameToken is ERC20 {
    address public admin;
    
    constructor() ERC20("GameToken", "GTK") {
        admin = msg.sender;
    }
    
    // Mint token (chỉ admin)
    function mint(address to, uint256 amount) public {
        require(msg.sender == admin, "Not admin");
        _mint(to, amount);
    }
}
```

### 3. MarketPlace.sol

```solidity
contract MarketPlace {
    struct Listing {
        address seller;
        address nftContract;
        uint256 tokenId;
        uint256 price;
        bool active;
    }
    
    address public gameToken;  // ERC20 token để thanh toán
    mapping(uint256 => Listing) public listings;
    
    event ItemListed(uint256 indexed tokenId, address seller, uint256 price);
    event ItemBought(uint256 indexed tokenId, address buyer, address seller, uint256 price);
    event ListingCancelled(uint256 indexed tokenId, address seller);
    
    function listItem(
        address nftContract,
        uint256 tokenId,
        uint256 price
    ) public {
        require(
            IERC721(nftContract).ownerOf(tokenId) == msg.sender,
            "Not owner"
        );
        require(
            IERC721(nftContract).getApproved(tokenId) == address(this),
            "Not approved"
        );
        
        listings[tokenId] = Listing({
            seller: msg.sender,
            nftContract: nftContract,
            tokenId: tokenId,
            price: price,
            active: true
        });
        
        emit ItemListed(tokenId, msg.sender, price);
    }
    
    function buyItem(uint256 tokenId) public {
        Listing storage listing = listings[tokenId];
        require(listing.active, "Not for sale");
        
        // Transfer token
        IERC20(gameToken).transferFrom(
            msg.sender,
            listing.seller,
            listing.price
        );
        
        // Transfer NFT
        IERC721(listing.nftContract).transferFrom(
            listing.seller,
            msg.sender,
            tokenId
        );
        
        delete listings[tokenId];
        
        emit ItemBought(tokenId, msg.sender, listing.seller, listing.price);
    }
    
    function cancelListing(uint256 tokenId) public {
        Listing storage listing = listings[tokenId];
        require(listing.seller == msg.sender, "Not seller");
        require(listing.active, "Not active");
        
        delete listings[tokenId];
        
        emit ListingCancelled(tokenId, msg.sender);
    }
}
```

---

## Code Examples

### Example 1: Complete Withdraw Flow

```csharp
// Unity C# - WithdrawManager.cs
public void WithdrawItem(ItemData itemData)
{
    // 1. Validate
    if (!itemData.withdrawable) return;
    
    // 2. Get contract and URI
    string contract = itemData.nftContractAddress;
    string tokenURI = $"ipfs://{itemData.metadataCID}";
    
    // 3. Open browser
    string url = $"http://localhost:8000/index.html?contract={contract}&uri={UnityWebRequest.EscapeURL(tokenURI)}";
    Application.OpenURL(url);
}
```

```javascript
// HTML - index.html
async function mintNFT() {
    // 1. Connect wallet
    const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
    const web3 = new Web3(window.ethereum);
    
    // 2. Get contract
    const contract = new web3.eth.Contract(RARE_ITEM_ABI, contractAddress);
    
    // 3. Get token URI from URL
    const tokenURI = new URLSearchParams(window.location.search).get('uri');
    
    // 4. Estimate gas
    const gasEstimate = await contract.methods.mintForMyself(tokenURI).estimateGas({
        from: accounts[0]
    });
    
    // 5. Send transaction
    const tx = await contract.methods.mintForMyself(tokenURI).send({
        from: accounts[0],
        gas: Math.floor(gasEstimate * 1.2)
    });
    
    console.log('Minted!', tx.transactionHash);
}
```

### Example 2: Read Token Balance

```csharp
// GameTokenBalanceManager.cs
public void RefreshBalance()
{
    string walletAddress = InventoryManager.Instance.GetWalletAddress();
    
    StartCoroutine(FetchTokenBalance(walletAddress));
}

private IEnumerator FetchTokenBalance(string walletAddress)
{
    string url = $"https://polygon-amoy.g.alchemy.com/v2/{alchemyApiKey}";
    string jsonBody = $"{{\"jsonrpc\":\"2.0\",\"method\":\"alchemy_getTokenBalances\",\"params\":[\"{walletAddress}\",[\"{gameTokenContractAddress}\"]],\"id\":1}}";
    
    using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            TokenBalanceResponse response = JsonUtility.FromJson<TokenBalanceResponse>(request.downloadHandler.text);
            string balanceHex = response.result.tokenBalances[0].tokenBalance;
            
            // Convert hex to float
            BigInteger balanceWei = ParseHexToBigInteger(balanceHex);
            float balance = (float)((double)balanceWei / Math.Pow(10, 18));
            
            OnBalanceUpdated?.Invoke(balance);
        }
    }
}
```

### Example 3: Complete Buy Flow

```csharp
// Unity - MarketplaceManager.cs
public void BuyItem(string tokenId, System.Action onSuccess, System.Action<string> onFailed)
{
    // Get price from MarketplaceDataManager
    var listing = MarketplaceDataManager.Instance.GetListing(tokenId);
    string price = listing.priceInGTK.ToString("F2");
    
    // Open buy page
    string url = $"http://localhost:8000/buy-item.html" +
        $"?marketplace={marketplaceContractAddress}" +
        $"&nft={nftContractAddress}" +
        $"&token={gameTokenContractAddress}" +
        $"&tokenId={tokenId}" +
        $"&price={price}";
    
    Application.OpenURL(url);
}
```

```javascript
// HTML - buy-item.html
async function buyItem() {
    // 1. Check allowance
    const allowance = await tokenContract.methods.allowance(
        userAccount, 
        marketplaceAddress
    ).call();
    
    const priceInWei = web3.utils.toWei(price, 'ether');
    const priceBigInt = BigInt(priceInWei.toString());
    const allowanceBigInt = BigInt(allowance.toString());
    
    // 2. Approve if needed
    if (allowanceBigInt < priceBigInt) {
        await tokenContract.methods.approve(
            marketplaceAddress, 
            priceBigInt.toString()
        ).send({ from: userAccount });
    }
    
    // 3. Buy item
    const tokenId = BigInt(tokenIdParam);
    const tx = await marketplaceContract.methods.buyItem(tokenId).send({
        from: userAccount,
        gas: 200000
    });
    
    console.log('Bought!', tx.transactionHash);
}
```

### Example 4: Đọc NFT từ Wallet

```csharp
// WalletInventoryManager.cs
public void RefreshWalletInventory()
{
    // Lấy wallet address từ InventoryManager
    string walletAddress = InventoryManager.Instance?.GetWalletAddress();
    if (string.IsNullOrEmpty(walletAddress))
    {
        Debug.LogError("Wallet address chưa được set!");
        return;
    }

    // Fetch NFTs từ Alchemy
    StartCoroutine(FetchNFTsFromAlchemy(walletAddress));
}

private IEnumerator FetchNFTsFromAlchemy(string walletAddress)
{
    // Alchemy API endpoint
    string url = $"{alchemyBaseUrl}/{alchemyApiKey}/getNFTs?owner={walletAddress}&contractAddresses[]={rareItemContractAddress}";

    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse response
            AlchemyNFTResponse response = JsonUtility.FromJson<AlchemyNFTResponse>(request.downloadHandler.text);
            
            if (response.ownedNfts != null && response.ownedNfts.Length > 0)
            {
                List<AlchemyNFT> nfts = AlchemyNFT.FromWrapper(response.ownedNfts);
                ProcessNFTs(nfts, walletAddress);
            }
        }
    }
}

// Sử dụng trong game
void OnWalletInventoryRefreshed()
{
    // Lấy tất cả NFT từ wallet
    Dictionary<int, WalletNFT> nfts = WalletInventoryManager.Instance.GetAllWalletNFTs();
    
    // Hiển thị trong UI
    foreach (var nft in nfts.Values)
    {
        Debug.Log($"NFT TokenID: {nft.tokenId}, ItemID: {nft.itemID}, ItemName: {nft.itemData?.itemName}");
    }
    
    // Kiểm tra số lượng NFT của một item
    int swordCount = WalletInventoryManager.Instance.GetNFTCountByItemID(1001);
    Debug.Log($"Số lượng Sword NFT trong wallet: {swordCount}");
}
```

---

## Tóm tắt

### Điểm mạnh của Kiến trúc này:

1. **Tách biệt rõ ràng**: Unity quản lý game logic, HTML/JS quản lý blockchain
2. **Bảo mật**: Private keys không bao giờ rời khỏi MetaMask
3. **User-friendly**: User quen thuộc với MetaMask UI
4. **Flexible**: Dễ thêm tính năng mới bằng cách tạo HTML page mới

### Hạn chế:

1. **Phải mở trình duyệt**: User phải chuyển giữa game và browser
2. **Cần local server**: Phải chạy HTTP server để MetaMask hoạt động
3. **Không real-time**: Không thể detect transaction success tự động (phải refresh)

### Cải tiến có thể làm:

1. **WebSocket connection**: Để detect transaction success real-time
2. **Unity WebGL build**: Tích hợp trực tiếp MetaMask trong WebGL
3. **Backend service**: Để cache và sync blockchain data nhanh hơn

---

**Tài liệu này mô tả cách game Crimson Knight tích hợp với blockchain Polygon Amoy để cho phép người chơi mint NFT, trade items, và quản lý tài sản blockchain.**
