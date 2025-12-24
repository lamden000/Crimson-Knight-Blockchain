// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC721/IERC721.sol";
import "@openzeppelin/contracts/token/ERC20/IERC20.sol";

contract Marketplace {
    struct Listing {
        address seller;
        uint256 price;
    }

    IERC721 public nft;
    IERC20 public token;

    mapping(uint256 => Listing) public listings;

    constructor(address nftAddress, address tokenAddress) {
        nft = IERC721(nftAddress);
        token = IERC20(tokenAddress);
    }

    // Người chơi list item để bán
    function listItem(uint256 tokenId, uint256 price) public {
        require(nft.ownerOf(tokenId) == msg.sender, "Not owner");
        require(price > 0, "Price must be > 0");

        // Marketplace cần được approveNFT trước
        listings[tokenId] = Listing(msg.sender, price);
    }

    // Cancel listing
    function cancelListing(uint256 tokenId) public {
        Listing memory lst = listings[tokenId];
        require(lst.seller == msg.sender, "Not seller");

        delete listings[tokenId];
    }

    // Người mua mua item
    function buyItem(uint256 tokenId) public {
        Listing memory lst = listings[tokenId];
        require(lst.price > 0, "Not listed");

        // 1) Người mua chuyển tiền cho người bán
        token.transferFrom(msg.sender, lst.seller, lst.price);

        // 2) Marketplace chuyển NFT cho người mua
        nft.safeTransferFrom(lst.seller, msg.sender, tokenId);

        // 3) Xóa listing
        delete listings[tokenId];
    }
}
