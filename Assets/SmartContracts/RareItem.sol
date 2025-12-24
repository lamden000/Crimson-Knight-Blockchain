// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract RareItem is ERC721, Ownable {
    uint256 public nextTokenId = 1;

    mapping(uint256 => string) private _tokenURIs;

    constructor() ERC721("RareItem", "RIT") Ownable(msg.sender) {}

    /// --------------------------------------------------
    /// ADMIN MINT (GIỮ LẠI ĐỂ TEST)
    /// --------------------------------------------------
    function adminMint(address to, string memory uri) external onlyOwner {
        _mintInternal(to, uri);
    }

    /// --------------------------------------------------
    /// USER SELF-MINT (NGƯỜI CHƠI TỰ KÝ)
    /// --------------------------------------------------
    function mintForMyself(string memory uri) external {
        _mintInternal(msg.sender, uri);
    }

    /// --------------------------------------------------
    /// INTERNAL MINT LOGIC (TRÁNH DUPLICATE)
    /// --------------------------------------------------
    function _mintInternal(address to, string memory uri) internal {
        uint256 tokenId = nextTokenId;
        _safeMint(to, tokenId);
        _tokenURIs[tokenId] = uri;
        nextTokenId++;
    }

    /// --------------------------------------------------
    /// TOKEN URI
    /// --------------------------------------------------
    function tokenURI(uint256 tokenId)
        public
        view
        override
        returns (string memory)
    {
        return _tokenURIs[tokenId];
    }
}
