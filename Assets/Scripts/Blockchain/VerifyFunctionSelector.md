# Hướng dẫn Verify Function Selector cho getListing(uint256)

## Cách 1: Dùng Remix (Dễ nhất)

1. Mở Remix, compile contract `MarketPlace.sol`
2. Deploy contract (hoặc dùng contract đã deploy)
3. Trong tab "Solidity Compiler", click vào contract đã compile
4. Tìm function `getListing` trong ABI
5. Click vào function, nhập `tokenId = 3`
6. Click "transact" hoặc "call" (vì là view function)
7. Xem "input data" trong transaction details
8. 4 bytes đầu (0x...) chính là function selector

## Cách 2: Dùng Online Tool

1. Mở https://abi.hashex.org/
2. Copy ABI của contract (từ Remix: Solidity Compiler tab → ABI button)
3. Paste vào tool
4. Chọn function `getListing`
5. Nhập parameter: `3` (hoặc `["3"]` nếu cần array)
6. Xem "data" field
7. 4 bytes đầu (sau 0x) chính là function selector

## Cách 3: Dùng Web3.js Console

```javascript
// Trong browser console hoặc Node.js với web3
const Web3 = require('web3');
const web3 = new Web3();
const selector = web3.utils.keccak256("getListing(uint256)").substring(0, 10);
console.log("Function selector:", selector);
```

## Cách 4: Dùng Ethers.js Console

```javascript
// Trong browser console hoặc Node.js với ethers
const { ethers } = require('ethers');
const selector = ethers.utils.id("getListing(uint256)").substring(0, 10);
console.log("Function selector:", selector);
```

## Cách 5: Dùng Python với eth_abi

```python
from eth_abi import encode
from eth_utils import keccak, to_hex

# Function signature
sig = "getListing(uint256)"
# Hash và lấy 4 bytes đầu
selector = to_hex(keccak(sig.encode())[:4])
print(f"Function selector: {selector}")
```

## Lưu ý

- Function signature phải chính xác: `getListing(uint256)` (không có space, đúng chữ hoa/thường)
- Selector hiện tại trong code: `0x99a5d324` - CẦN VERIFY LẠI!
- Nếu selector sai, sẽ thấy lỗi "execution reverted" hoặc result = "0x0000..."

