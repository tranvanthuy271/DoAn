using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Dịch vụ lưu trữ an toàn cấp thiết bị (Device-Bound Secure Storage).
/// Sử dụng thuật toán AES-256 với Khóa mã hóa (Key) được sinh động từ Hardware ID của thiết bị (SystemInfo.deviceUniqueIdentifier).
/// Ngăn chặn việc sao chép file cấu hình sang máy khác và bảo vệ dữ liệu nhạy cảm khỏi việc đọc trộm plaintext.
/// </summary>
public static class SecureStorage
{
    private static readonly byte[] Key;

    static SecureStorage()
    {
        // 1. Lấy mã định danh phần cứng duy nhất của thiết bị
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
        {
            // Dự phòng nếu OS không hỗ trợ trả về Hardware ID (an toàn hơn là crash)
            deviceId = "MutantsArenaSecureKeyFallback_5a9d8c7b!";
        }

        // 2. Hash mã phần cứng kèm salt để tạo khóa AES 256-bit (32 bytes)
        using (var sha256 = SHA256.Create())
        {
            string salt = "MutantsArena_DeviceBoundSalt_@2026!";
            Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId + salt));
        }
    }

    /// <summary>
    /// Mã hóa và lưu chuỗi vào PlayerPrefs
    /// </summary>
    public static void SaveString(string key, string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                return;
            }

            string encrypted = Encrypt(value);
            PlayerPrefs.SetString(key, encrypted);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SecureStorage] Lỗi khi lưu dữ liệu an toàn cho key '{key}': {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc và giải mã chuỗi từ PlayerPrefs
    /// </summary>
    public static string GetString(string key, string defaultValue = "")
    {
        if (!PlayerPrefs.HasKey(key)) return defaultValue;

        try
        {
            string encrypted = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(encrypted)) return defaultValue;

            return Decrypt(encrypted);
        }
        catch (Exception ex)
        {
            // Đề phòng trường hợp dữ liệu cũ chưa mã hóa còn sót lại hoặc bị hỏng, trả về default
            Debug.LogWarning($"[SecureStorage] Không thể giải mã key '{key}', chuyển về giá trị mặc định. Chi tiết: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Xóa dữ liệu
    /// </summary>
    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    #region Crypto Core (AES-256 với IV ngẫu nhiên)

    private static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.GenerateIV(); // Tạo IV ngẫu nhiên cho mỗi lần mã hóa
            byte[] iv = aes.IV;

            using (MemoryStream ms = new MemoryStream())
            {
                // Viết 16 bytes IV vào đầu Stream để phục vụ giải mã
                ms.Write(iv, 0, iv.Length);

                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private static string Decrypt(string cipherText)
    {
        byte[] cipherBytes = Convert.FromBase64String(cipherText);
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            byte[] iv = new byte[aes.BlockSize / 8]; // 16 bytes IV cho AES

            if (cipherBytes.Length < iv.Length)
            {
                throw new InvalidDataException("Dữ liệu mã hóa không hợp lệ (kích thước quá nhỏ).");
            }

            // Đọc 16 bytes IV ở đầu mảng
            Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using (MemoryStream ms = new MemoryStream())
            {
                // Đọc phần dữ liệu mã hóa còn lại (bỏ qua 16 bytes IV)
                ms.Write(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
                ms.Position = 0;

                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }

    #endregion
}
