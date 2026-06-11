using System;

// DTO cho 1 buff đang active trên client.
// Ánh xạ JSON từ server (ActiveBuff model) – dùng Unity JsonUtility.
[Serializable]
public class ActiveBuffDto
{
    // Loại hiệu ứng: GeneExpBuff, ExpBuff, PhucBuff, AttackBuff, DefenseBuff …
    public string effectType;

    // Giá trị buff (đơn vị %). Ví dụ: 20 → +20%.
    public int value;

    // ID icon trong IconDatabase.
    public int iconId;

    // Tên hiển thị trong tooltip.
    public string name;

    // Chi tiết hiển thị trong tooltip.
    public string detail;

    // Thời điểm hết hạn UTC theo định dạng ISO 8601 ("o").
    // Null/rỗng = buff vĩnh viễn hoặc instant (đã apply xong).
    public string expireAt;

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    // Trả về true nếu buff đã hết hạn.
    public bool IsExpired()
    {
        if (string.IsNullOrEmpty(expireAt)) return false; // permanent
        if (DateTime.TryParse(expireAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry))
            return DateTime.UtcNow >= expiry;
        return false;
    }

    // Số giây còn lại; -1 nếu permanent/instant.
    public float GetRemainingSeconds()
    {
        if (string.IsNullOrEmpty(expireAt)) return -1f;
        if (DateTime.TryParse(expireAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry))
            return (float)(expiry - DateTime.UtcNow).TotalSeconds;
        return 0f;
    }
}

// Wrapper dùng JsonUtility để deserialize { "active_buffs": [...] }.
[Serializable]
public class ActiveBuffsResponse
{
    public ActiveBuffDto[] active_buffs;
}
