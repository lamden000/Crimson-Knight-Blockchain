using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Editor script để tự động copy file HTML từ BlockchainWeb vào StreamingAssets và build folder sau khi build
/// </summary>
public class CopyWithdrawWebToBuild
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // Đường dẫn tới thư mục BlockchainWeb trong Assets
        string sourceFolder = Path.Combine(Application.dataPath, "BlockchainWeb");
        
        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning($"[CopyWithdrawWebToBuild] Không tìm thấy thư mục BlockchainWeb tại: {sourceFolder}");
            return;
        }

        // 1. Copy vào StreamingAssets (để có thể truy cập từ runtime)
        string streamingAssetsDest = Path.Combine(Application.streamingAssetsPath, "BlockchainWeb");
        CopyFolder(sourceFolder, streamingAssetsDest, "StreamingAssets");

        // 2. Copy vào build folder root (để có thể chạy HTTP server)
        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        string buildDest = Path.Combine(buildFolder, "BlockchainWeb");
        CopyFolder(sourceFolder, buildDest, "Build Folder");
    }

    /// <summary>
    /// Copy toàn bộ folder và file HTML
    /// </summary>
    private static void CopyFolder(string sourceFolder, string destFolder, string locationName)
    {
        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
            Debug.Log($"[CopyWithdrawWebToBuild] Đã tạo thư mục tại {locationName}: {destFolder}");
        }

        // Copy tất cả file HTML và các file liên quan
        string[] filesToCopy = Directory.GetFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly);
        int copiedCount = 0;

        foreach (string sourceFile in filesToCopy)
        {
            // Bỏ qua file .meta
            if (sourceFile.EndsWith(".meta"))
                continue;

            string fileName = Path.GetFileName(sourceFile);
            string destFile = Path.Combine(destFolder, fileName);

            try
            {
                File.Copy(sourceFile, destFile, true);
                copiedCount++;
                Debug.Log($"[CopyWithdrawWebToBuild] ✅ Đã copy {fileName} vào {locationName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CopyWithdrawWebToBuild] ❌ Lỗi copy {fileName}: {e.Message}");
            }
        }

        Debug.Log($"[CopyWithdrawWebToBuild] ✅ Hoàn thành copy {copiedCount} files vào {locationName}: {destFolder}");
    }
}

