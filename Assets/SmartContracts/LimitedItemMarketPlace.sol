// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC1155/IERC1155.sol";
import "@openzeppelin/contracts/token/ERC20/IERC20.sol";

contract Marketplace1155 {
    struct Listing {
        address seller;
        uint256 tokenId;
        uint256 amount;
        uint256 pricePerUnit;
    }

    IERC1155 public nft;
    IERC20 public paymentToken;

    // tokenId => listing
    mapping(uint256 => Listing) public listings;

    constructor(address nftAddress, address paymentTokenAddress) {
        nft = IERC1155(nftAddress);
        paymentToken = IERC20(paymentTokenAddress);
    }

    /// @notice List ERC1155 item
    function listItem(
        uint256 tokenId,
        uint256 amount,
        uint256 pricePerUnit
    ) external {
        require(amount > 0, "Amount must be > 0");
        require(pricePerUnit > 0, "Price must be > 0");
        require(
            nft.balanceOf(msg.sender, tokenId) >= amount,
            "Not enough balance"
        );
        require(
            nft.isApprovedForAll(msg.sender, address(this)),
            "Marketplace not approved"
        );

        listings[tokenId] = Listing({
            seller: msg.sender,
            tokenId: tokenId,
            amount: amount,
            pricePerUnit: pricePerUnit
        });
    }

    /// @notice Cancel listing
    function cancelListing(uint256 tokenId) external {
        Listing memory lst = listings[tokenId];
        require(lst.seller == msg.sender, "Not seller");

        delete listings[tokenId];
    }

    /// @notice Buy item
    function buyItem(uint256 tokenId, uint256 buyAmount) external {
        Listing storage lst = listings[tokenId];
        require(lst.amount >= buyAmount, "Not enough listed");
        require(buyAmount > 0, "Invalid amount");

        uint256 totalPrice = buyAmount * lst.pricePerUnit;

        // Transfer payment
        require(
            paymentToken.transferFrom(
                msg.sender,
                lst.seller,
                totalPrice
            ),
            "Payment failed"
        );

        // Transfer NFT
        nft.safeTransferFrom(
            lst.seller,
            msg.sender,
            tokenId,
            buyAmount,
            ""
        );

        // Update listing
        lst.amount -= buyAmount;
        if (lst.amount == 0) {
            delete listings[tokenId];
        }
    }
}
