using System;

/// <summary>
/// DTO cho 1 buff đang active trên client.
/// Ánh xạ JSON từ server (ActiveBuff model) – dùng Unity JsonUtility.
/// </summary>
[Serializable]
public class ActiveBuffDto
{
    /// <summary>Loại hiệu ứng: GeneExpBuff, ExpBuff, PhucBuff, AttackBuff, DefenseBuff …</summary>
    public string effectType;

    /// <summary>Giá trị buff (đơn vị %). Ví dụ: 20 → +20%.</summary>
    public int value;

    /// <summary>ID icon trong IconDatabase.</summary>
    public int iconId;

    /// <summary>Tên hiển thị trong tooltip.</summary>
    public string name;

    /// <summary>Chi tiết hiển thị trong tooltip.</summary>
    public string detail;

    /// <summary>
    /// Thời điểm hết hạn UTC theo định dạng ISO 8601 ("o").
    /// Null/rỗng = buff vĩnh viễn hoặc instant (đã apply xong).
    /// </summary>
    public string expireAt;

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Trả về true nếu buff đã hết hạn.</summary>
    public bool IsExpired()
    {
        if (string.IsNullOrEmpty(expireAt)) return false; // permanent
        if (DateTime.TryParse(expireAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry))
            return DateTime.UtcNow >= expiry;
        return false;
    }

    /// <summary>Số giây còn lại; -1 nếu permanent/instant.</summary>
    public float GetRemainingSeconds()
    {
        if (string.IsNullOrEmpty(expireAt)) return -1f;
        if (DateTime.TryParse(expireAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry))
            return (float)(expiry - DateTime.UtcNow).TotalSeconds;
        return 0f;
    }
}

/// <summary>Wrapper dùng JsonUtility để deserialize { "active_buffs": [...] }.</summary>
[Serializable]
public class ActiveBuffsResponse
{
    public ActiveBuffDto[] active_buffs;
}
