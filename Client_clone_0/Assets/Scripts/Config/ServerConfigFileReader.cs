using UnityEngine;

// Đọc file server_config.json từ StreamingAssets hoặc cùng thư mục build.
// Cho phép thay đổi IP/port mà không cần rebuild Unity.
public static class ServerConfigFileReader
{
    private const string FileName = "server_config.json";

    // Trả về nội dung JSON hoặc null nếu file không tồn tại.
    // Ưu tiên: StreamingAssets → cùng thư mục exe → null.
    public static string ReadConfigJson()
    {
        return TryReadConfig(out string json, out _, out _) ? json : null;
    }

    public static bool TryReadConfig(out string json, out string path, out long lastWriteUtcTicks)
    {
        json = null;
        path = null;
        lastWriteUtcTicks = 0;

        // 1 — StreamingAssets (hoạt động trên mọi platform)
        string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, FileName);
        if (TryRead(streamingPath, out json, out lastWriteUtcTicks))
        {
            path = streamingPath;
            return true;
        }

        // 2 — Cùng thư mục với executable (tiện cho server build)
        string exeDir = System.IO.Path.GetDirectoryName(Application.dataPath); // parent of _Data
        if (!string.IsNullOrEmpty(exeDir))
        {
            string exePath = System.IO.Path.Combine(exeDir, FileName);
            if (TryRead(exePath, out json, out lastWriteUtcTicks))
            {
                path = exePath;
                return true;
            }
        }

        return false;
    }

    private static bool TryRead(string path, out string content, out long lastWriteUtcTicks)
    {
        content = null;
        lastWriteUtcTicks = 0;
        try
        {
            var fileInfo = new System.IO.FileInfo(path);
            if (fileInfo.Exists)
            {
                content = System.IO.File.ReadAllText(path);
                lastWriteUtcTicks = fileInfo.LastWriteTimeUtc.Ticks;
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
