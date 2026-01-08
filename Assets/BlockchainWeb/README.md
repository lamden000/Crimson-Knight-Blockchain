# BlockchainWeb - Hướng dẫn sử dụng

## 📋 Tổng quan

Thư mục `BlockchainWeb` chứa các trang web HTML để tương tác với blockchain (MetaMask) cho game Crimson Knight. Các trang web này cho phép người chơi:
- **Withdraw Rare Items**: Rút NFT từ game lên blockchain
- **Withdraw Game Tokens**: Rút game token (GTK) từ coin trong game
- **Buy Items**: Mua NFT từ marketplace
- **Sell Items**: Bán NFT trên marketplace
- **Cancel Listing**: Hủy listing trên marketplace
- **Link Wallet**: Liên kết ví MetaMask với tài khoản game

## ⚠️ QUAN TRỌNG: Phải chạy Local Server

**Các trang web này KHÔNG được deploy lên internet**, do đó bạn **BẮT BUỘC** phải chạy một HTTP server trên localhost để các trang web hoạt động được với MetaMask.

### Tại sao cần Local Server?

- MetaMask không hoạt động với `file://` protocol trên Chrome
- Các trang web cần chạy qua HTTP/HTTPS để MetaMask có thể inject provider
- Local server cho phép truy cập các file HTML qua `http://localhost:8000`

## 🚀 Cài đặt và Chạy Server

### Cách 1: Sử dụng file batch (Khuyến nghị - Dễ nhất)

1. **Mở file `start-server.bat`**:
   - Double-click vào file `start-server.bat` trong thư mục `BlockchainWeb`
   - Hoặc click chuột phải → "Run as administrator" (nếu cần)

2. **Server sẽ tự động khởi động**:
   - File sẽ tự động kiểm tra và sử dụng Python hoặc Node.js
   - Server sẽ chạy tại: `http://localhost:8000`
   - **ĐỪNG đóng cửa sổ Command Prompt** - giữ nó mở trong khi sử dụng

3. **Kiểm tra server đã chạy**:
   - Mở trình duyệt và truy cập: `http://localhost:8000`
   - Bạn sẽ thấy danh sách các file HTML

### Cách 2: Chạy thủ công với Python

1. **Mở Terminal/PowerShell** trong thư mục `BlockchainWeb`

2. **Chạy lệnh**:
   ```bash
   python -m http.server 8000
   ```
   hoặc nếu dùng Python 3:
   ```bash
   python3 -m http.server 8000
   ```

3. **Server sẽ chạy tại**: `http://localhost:8000`

### Cách 3: Chạy thủ công với Node.js

1. **Cài đặt http-server** (nếu chưa có):
   ```bash
   npm install -g http-server
   ```

2. **Mở Terminal/PowerShell** trong thư mục `BlockchainWeb`

3. **Chạy lệnh**:
   ```bash
   npx http-server -p 8000
   ```

4. **Server sẽ chạy tại**: `http://localhost:8000`

## 📁 Cấu trúc File

```
BlockchainWeb/
├── README.md                    # File hướng dẫn này
├── start-server.bat            # Script tự động khởi động server (Windows)
├── index.html                  # Trang withdraw Rare Items (NFT)
├── withdraw-coin.html          # Trang withdraw Game Tokens (GTK)
├── buy-item.html               # Trang mua item từ marketplace
├── sell-item.html              # Trang bán item trên marketplace
├── cancel-listing.html         # Trang hủy listing trên marketplace
└── link-wallet.html            # Trang liên kết ví MetaMask
```

## 🎮 Cách sử dụng trong Game

### 1. Withdraw Rare Items (NFT)

- Từ game, chọn item muốn withdraw
- Click nút "Withdraw"
- Trình duyệt sẽ mở: `http://localhost:8000/index.html?contract=0x...&uri=ipfs://...`
- Kết nối MetaMask và mint NFT

### 2. Withdraw Game Tokens

- Từ game, chọn coin muốn withdraw
- Click nút "Withdraw"
- Trình duyệt sẽ mở: `http://localhost:8000/withdraw-coin.html?contract=0x...&amount=100`
- Kết nối MetaMask và mint tokens

### 3. Buy Items từ Marketplace

- Từ marketplace trong game, chọn item muốn mua
- Click nút "Buy"
- Trình duyệt sẽ mở: `http://localhost:8000/buy-item.html?marketplace=0x...&token=0x...&tokenId=3&price=10.00`
- Kết nối MetaMask, approve token, và mua item

### 4. Sell Items trên Marketplace

- Từ inventory, chọn item muốn bán
- Click nút "Sell"
- Trình duyệt sẽ mở: `http://localhost:8000/sell-item.html?marketplace=0x...&nft=0x...&token=0x...&tokenId=3&price=10.00`
- Kết nối MetaMask, approve NFT và token, rồi list item

### 5. Cancel Listing

- Từ marketplace, chọn item của bạn đang bán
- Click nút "Hủy bán"
- Trình duyệt sẽ mở: `http://localhost:8000/cancel-listing.html?marketplace=0x...&tokenId=3`
- Kết nối MetaMask và cancel listing

### 6. Link Wallet

- Từ game, vào menu Settings/Account
- Click "Link Wallet"
- Trình duyệt sẽ mở: `http://localhost:8000/link-wallet.html`
- Kết nối MetaMask và xác nhận

## ⚙️ Yêu cầu hệ thống

### Bắt buộc:
- **MetaMask Extension**: Cài đặt trên trình duyệt (Chrome, Firefox, Edge, Brave)
- **Python 3.x** HOẶC **Node.js** (để chạy local server)
- **Trình duyệt web**: Chrome, Firefox, Edge, hoặc Brave

### Khuyến nghị:
- **Network**: Polygon Amoy Testnet (Chain ID: 80002)
- **POL tokens**: Để trả gas fee (có thể lấy từ faucet)

## 🔧 Cấu hình trong Unity

Trong Unity Editor, đảm bảo các manager được cấu hình đúng:

1. **WithdrawManager**:
   - `useLocalhost = true`
   - `localhostPort = 8000`
   - `defaultContractAddress` = Địa chỉ RareItem contract
   - `gameTokenContractAddress` = Địa chỉ GameToken contract

2. **MarketplaceManager**:
   - `useLocalhost = true`
   - `localhostPort = 8000`
   - Các contract addresses đã được set

## 🐛 Troubleshooting

### Lỗi: "MetaMask chưa được cài đặt"
- **Giải pháp**: Cài đặt MetaMask extension từ [metamask.io](https://metamask.io)

### Lỗi: "Đang chạy từ file:// - MetaMask có thể không hoạt động"
- **Nguyên nhân**: Bạn đang mở file HTML trực tiếp (double-click), không qua local server
- **Giải pháp**: 
  1. Chạy `start-server.bat` trước
  2. Mở trình duyệt và truy cập `http://localhost:8000`
  3. Hoặc đảm bảo game đang mở URL với `http://localhost:8000`

### Lỗi: "Cannot connect to localhost:8000"
- **Nguyên nhân**: Server chưa được khởi động
- **Giải pháp**: 
  1. Chạy `start-server.bat`
  2. Kiểm tra cửa sổ Command Prompt vẫn đang mở
  3. Thử truy cập `http://localhost:8000` trong trình duyệt

### Lỗi: "Port 8000 is already in use"
- **Nguyên nhân**: Có ứng dụng khác đang dùng port 8000
- **Giải pháp**: 
  1. Đóng ứng dụng đang dùng port 8000
  2. Hoặc thay đổi port trong `start-server.bat` và cấu hình lại trong Unity

### Lỗi: "Internal JSON-RPC error" khi approve/buy
- **Nguyên nhân**: Gas fee quá thấp hoặc network issue
- **Giải pháp**: 
  1. Trong MetaMask, chọn gas option "Cao" (High) thay vì "Website"
  2. Đảm bảo có đủ POL để trả gas fee
  3. Kiểm tra network đang ở Polygon Amoy (Chain ID: 80002)

### Lỗi: "Transaction reverted"
- **Nguyên nhân**: Contract validation failed hoặc không đủ balance
- **Giải pháp**: 
  1. Kiểm tra lại contract addresses
  2. Kiểm tra balance (token/NFT)
  3. Kiểm tra item đã được approve chưa

### Server không khởi động được
- **Kiểm tra Python**:
  ```bash
  python --version
  ```
- **Kiểm tra Node.js**:
  ```bash
  node --version
  ```
- **Nếu không có cả hai**: Cài đặt Python từ [python.org](https://www.python.org) hoặc Node.js từ [nodejs.org](https://nodejs.org)

## 📝 Lưu ý quan trọng

1. **Luôn chạy server trước khi chơi game**: Đảm bảo `start-server.bat` đang chạy trước khi thực hiện các thao tác blockchain trong game

2. **Đừng đóng cửa sổ Command Prompt**: Giữ cửa sổ server mở trong khi chơi game

3. **Network phải đúng**: Đảm bảo MetaMask đang ở Polygon Amoy Testnet (Chain ID: 80002)

4. **Gas fee**: Trên testnet, gas fee thường rất thấp, nhưng vẫn cần có POL trong ví

5. **Build game**: Khi build game, các file HTML sẽ được copy vào `StreamingAssets/BlockchainWeb/` và build folder. Đảm bảo server cũng chạy từ thư mục build nếu cần

## 🔗 Liên kết hữu ích

- [MetaMask Documentation](https://docs.metamask.io)
- [Polygon Amoy Testnet](https://docs.polygon.technology/docs/develop/network-details/network/)
- [Web3.js Documentation](https://web3js.readthedocs.io)

## 📞 Hỗ trợ

Nếu gặp vấn đề, vui lòng:
1. Kiểm tra lại các bước trong phần Troubleshooting
2. Xem console log trong trình duyệt (F12 → Console)
3. Kiểm tra MetaMask console (F12 → Console, filter "MetaMask")

---

**Lưu ý cuối cùng**: Nhớ rằng các trang web này **PHẢI chạy qua local server** (`http://localhost:8000`), không thể mở trực tiếp bằng double-click file HTML!
