// CloudScript function để update Title Data
// Function name: UpdateMarketplaceListings
// 
// Cách setup trong PlayFab Dashboard:
// 1. Vào PlayFab Dashboard > Your Game > Automation > CloudScript
// 2. Tạo new CloudScript function với tên "UpdateMarketplaceListings"
// 3. Copy code này vào
// 4. Save và Publish

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

