using UnityEngine;

/// <summary>
/// Đọc file server_config.json từ StreamingAssets hoặc cùng thư mục build.
/// Cho phép thay đổi IP/port mà không cần rebuild Unity.
/// </summary>
public static class ServerConfigFileReader
{
    private const string FileName = "server_config.json";

    /// <summary>
    /// Trả về nội dung JSON hoặc null nếu file không tồn tại.
    /// Ưu tiên: StreamingAssets → cùng thư mục exe → null.
    /// </summary>
    public static string ReadConfigJson()
    {
        // 1 — StreamingAssets (hoạt động trên mọi platform)
        string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, FileName);
        if (TryRead(streamingPath, out string json))
            return json;

        // 2 — Cùng thư mục với executable (tiện cho server build)
        string exeDir = System.IO.Path.GetDirectoryName(Application.dataPath); // parent of _Data
        if (!string.IsNullOrEmpty(exeDir))
        {
            string exePath = System.IO.Path.Combine(exeDir, FileName);
            if (TryRead(exePath, out json))
                return json;
        }

        return null;
    }

    private static bool TryRead(string path, out string content)
    {
        content = null;
        try
        {
            if (System.IO.File.Exists(path))
            {
                content = System.IO.File.ReadAllText(path);
                Debug.Log($"[ServerConfigFileReader] Loaded config from: {path}");
                return true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ServerConfigFileReader] Không đọc được {path}: {ex.Message}");
        }
        return false;
    }
}
