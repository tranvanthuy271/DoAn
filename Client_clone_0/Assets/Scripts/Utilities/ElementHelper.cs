/// <summary>
/// Ánh xạ trung tâm giữa element ID (số) ↔ English key (lưu DB/API) ↔ Tên Tiếng Việt (hiển thị)
/// Mỗi hệ có một giới tính cố định (không do người dùng chọn).
///
/// Thứ tự:
///   0 = Kim   (Metal) → Nam
///   1 = Mộc   (Wood)  → Nữ
///   2 = Thủy  (Water) → Nữ
///   3 = Hỏa   (Fire)  → Nam
///   4 = Thổ   (Earth) → Nam
///   5 = Phong (Wind)  → Nữ
/// </summary>
public static class ElementHelper
{
    public static readonly string[] VietnameseNames =
    {
        "Kim",   // 0
        "Mộc",   // 1
        "Thủy",  // 2
        "Hỏa",   // 3
        "Thổ",   // 4
        "Phong"  // 5
    };

    public static readonly string[] EnglishKeys =
    {
        "Metal",  // 0
        "Wood",   // 1
        "Water",  // 2
        "Fire",   // 3
        "Earth",  // 4
        "Wind"    // 5
    };

    /// <summary>Giới tính cố định của từng hệ (Male/Female) – dùng khi tạo nhân vật và spawn prefab</summary>
    public static readonly string[] Genders =
    {
        "Male",    // 0 Kim
        "Female",  // 1 Mộc
        "Female",  // 2 Thủy
        "Male",    // 3 Hỏa
        "Male",    // 4 Thổ
        "Female"   // 5 Phong
    };

    public const int Count = 6;

    /// <summary>Chuyển elementId → Tên Tiếng Việt để hiển thị UI</summary>
    public static string ToVietnamese(int elementId)
    {
        if (elementId < 0 || elementId >= VietnameseNames.Length)
            return "Không rõ";
        return VietnameseNames[elementId];
    }

    /// <summary>Chuyển English key (từ server/DB) → Tên Tiếng Việt</summary>
    public static string ToVietnamese(string englishKey)
    {
        if (string.IsNullOrEmpty(englishKey)) return "Không rõ";
        for (int i = 0; i < EnglishKeys.Length; i++)
        {
            if (string.Equals(EnglishKeys[i], englishKey, System.StringComparison.OrdinalIgnoreCase))
                return VietnameseNames[i];
        }
        return englishKey;
    }

    /// <summary>Chuyển elementId → English key để gửi lên API / lưu DB</summary>
    public static string ToEnglishKey(int elementId)
    {
        if (elementId < 0 || elementId >= EnglishKeys.Length)
            return "Fire";
        return EnglishKeys[elementId];
    }

    /// <summary>Chuyển English key → elementId (trả về -1 nếu không tìm thấy)</summary>
    public static int ToId(string englishKey)
    {
        if (string.IsNullOrEmpty(englishKey)) return -1;
        
        string key = englishKey.Trim();
        
        // 1. Check English Keys
        for (int i = 0; i < EnglishKeys.Length; i++)
        {
            if (string.Equals(EnglishKeys[i], key, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        
        // 2. Check Vietnamese Names (e.g., Phong, Kim)
        for (int i = 0; i < VietnameseNames.Length; i++)
        {
            if (string.Equals(VietnameseNames[i], key, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        
        // 3. Xử lý trường hợp không dấu
        if (string.Equals(key, "Moc", System.StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(key, "Thuy", System.StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(key, "Hoa", System.StringComparison.OrdinalIgnoreCase)) return 3;
        if (string.Equals(key, "Tho", System.StringComparison.OrdinalIgnoreCase)) return 4;

        // 4. Khả năng parse số
        if (int.TryParse(key, out int numericId) && IsValid(numericId))
            return numericId;

        return -1;
    }

    /// <summary>Lấy giới tính cố định (Male/Female) theo elementId</summary>
    public static string GetGender(int elementId)
    {
        if (elementId < 0 || elementId >= Genders.Length) return "Male";
        return Genders[elementId];
    }

    /// <summary>Lấy giới tính cố định theo English key</summary>
    public static string GetGender(string englishKey) => GetGender(ToId(englishKey));

    /// <summary>Id có hợp lệ không (0–5)?</summary>
    public static bool IsValid(int elementId) => elementId >= 0 && elementId < Count;

    // ─── Counter / Immunity ────────────────────────────────────────────────

    /// <summary>
    /// Trả về true nếu player Hybrid miễn nhiễm với element tấn công.
    /// Gọi trong DamageCalculator TRƯỚC khi áp -25% ATK penalty counter.
    ///
    /// Sử dụng:
    ///   if (ElementHelper.IsImmuneToCounter(attackerElement, targetPlayer)) skip penalty;
    /// </summary>
    /// <param name="attackerElement">English key của hệ người tấn công (ví dụ: "Water").</param>
    /// <param name="target">PlayerDataResponse của người bị tấn công.</param>
    public static bool IsImmuneToCounter(string attackerElement, PlayerDataResponse target)
    {
        if (target == null || !target.is_hybrid) return false;
        if (string.IsNullOrEmpty(target.hybrid_immune_elements)) return false;

        var immuneList = target.hybrid_immune_elements
            .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var immune in immuneList)
            if (string.Equals(immune.Trim(), attackerElement, System.StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    /// <summary>
    /// Trả về hệ sẽ bị hệ này khắc (theo Ngũ Hành Tương Khắc).
    /// English key → English key bị khắc.
    /// Wind không tham gia vòng khắc chuẩn (trả về null).
    /// </summary>
    public static string GetCounteredElement(string englishKey) => englishKey switch
    {
        "Metal" => "Wood",
        "Wood"  => "Water",
        "Water" => "Fire",
        "Fire"  => "Earth",
        "Earth" => "Metal",
        _       => null
    };

    /// <summary>
    /// Trả về hệ phụ CỐ ĐỊNH theo cặp hybrid của game:
    ///   Hỏa ↔ Thổ  |  Thủy ↔ Mộc  |  Kim ↔ Phong
    /// Trả về null nếu englishKey không hợp lệ.
    /// </summary>
    public static string GetFixedSecondary(string englishKey) => englishKey switch
    {
        "Fire"  => "Earth",
        "Earth" => "Fire",
        "Water" => "Wood",
        "Wood"  => "Water",
        "Metal" => "Wind",
        "Wind"  => "Metal",
        _       => null
    };
}
