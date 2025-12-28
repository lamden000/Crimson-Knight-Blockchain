// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

/*
 * SpiritShard (GTK)
 * - ERC20 utility token cho DEMO
 * - Người dùng có thể tự mint (faucet)
 * - Không cooldown, không giới hạn
 * - Người gọi tự trả gas
 * - KHÔNG dùng cho production
 */
contract SpiritShard is ERC20, Ownable {
    uint256 public constant MINT_AMOUNT = 100 * 10 ** 18;

    constructor() ERC20("SpiritShard", "GTK") Ownable(msg.sender) {}

    // Owner mint (giữ lại cho debug / test)
    function mint(address to, uint256 amount) external onlyOwner {
        _mint(to, amount);
    }

    // Faucet: người dùng tự mint token để test flow
    function mintForYourself() external {
        _mint(msg.sender, MINT_AMOUNT);
    }

    // Mint với số lượng tùy chỉnh (1 coin = 1 token, với decimals = 18)
    // amount: số lượng token muốn mint (đã tính decimals, ví dụ: 100 tokens = 100 * 10^18)
    function mintAmount(uint256 amount) external {
        require(amount > 0, "Amount must be greater than 0");
        _mint(msg.sender, amount);
    }
}
