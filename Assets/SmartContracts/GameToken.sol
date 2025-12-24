// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract SpiritShard is ERC20, Ownable {
    constructor() ERC20("SpiritShard", "GTK") Ownable(msg.sender) {}

    // Owner được quyền mint token để thưởng người chơi
    function mint(address to, uint256 amount) public onlyOwner {
        _mint(to, amount);
    }
}
