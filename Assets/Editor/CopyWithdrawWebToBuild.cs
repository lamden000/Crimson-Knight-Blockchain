using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Editor script để tự động copy file HTML từ WithdrawWeb vào build folder sau khi build
/// </summary>
public class CopyWithdrawWebToBuild
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // Lấy đường dẫn tới thư mục build
        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        
        // Đường dẫn tới thư mục WithdrawWeb trong Assets
        string sourceFolder = Path.Combine(Application.dataPath, "WithdrawWeb");
        
        // Đường dẫn tới thư mục WithdrawWeb trong build folder
        string destFolder = Path.Combine(buildFolder, "WithdrawWeb");
        
        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning($"[CopyWithdrawWebToBuild] Không tìm thấy thư mục WithdrawWeb tại: {sourceFolder}");
            return;
        }
        
        // Tạo thư mục đích nếu chưa có
        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
            Debug.Log($"[CopyWithdrawWebToBuild] Đã tạo thư mục: {destFolder}");
        }
        
        // Copy tất cả file HTML
        string[] htmlFiles = { "index.html", "withdraw-coin.html", "link-wallet.html" };
        foreach (string fileName in htmlFiles)
        {
            string sourceFile = Path.Combine(sourceFolder, fileName);
            string destFile = Path.Combine(destFolder, fileName);
            
            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, destFile, true);
                Debug.Log($"[CopyWithdrawWebToBuild] Đã copy {fileName} từ {sourceFile} tới {destFile}");
            }
            else
            {
                Debug.LogWarning($"[CopyWithdrawWebToBuild] Không tìm thấy file: {sourceFile}");
            }
        }
        
        Debug.Log($"[CopyWithdrawWebToBuild] Hoàn thành copy file HTML vào build folder: {destFolder}");
    }
}

