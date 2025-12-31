# Cách Lấy Function Selector Từ Remix (NHANH NHẤT)

## Bước 1: Mở Remix và Compile Contract
1. Mở https://remix.ethereum.org/
2. Load file `MarketPlace.sol`
3. Compile contract (Solidity Compiler tab)

## Bước 2: Deploy hoặc Connect Contract
1. Vào tab "Deploy & Run Transactions"
2. Deploy contract HOẶC connect với contract đã deploy (dán address vào "At Address")

## Bước 3: Gọi Function và Xem Input Data
1. Tìm function `getListing` trong deployed contract
2. Nhập `tokenId = 3` vào input field
3. Click "call" (vì là view function)
4. **QUAN TRỌNG**: Mở Developer Tools (F12) → Tab "Network"
5. Tìm request đến RPC endpoint (thường là `eth_call`)
6. Xem "Request Payload" → `params[0].data`
7. **4 bytes đầu (sau 0x) chính là function selector!**

## Ví dụ:
Nếu `data` là: `0x99a5d3240000000000000000000000000000000000000000000000000000000000000003`
Thì selector là: `0x99a5d324`

## Bước 4: Cập nhật vào Unity
1. Mở Unity
2. Chọn GameObject có `MarketplaceDataManager` component
3. Trong Inspector, tìm field `Get Listing Function Selector`
4. Paste selector vừa lấy (ví dụ: `0x99a5d324`)
5. Test lại!

## Lưu ý:
- Selector phải có format: `0x` + 8 hex characters (ví dụ: `0x99a5d324`)
- Nếu selector sai, sẽ thấy lỗi "execution reverted"
- Có thể test bằng cách gọi `getListing(3)` trong Remix và so sánh với data trong Unity logs

