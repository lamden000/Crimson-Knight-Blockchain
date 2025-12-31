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
    uint256[] public listedTokenIds; // Array để lưu danh sách tokenIds đang được list

    // Events để có thể query nhanh qua Alchemy API
    event ItemListed(uint256 indexed tokenId, address indexed seller, uint256 price);
    event ItemSold(uint256 indexed tokenId, address indexed seller, address indexed buyer, uint256 price);
    event ListingCancelled(uint256 indexed tokenId, address indexed seller);

    constructor(address nftAddress, address tokenAddress) {
        nft = IERC721(nftAddress);
        token = IERC20(tokenAddress);
    }

    // Người chơi list item để bán
    function listItem(uint256 tokenId, uint256 price) public {
        require(nft.ownerOf(tokenId) == msg.sender, "Not owner");
        require(price > 0, "Price must be > 0");
        require(listings[tokenId].price == 0, "Already listed"); // Đảm bảo chưa được list

        // Marketplace cần được approveNFT trước
        listings[tokenId] = Listing(msg.sender, price);
        listedTokenIds.push(tokenId); // Thêm vào array

        emit ItemListed(tokenId, msg.sender, price);
    }

    // Cancel listing
    function cancelListing(uint256 tokenId) public {
        Listing memory lst = listings[tokenId];
        require(lst.seller == msg.sender, "Not seller");

        delete listings[tokenId];
        _removeTokenIdFromList(tokenId); // Xóa khỏi array

        emit ListingCancelled(tokenId, msg.sender);
    }

    // Người mua mua item
    function buyItem(uint256 tokenId) public {
        Listing memory lst = listings[tokenId];
        require(lst.price > 0, "Not listed");

        address seller = lst.seller;
        uint256 price = lst.price;

        // 1) Người mua chuyển tiền cho người bán
        token.transferFrom(msg.sender, seller, price);

        // 2) Marketplace chuyển NFT cho người mua
        nft.safeTransferFrom(seller, msg.sender, tokenId);

        // 3) Xóa listing
        delete listings[tokenId];
        _removeTokenIdFromList(tokenId); // Xóa khỏi array

        emit ItemSold(tokenId, seller, msg.sender, price);
    }

    // Helper function để xóa tokenId khỏi array
    function _removeTokenIdFromList(uint256 tokenId) private {
        for (uint256 i = 0; i < listedTokenIds.length; i++) {
            if (listedTokenIds[i] == tokenId) {
                // Di chuyển phần tử cuối lên vị trí i và xóa phần tử cuối
                listedTokenIds[i] = listedTokenIds[listedTokenIds.length - 1];
                listedTokenIds.pop();
                break;
            }
        }
    }

    // View function để lấy số lượng listings
    function getListedCount() public view returns (uint256) {
        return listedTokenIds.length;
    }

    // View function để lấy danh sách tokenIds đang được list (có thể tốn gas nếu nhiều)
    function getListedTokenIds() public view returns (uint256[] memory) {
        return listedTokenIds;
    }

    // View function để lấy một range tokenIds (để tránh gas limit)
    function getListedTokenIdsRange(uint256 from, uint256 to) public view returns (uint256[] memory) {
        require(to >= from, "Invalid range");
        require(to < listedTokenIds.length, "Index out of bounds");
        
        uint256 length = to - from + 1;
        uint256[] memory result = new uint256[](length);
        
        for (uint256 i = 0; i < length; i++) {
            result[i] = listedTokenIds[from + i];
        }
        
        return result;
    }

    // View function để lấy thông tin listing của một tokenId
    function getListing(uint256 tokenId) public view returns (address seller, uint256 price, bool isListed) {
        Listing memory lst = listings[tokenId];
        return (lst.seller, lst.price, lst.price > 0);
    }
}
