# Ví dụ sử dụng WithdrawManager trong Unity

## 1. Setup cơ bản

### Trong Unity Inspector:
1. Tạo GameObject mới trong scene
2. Add component `WithdrawManager`
3. Set `Contract Address` = địa chỉ contract RareItem đã deploy
4. Set `Withdraw Web Path` = `WithdrawWeb/index.html` (hoặc đường dẫn tương đối từ Assets)

## 2. Sử dụng trong code

### Ví dụ 1: Withdraw từ InventoryUI (Đơn giản nhất - sử dụng ItemData)

```csharp
// Thêm vào InventoryItemUI hoặc InventoryUI
public void OnWithdrawButtonClick(ItemData itemData)
{
    if (WithdrawManager.Instance == null)
    {
        Debug.LogError("WithdrawManager chưa được khởi tạo!");
        return;
    }

    // Chỉ cần truyền ItemData - WithdrawManager sẽ tự động lấy:
    // - nftContractAddress từ itemData.nftContractAddress
    // - metadataCID từ itemData.metadataCID
    // - Kiểm tra withdrawable flag
    WithdrawManager.Instance.WithdrawItem(itemData);
}
```

### Ví dụ 2: Withdraw từ itemID (tự động lấy ItemData)

```csharp
void WithdrawItemByID(int itemID)
{
    // Chỉ cần itemID - WithdrawManager sẽ tự động:
    // 1. Lấy ItemData từ ItemDatabase
    // 2. Kiểm tra withdrawable
    // 3. Lấy contract address và metadata CID
    WithdrawManager.Instance.WithdrawItem(itemID);
}
```

### Ví dụ 3: Set default contract address (fallback)

```csharp
void Start()
{
    // Set default contract address (sẽ dùng nếu ItemData không có nftContractAddress)
    string defaultContract = "0x1234567890abcdef...";
    WithdrawManager.Instance.SetDefaultContractAddress(defaultContract);
}
```

### Ví dụ 4: Withdraw với thông tin tùy chỉnh (legacy)

```csharp
void WithdrawWithCustomInfo(int itemID, string tokenURI, string contractAddress)
{
    // Nếu muốn override thông tin từ ItemData
    WithdrawManager.Instance.WithdrawItem(itemID, tokenURI, contractAddress);
}
```

## 3. Tích hợp với UI Button

### Trong Unity:
1. Tạo Button trong Inventory UI
2. Add OnClick event
3. Gọi method withdraw

```csharp
// Trong InventoryItemUI.cs
[SerializeField] private Button withdrawButton;
private ItemData currentItemData; // ItemData hiện tại

void SetupItem(ItemData itemData)
{
    currentItemData = itemData;
    
    // Chỉ hiển thị nút withdraw nếu item có thể withdraw
    if (withdrawButton != null)
    {
        withdrawButton.gameObject.SetActive(itemData != null && itemData.withdrawable);
        withdrawButton.onClick.RemoveAllListeners();
        withdrawButton.onClick.AddListener(OnWithdrawClick);
    }
}

void OnWithdrawClick()
{
    if (currentItemData != null && currentItemData.withdrawable)
    {
        WithdrawManager.Instance.WithdrawItem(currentItemData);
    }
}
```

## 4. Test trong Editor

1. Chọn GameObject có WithdrawManager
2. Right-click component
3. Chọn "Test Open Withdraw Page"
4. Trình duyệt sẽ mở với trang withdraw

## 5. Flow hoàn chỉnh

```
Game (Unity)
    ↓
User clicks "Withdraw" button
    ↓
WithdrawManager.WithdrawItem(ItemData)
    ↓
Kiểm tra itemData.withdrawable
    ↓
Lấy nftContractAddress và metadataCID từ ItemData
    ↓
Opens browser with index.html?contract=0x...&uri=ipfs://...
    ↓
Trang HTML tự động fill contract và tokenURI
    ↓
User connects MetaMask
    ↓
User clicks "Mint NFT"
    ↓
MetaMask popup appears
    ↓
User confirms transaction
    ↓
NFT minted to user's wallet
    ↓
Game can verify transaction (optional)
```

## 6. Cấu hình ItemData

Để item có thể withdraw, cần set trong ItemData (ScriptableObject):

1. **withdrawable** = `true`
2. **nftContractAddress** = Địa chỉ contract RareItem (ví dụ: `0x1234...`)
3. **metadataCID** = IPFS CID hoặc URL metadata (ví dụ: `QmYourHash` hoặc `ipfs://QmYourHash`)

Lưu ý: Nếu `metadataCID` không có prefix (`ipfs://`, `http://`, `https://`), hệ thống sẽ tự động thêm `ipfs://` prefix.

## 7. Verify transaction (Optional)

Sau khi mint thành công, bạn có thể verify transaction:

```csharp
// Sử dụng Web3 API hoặc blockchain explorer API
async void VerifyWithdraw(string transactionHash)
{
    // Check transaction status
    // Update inventory in game
    // Remove item from PlayFab inventory
}
```

## 8. Lưu ý quan trọng

- **ItemData phải có `withdrawable = true`** để có thể withdraw
- **Nếu ItemData có `nftContractAddress`**, sẽ ưu tiên dùng, nếu không sẽ dùng `defaultContractAddress` từ WithdrawManager
- **metadataCID** sẽ tự động được format thành `ipfs://...` nếu chưa có prefix
- Nếu thiếu thông tin, hệ thống sẽ log warning/error và không mở trình duyệt

