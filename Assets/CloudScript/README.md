# CloudScript Setup Guide

## Tổng quan

PlayFab hỗ trợ **CloudScript** - một tính năng cho phép chạy server-side JavaScript functions. Điều này rất hữu ích vì:

1. **Client không thể write Title Data trực tiếp** - chỉ có thể read
2. **CloudScript có thể write Title Data** - vì chạy server-side với quyền admin
3. **Bảo mật tốt hơn** - logic nhạy cảm chạy trên server, không expose cho client

## Setup CloudScript Function

### Bước 1: Tạo CloudScript Function trong PlayFab Dashboard

1. Đăng nhập vào [PlayFab Dashboard](https://developer.playfab.com/)
2. Chọn game của bạn
3. Vào **Automation** > **CloudScript**
4. Click **New Function** hoặc **Edit** nếu đã có
5. Tạo function với tên: `UpdateMarketplaceListings`

### Bước 2: Copy Code

Copy code từ file `UpdateMarketplaceListings.js` vào CloudScript editor:

```javascript
handlers.UpdateMarketplaceListings = function (args, context) {
    // Validate input
    if (!args || !args.key || !args.value) {
        return {
            success: false,
            error: "Missing required parameters: key and value"
        };
    }

    // Update Title Data
    var updateRequest = {
        Key: args.key,
        Value: args.value
    };

    var updateResult = server.SetTitleData(updateRequest);

    if (updateResult) {
        return {
            success: true,
            message: "Title Data updated successfully"
        };
    } else {
        return {
            success: false,
            error: "Failed to update Title Data"
        };
    }
};
```

### Bước 3: Save và Publish

1. Click **Save** để lưu function
2. Click **Publish** để publish function (cần publish thì client mới gọi được)

## Cách hoạt động

1. **Client gọi CloudScript**: Unity client gọi `ExecuteCloudScript` với function name `UpdateMarketplaceListings`
2. **CloudScript chạy server-side**: Function chạy trên PlayFab server với quyền admin
3. **Update Title Data**: CloudScript gọi `server.SetTitleData()` để update Title Data
4. **Return result**: CloudScript return kết quả về client

## Lưu ý

- **CloudScript có giới hạn**: 
  - Execution time: 10 seconds
  - Memory: 128 MB
  - API calls: Có giới hạn rate limit
- **Title Data size limit**: Mỗi key có thể lưu tối đa 10KB
- **Fallback**: Nếu CloudScript fail, code sẽ tự động fallback về User Data

## Testing

Sau khi setup, test bằng cách:
1. List một item trong game
2. Check PlayFab Dashboard > **Settings** > **Title Data** để xem data đã được update chưa
3. Check Unity Console để xem logs

## Troubleshooting

- **Error: Function not found**: Đảm bảo đã publish CloudScript function
- **Error: Missing parameters**: Check xem `key` và `value` có được pass đúng không
- **Error: Permission denied**: Đảm bảo CloudScript function có quyền gọi `server.SetTitleData()`

