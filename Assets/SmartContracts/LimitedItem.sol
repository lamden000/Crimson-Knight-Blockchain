// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC1155/ERC1155.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract LimitedItems is ERC1155, Ownable {
    struct ItemInfo {
        uint256 maxSupply;
        uint256 minted;
        string uri;
    }

    mapping(uint256 => ItemInfo) public items;

    constructor() ERC1155("") Ownable(msg.sender) {}

    /// --------------------------------------------------
    /// CREATE ITEM (ADMIN ONLY)
    /// --------------------------------------------------
    function createItem(
        uint256 itemId,
        uint256 maxSupply,
        string memory metadataURI
    ) external onlyOwner {
        require(maxSupply > 0, "Supply must be > 0");
        require(items[itemId].maxSupply == 0, "Item already exists");

        items[itemId] = ItemInfo({
            maxSupply: maxSupply,
            minted: 0,
            uri: metadataURI
        });
    }

    /// --------------------------------------------------
    /// ADMIN MINT (GIỮ ĐỂ TEST)
    /// --------------------------------------------------
    function adminMint(
        address to,
        uint256 itemId,
        uint256 amount
    ) external onlyOwner {
        _mintInternal(to, itemId, amount);
    }

    /// --------------------------------------------------
    /// USER SELF-MINT (NGƯỜI CHƠI TỰ KÝ)
    /// --------------------------------------------------
    function mintForMyself(
        uint256 itemId,
        uint256 amount
    ) external {
        _mintInternal(msg.sender, itemId, amount);
    }

    /// --------------------------------------------------
    /// INTERNAL MINT LOGIC (CHUNG)
    /// --------------------------------------------------
    function _mintInternal(
        address to,
        uint256 itemId,
        uint256 amount
    ) internal {
        ItemInfo storage item = items[itemId];

        require(item.maxSupply > 0, "Item does not exist");
        require(amount > 0, "Amount must be > 0");
        require(
            item.minted + amount <= item.maxSupply,
            "Exceeds max supply"
        );

        item.minted += amount;
        _mint(to, itemId, amount, "");
    }

    /// --------------------------------------------------
    /// METADATA URI
    /// --------------------------------------------------
    function uri(uint256 id)
        public
        view
        override
        returns (string memory)
    {
        return items[id].uri;
    }
}
