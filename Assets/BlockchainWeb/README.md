# RareItem Withdraw - MetaMask Integration

Trang web để withdraw item từ game lên blockchain thông qua MetaMask.

## Cách sử dụng

### 1. Chuẩn bị

- Cài đặt [MetaMask Extension](https://metamask.io/) trên trình duyệt
- Deploy contract `RareItem.sol` lên blockchain (testnet hoặc mainnet)
- Lấy địa chỉ contract sau khi deploy

### 2. Chạy trang web

#### ⚠️ QUAN TRỌNG: MetaMask không hoạt động với file:// protocol!

**Bạn PHẢI chạy qua HTTP server**, không thể mở trực tiếp file HTML.

#### Cách 1: Chạy HTTP Server (Khuyến nghị)

**Windows:**
1. Mở PowerShell hoặc CMD trong thư mục `WithdrawWeb`
2. Chạy: `start-server.bat` (hoặc `python -m http.server 8000`)
3. Mở trình duyệt: `http://localhost:8000`

**Mac/Linux:**
1. Mở Terminal trong thư mục `WithdrawWeb`
2. Chạy: `chmod +x start-server.sh && ./start-server.sh` (hoặc `python3 -m http.server 8000`)
3. Mở trình duyệt: `http://localhost:8000`

#### Cách 2: Mở từ Unity
- Trong Unity, gọi `WithdrawManager.Instance.OpenWithdrawPage(itemID, tokenURI)`
- **Đảm bảo HTTP server đang chạy trước!** (WithdrawManager sẽ mở `http://localhost:8000`)
- Nếu chưa có server, Unity sẽ mở file:// (sẽ không hoạt động với MetaMask)

### 3. Sử dụng

1. **Kết nối MetaMask**
   - Click nút "Kết nối MetaMask"
   - Chọn tài khoản và approve trong MetaMask

2. **Nhập Contract Address**
   - Nhập địa chỉ contract RareItem đã deploy
   - Hoặc truyền qua URL: `?contract=0x...`

3. **Nhập Token URI**
   - URI cho metadata của NFT (IPFS hoặc HTTPS)
   - Ví dụ: `ipfs://QmYourHashHere`

4. **Mint NFT**
   - Click "Mint NFT (Withdraw)"
   - Xác nhận giao dịch trong MetaMask
   - Đợi transaction được confirm

5. **Kiểm tra số lượng**
   - Click "Kiểm tra số lượng NFT" để xem số NFT bạn đang sở hữu

## URL Parameters

- `?contract=0x...` - Set contract address tự động
- `&uri=ipfs://...` - Set token URI tự động

## Lưu ý

- Đảm bảo MetaMask đang kết nối đúng network (testnet/mainnet)
- Cần có ETH/BNB để trả gas fee
- Contract address phải đúng với network hiện tại

## Tích hợp với Unity

```csharp
// Trong script Unity
using UnityEngine;

public class Example : MonoBehaviour
{
    void Start()
    {
        // Set contract address
        WithdrawManager.Instance.SetContractAddress("0xYourContractAddress");
        
        // Withdraw item
        WithdrawManager.Instance.WithdrawItem(itemID, "ipfs://QmYourHash");
    }
}
```

## Troubleshooting

### MetaMask không hiện / "MetaMask chưa được cài đặt"
**Nguyên nhân:** Đang chạy từ `file://` protocol - Chrome chặn extension injection.

**Giải pháp:**
1. ✅ **Chạy qua HTTP server** (không phải mở trực tiếp file HTML)
2. Chạy: `python -m http.server 8000` trong thư mục `WithdrawWeb`
3. Mở: `http://localhost:8000` (không phải `file:///...`)

### Transaction failed
- Kiểm tra gas limit và đảm bảo có đủ ETH/BNB
- Kiểm tra network (testnet/mainnet) có đúng không

### Contract không tìm thấy
- Kiểm tra contract address và network
- Đảm bảo contract đã được deploy trên network hiện tại

### File HTML không mở
- Kiểm tra đường dẫn trong `WithdrawManager.cs`
- Đảm bảo file `index.html` nằm trong `Assets/WithdrawWeb/`

