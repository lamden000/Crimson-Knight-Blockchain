# \# Crimson Knight - Game Client

# 

# \## 📖 Tổng quan

# 

# \*\*Crimson Knight\*\* là một game multiplayer 2D RPG với tích hợp blockchain, cho phép người chơi:

# \- Chơi multiplayer với Photon Network

# \- Quản lý inventory và character data qua PlayFab

# \- Rút NFT và game tokens lên blockchain (Polygon Amoy)

# \- Mua/bán NFT trên marketplace

# \- Tương tác với smart contracts qua MetaMask

# 

# \## 🎮 Tính năng chính

# 

# \- \*\*Multiplayer\*\*: Chơi cùng nhiều người chơi khác qua Photon Network

# \- \*\*Inventory System\*\*: Quản lý items, equipment, và currency

# \- \*\*Blockchain Integration\*\*: 

# &nbsp; - Withdraw Rare Items (NFT) lên blockchain

# &nbsp; - Withdraw Game Tokens (GTK) từ coin trong game

# &nbsp; - Marketplace: Mua/bán NFT

# &nbsp; - Link MetaMask wallet với tài khoản game

# \- \*\*Character System\*\*: Level, EXP, skills, equipment

# \- \*\*Monster System\*\*: Combat với nhiều loại quái vật

# \- \*\*Quest System\*\*: Nhiệm vụ và phần thưởng

# 

# \## 📋 Yêu cầu hệ thống

# 

# \### Phần mềm bắt buộc:

# \- \*\*Unity 2022.3 LTS\*\* hoặc mới hơn

# \- \*\*Visual Studio 2022\*\* hoặc \*\*Visual Studio Code\*\* (cho C# scripting)

# \- \*\*Git\*\* (để clone repository)

# 

# \### Cho Blockchain Features:

# \- \*\*MetaMask Extension\*\*: Cài đặt trên trình duyệt (Chrome, Firefox, Edge, Brave)

# \- \*\*Python 3.x\*\* HOẶC \*\*Node.js\*\* (để chạy local HTTP server cho blockchain web pages)

# \- \*\*POL tokens\*\*: Để trả gas fee trên Polygon Amoy Testnet (có thể lấy từ faucet)

# 

# \### Tài khoản cần thiết:

# \- \*\*PlayFab Account\*\*: Để quản lý player data và inventory

# \- \*\*Photon Account\*\*: Để multiplayer networking

# \- \*\*MetaMask Wallet\*\*: Để tương tác với blockchain

# 

# \## 🚀 Cài đặt và Setup

# 

# \### Bước 1: Clone Repository

# 

# ```bash

# git clone <repository-url>

# cd Crimson\_Knight-Client

# ```

# 

# \### Bước 2: Mở Project trong Unity

# 

# 1\. Mở \*\*Unity Hub\*\*

# 2\. Click \*\*Add\*\* → Chọn thư mục `Crimson\_Knight-Client`

# 3\. Unity sẽ tự động import project (có thể mất vài phút)

# 

# \### Bước 3: Cấu hình PlayFab

# 

# 1\. Tạo tài khoản tại \[PlayFab](https://playfab.com)

# 2\. Tạo một Game Title mới

# 3\. Lấy \*\*Title ID\*\* và \*\*Secret Key\*\*

# 4\. Trong Unity:

# &nbsp;  - Mở \*\*Window → PlayFab → Editor Extensions\*\*

# &nbsp;  - Nhập \*\*Title ID\*\* và \*\*Secret Key\*\*

# &nbsp;  - Click \*\*Install SDK\*\* (nếu chưa có)

# 

# \### Bước 4: Cấu hình Photon

# 

# 1\. Tạo tài khoản tại \[Photon](https://www.photonengine.com)

# 2\. Tạo một ứng dụng mới

# 3\. Lấy \*\*App ID\*\*

# 4\. Trong Unity:

# &nbsp;  - Mở file `PhotonServerSettings` (trong `Assets/Photon/PhotonUnityNetworking/Resources/`)

# &nbsp;  - Nhập \*\*App ID\*\* vào `App Id Realtime`

# &nbsp;  - Lưu lại

# 

# \### Bước 5: Cấu hình Blockchain (Quan trọng!)

# 

# \#### 5.1. Cài đặt MetaMask

# 

# 1\. Cài đặt MetaMask extension từ \[metamask.io](https://metamask.io)

# 2\. Tạo hoặc import wallet

# 3\. Thêm Polygon Amoy Testnet:

# &nbsp;  - Network Name: `Polygon Amoy Testnet`

# &nbsp;  - RPC URL: `https://rpc-amoy.polygon.technology`

# &nbsp;  - Chain ID: `80002`

# &nbsp;  - Currency Symbol: `POL`

# &nbsp;  - Block Explorer: `https://www.oklink.com/amoy`

# 

# \#### 5.2. Setup Local HTTP Server

# 

# \*\*⚠️ QUAN TRỌNG\*\*: Các trang web blockchain \*\*KHÔNG được deploy lên internet\*\*, bạn \*\*BẮT BUỘC\*\* phải chạy local HTTP server.

# 

# \*\*Cách 1: Sử dụng file batch (Khuyến nghị)\*\*

# 

# 1\. Mở thư mục `Assets/BlockchainWeb/`

# 2\. Double-click file `start-server.bat`

# 3\. Giữ cửa sổ Command Prompt mở trong khi chơi game

# 4\. Server sẽ chạy tại: `http://localhost:8000`

# 

# \*\*Cách 2: Chạy thủ công với Python\*\*

# 

# ```bash

# cd Assets/BlockchainWeb

# python -m http.server 8000

# ```

# 

# \*\*Cách 3: Chạy thủ công với Node.js\*\*

# 

# ```bash

# cd Assets/BlockchainWeb

# npx http-server -p 8000

# ```

# 

# \*\*Lưu ý\*\*: Server phải chạy \*\*TRƯỚC KHI\*\* mở game và thực hiện các thao tác blockchain!

# 

# \### Bước 6: Cấu hình Smart Contracts

# 

# 1\. Deploy các smart contracts lên Polygon Amoy:

# &nbsp;  - `RareItem.sol` - NFT contract

# &nbsp;  - `GameToken.sol` - ERC20 token contract

# &nbsp;  - `Marketplace.sol` - Marketplace contract

# 

# 2\. Trong Unity, cấu hình contract addresses:

# &nbsp;  - \*\*WithdrawManager\*\*: 

# &nbsp;    - `defaultContractAddress` = Địa chỉ RareItem contract

# &nbsp;    - `gameTokenContractAddress` = Địa chỉ GameToken contract

# &nbsp;  - \*\*MarketplaceManager\*\*:

# &nbsp;    - Các contract addresses đã được set trong Inspector

# 

# \### Bước 7: Build Settings

# 

# 1\. Mở \*\*File → Build Settings\*\*

# 2\. Thêm các scenes:

# &nbsp;  - `Scenes/Authentication` (scene đầu tiên)

# &nbsp;  - `Scenes/Main` (scene chính)

# 3\. Đảm bảo \*\*Authentication\*\* là scene đầu tiên

# 

# \## 🎯 Chạy Game

# 

# \### Trong Unity Editor:

# 

# 1\. \*\*Đảm bảo local server đang chạy\*\* (`start-server.bat`)

# 2\. Mở scene `Scenes/Authentication`

# 3\. Click \*\*Play\*\* button

# 4\. Đăng nhập với tài khoản PlayFab

# 5\. Game sẽ load vào scene `Main`

# 

# \### Build và Chạy:

# 

# 1\. \*\*File → Build Settings\*\*

# 2\. Chọn platform (Windows, Mac, Linux)

# 3\. Click \*\*Build\*\*

# 4\. \*\*QUAN TRỌNG\*\*: Sau khi build, copy thư mục `BlockchainWeb` vào:

# &nbsp;  - `BuildFolder/StreamingAssets/BlockchainWeb/`

# &nbsp;  - `BuildFolder/BlockchainWeb/` (root của build folder)

# 5\. Chạy local server từ thư mục build:

# &nbsp;  ```bash

# &nbsp;  cd BuildFolder/BlockchainWeb

# &nbsp;  start-server.bat

# &nbsp;  ```

# 6\. Chạy game executable

# 

# \## 📁 Cấu trúc Project

# 

# ```

# Crimson\_Knight-Client/

# ├── Assets/

# │   ├── BlockchainWeb/          # Các trang web cho blockchain (HTML)

# │   │   ├── index.html          # Withdraw Rare Items

# │   │   ├── withdraw-coin.html  # Withdraw Game Tokens

# │   │   ├── buy-item.html       # Mua item từ marketplace

# │   │   ├── sell-item.html      # Bán item trên marketplace

# │   │   ├── cancel-listing.html # Hủy listing

# │   │   ├── link-wallet.html    # Link MetaMask wallet

# │   │   ├── start-server.bat    # Script khởi động server

# │   │   └── README.md           # Hướng dẫn chi tiết blockchain

# │   ├── Scripts/                # C# scripts

# │   │   ├── AuthenticationManager.cs

# │   │   ├── NetworkManager.cs

# │   │   ├── InventoryManager.cs

# │   │   ├── Blockchain/

# │   │   │   ├── WithdrawManager.cs

# │   │   │   ├── MarketplaceManager.cs

# │   │   │   └── GameTokenBalanceManager.cs

# │   │   └── ...

# │   ├── Scenes/                 # Unity scenes

# │   │   ├── Authentication.unity

# │   │   └── Main.unity

# │   ├── SmartContracts/         # Solidity smart contracts

# │   │   ├── RareItem.sol

# │   │   ├── GameToken.sol

# │   │   └── Marketplace.sol

# │   ├── StreamingAssets/        # Assets được copy vào build

# │   │   └── BlockchainWeb/     # Copy từ BlockchainWeb/

# │   └── ...

# ├── README.md                   # File này

# └── ...

# ```

# 

# \## 🔧 Cấu hình Chi tiết

# 

# \### WithdrawManager

# 

# Trong Unity Inspector, cấu hình:

# \- `defaultContractAddress`: Địa chỉ RareItem contract

# \- `gameTokenContractAddress`: Địa chỉ GameToken contract

# \- `useLocalhost`: `true` (bắt buộc)

# \- `localhostPort`: `8000`

# 

# \### MarketplaceManager

# 

# Cấu hình các contract addresses:

# \- Marketplace contract address

# \- NFT contract address

# \- Token contract address

# \- `useLocalhost`: `true`

# \- `localhostPort`: `8000`

# 

# \### NetworkManager

# 

# \- `debugMode`: Bật/tắt debug logs

# \- `useFakeProfileForTesting`: Dùng profile giả để test

# 

# \## 🎮 Hướng dẫn Chơi

# 

# \### Đăng nhập

# 

# 1\. Mở game

# 2\. Nhập email và password (đã đăng ký trên PlayFab)

# 3\. Click \*\*Login\*\*

# 4\. Game sẽ load vào scene chính

# 

# \### Withdraw Items lên Blockchain

# 

# 1\. Mở Inventory

# 2\. Chọn item muốn withdraw (phải là Rare Item)

# 3\. Click nút \*\*Withdraw\*\*

# 4\. Trình duyệt sẽ mở trang withdraw

# 5\. Kết nối MetaMask

# 6\. Chuyển sang Polygon Amoy network (nếu chưa)

# 7\. Click \*\*Mint NFT\*\*

# 8\. Xác nhận transaction trong MetaMask

# 

# \### Mua/Bán Items trên Marketplace

# 

# \*\*Mua Item:\*\*

# 1\. Mở Marketplace

# 2\. Chọn item muốn mua

# 3\. Click \*\*Buy\*\*

# 4\. Trình duyệt sẽ mở trang buy

# 5\. Kết nối MetaMask

# 6\. Click \*\*Approve Token\*\* (lần đầu)

# 7\. Click \*\*Buy Item\*\*

# 8\. Xác nhận transactions

# 

# \*\*Bán Item:\*\*

# 1\. Mở Inventory

# 2\. Chọn item muốn bán

# 3\. Click \*\*Sell\*\*

# 4\. Nhập giá

# 5\. Trình duyệt sẽ mở trang sell

# 6\. Kết nối MetaMask

# 7\. Approve NFT và Token

# 8\. Click \*\*List Item\*\*

# 

# \*\*Hủy Listing:\*\*

# 1\. Mở Marketplace

# 2\. Tìm item của bạn đang bán

# 3\. Click \*\*Hủy bán\*\*

# 4\. Xác nhận transaction

# 

# \### Link Wallet

# 

# 1\. Vào Settings/Account

# 2\. Click \*\*Link Wallet\*\*

# 3\. Kết nối MetaMask

# 4\. Xác nhận để link wallet với tài khoản game

# 

# \## 🐛 Troubleshooting

# 

# \### Game không kết nối được PlayFab

# 

# \- Kiểm tra Title ID và Secret Key đã đúng chưa

# \- Kiểm tra internet connection

# \- Xem Console logs trong Unity để biết lỗi cụ thể

# 

# \### Game không kết nối được Photon

# 

# \- Kiểm tra App ID đã đúng chưa

# \- Kiểm tra internet connection

# \- Xem Console logs

# 

# \### Blockchain features không hoạt động

# 

# \*\*Lỗi: "MetaMask chưa được cài đặt"\*\*

# \- Cài đặt MetaMask extension

# 

# \*\*Lỗi: "Đang chạy từ file:// - MetaMask có thể không hoạt động"\*\*

# \- \*\*Nguyên nhân\*\*: Local server chưa chạy

# \- \*\*Giải pháp\*\*: Chạy `start-server.bat` trước khi mở game

# 

# \*\*Lỗi: "Cannot connect to localhost:8000"\*\*

# \- Kiểm tra server đang chạy (cửa sổ Command Prompt vẫn mở)

# \- Thử truy cập `http://localhost:8000` trong trình duyệt

# 

# \*\*Lỗi: "Internal JSON-RPC error"\*\*

# \- Chọn gas option "Cao" (High) trong MetaMask thay vì "Website"

# \- Đảm bảo có đủ POL để trả gas fee

# \- Kiểm tra network đang ở Polygon Amoy (Chain ID: 80002)

# 

# \### Build không tìm thấy HTML files

# 

# \- Đảm bảo `Editor/CopyWithdrawWebToBuild.cs` đã copy files vào build folder

# \- Kiểm tra `StreamingAssets/BlockchainWeb/` có đầy đủ files không

# \- Copy thủ công `BlockchainWeb/` vào build folder nếu cần

# 

# \### Multiplayer không hoạt động

# 

# \- Kiểm tra Photon App ID

# \- Kiểm tra internet connection

# \- Xem Console logs để biết lỗi cụ thể

# 

# \## 📚 Tài liệu Tham khảo

# 

# \### Unity \& Game Development

# \- \[Unity Documentation](https://docs.unity3d.com)

# \- \[Photon PUN Documentation](https://doc.photonengine.com/pun/current/getting-started/pun-intro)

# \- \[PlayFab Documentation](https://docs.microsoft.com/en-us/gaming/playfab/)

# 

# \### Blockchain

# \- \[MetaMask Documentation](https://docs.metamask.io)

# \- \[Web3.js Documentation](https://web3js.readthedocs.io)

# \- \[Polygon Amoy Testnet](https://docs.polygon.technology/docs/develop/network-details/network/)

# \- \[Solidity Documentation](https://docs.soliditylang.org)

# 

# \### Xem thêm

# \- `Assets/BlockchainWeb/README.md` - Hướng dẫn chi tiết về blockchain integration

# 

# \## 🔐 Bảo mật

# 

# \- \*\*KHÔNG\*\* commit private keys, secret keys, hoặc mnemonic phrases

# \- \*\*KHÔNG\*\* commit PlayFab Secret Key

# \- \*\*KHÔNG\*\* commit Photon App ID (nếu là production)

# \- Sử dụng `.gitignore` để loại trừ các file nhạy cảm

# 

# \## 📝 Lưu ý Quan trọng

# 

# 1\. \*\*Local Server\*\*: Luôn chạy `start-server.bat` trước khi chơi game và thực hiện blockchain operations

# 

# 2\. \*\*Network\*\*: Đảm bảo MetaMask đang ở Polygon Amoy Testnet (Chain ID: 80002)

# 

# 3\. \*\*Gas Fee\*\*: Trên testnet, gas fee rất thấp nhưng vẫn cần có POL trong ví

# 

# 4\. \*\*Build\*\*: Sau khi build, nhớ copy `BlockchainWeb/` vào build folder và chạy server từ đó

# 

# 5\. \*\*Testing\*\*: Sử dụng testnet tokens, không dùng mainnet tokens cho testing

# 

# \## 🤝 Đóng góp

# 

# Nếu bạn muốn đóng góp cho project:

# 1\. Fork repository

# 2\. Tạo feature branch

# 3\. Commit changes

# 4\. Push và tạo Pull Request

# 

# \## 📄 License

# 

# 

# \## 📞 Liên hệ \& Hỗ trợ

# 

# Nếu gặp vấn đề:

# 1\. Kiểm tra phần Troubleshooting

# 2\. Xem Console logs trong Unity (Window → General → Console)

# 3\. Xem browser console (F12) khi sử dụng blockchain features

# 4\. Kiểm tra MetaMask console logs

# 

# ---



