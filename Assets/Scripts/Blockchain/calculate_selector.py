#!/usr/bin/env python3
"""
Script để tính function selector cho getListing(uint256)
Cần cài: pip install eth-utils
"""

try:
    from eth_utils import keccak, to_hex
    
    # Function signature
    function_signature = "getListing(uint256)"
    
    # Hash function signature và lấy 4 bytes đầu
    hash_bytes = keccak(function_signature.encode('utf-8'))
    selector = to_hex(hash_bytes[:4])
    
    print(f"Function signature: {function_signature}")
    print(f"Function selector: {selector}")
    print(f"\nNếu selector khác với {selector}, hãy:")
    print("1. Kiểm tra function signature có đúng không")
    print("2. Verify trong Remix bằng cách xem 'input data' khi gọi function")
    
except ImportError:
    print("Cần cài đặt: pip install eth-utils")
    print("\nHoặc dùng cách khác:")
    print("1. Mở Remix, compile contract")
    print("2. Gọi getListing(3)")
    print("3. Xem 'input data' trong transaction")
    print("4. 4 bytes đầu (0x...) chính là selector")

