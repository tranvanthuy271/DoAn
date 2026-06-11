using System;
using UnityEngine;

// ============================================================
// OptionTemplateDto  –  option_template từ DB
//
// type: 0=vũ khí base   2=giáp/nhẫn base
//       3=(+4)unlock    4=(+8)unlock
//       5=(+12)unlock   6=(+16)unlock
//
// level: bậc nâng cấp tối thiểu để option ACTIVE
//   item.upgradeLevel <  option.level → DIM  (màu xám, chưa đạt)
//   item.upgradeLevel >= option.level → BRIGHT (màu trắng/vàng, đang hoạt động)
//
// strOption: 20 giá trị cách nhau ';'
//   index N = tổng stat khi item đang ở bậc +N
//
// *** HƯỚNG DẪN HIỂN THỊ TRONG ItemDetailPanel ***
//
// 1. Parse item.strOptions → List<(optId, storedValue)>
//    (storedValue = strOption[upgradeLevel] đã tính sẵn phía server,
//     dùng để hiển thị; không cần parse lại strOption phía client)
//
// 2. Với mỗi (optId, storedValue):
//    var tmpl = optionTemplates[optId];
//
// 3. Xây label:
//    string label = tmpl.name.Replace("#", storedValue.ToString());
//    // Nếu là unlock option, thêm (+N) prefix:
//    if (tmpl.level > 0)
//        label = $"(+{tmpl.level}) " + label;
//
// 4. Màu sắc:
//    Color color = tmpl.IsActive(item.upgradeLevel) ? Color.white : Color.gray;
//
// 5. Gán text + color vào UI Text/TMP element.
//
// VÍ DỤ:
//   tmpl.level=4, item.upgradeLevel=3  → "(+4) HP tối đa: +79"  [dim/gray]
//   tmpl.level=4, item.upgradeLevel=5  → "(+4) HP tối đa: +99"  [bright/white]
//   tmpl.level=0, item.upgradeLevel=2  → "Tấn công: +17"        [bright/white]
// ============================================================

[Serializable]
public class OptionTemplateDto
{
    public int    id;
    public string name;      // tên hiển thị, '#' là placeholder cho giá trị số
    public int    type;      // 0=weapon-base 2=armor-base 3=(+4) 4=(+8) 5=(+12) 6=(+16)
    public int    level;     // min item upgradeLevel để kích hoạt; 0=luôn active

    // 20 giá trị cách nhau ';': index N = stat value tại bậc +N
    public string strOption;

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    // Lấy giá trị stat tại upgradeLevel chỉ định.
    public int GetValueAt(int upgradeLevel)
    {
        if (string.IsNullOrEmpty(strOption)) return 0;
        var parts = strOption.Split(';');
        int idx = Mathf.Clamp(upgradeLevel, 0, parts.Length - 1);
        return int.TryParse(parts[idx], out int v) ? v : 0;
    }

    // Option đang hoạt động (sáng màu) khi item đạt đủ bậc nâng cấp.
    public bool IsActive(int itemUpgradeLevel) => itemUpgradeLevel >= level;

    // Xây chuỗi hiển thị đầy đủ, ví dụ "(+4) HP tối đa: +79"
    public string BuildLabel(int value)
    {
        string label = name.Replace("#", value.ToString());
        return level > 0 ? $"(+{level}) {label}" : label;
    }
}

// ============================================================
// EquippedOptionDisplay  –  helper để ItemDetailPanel dùng
// ============================================================

// Một option đã parse từ EquipmentItemDto.strOptions hoặc InventorySlotDto.strOptions.
[Serializable]
public struct EquippedOptionDisplay
{
    public int optionId;
    public int value;   // giá trị đã tính (= strOption[upgradeLevel] từ server)

    // Parse strOptions string thành array EquippedOptionDisplay.
    // strOptions format: "optId,value;optId,value;..."
    public static EquippedOptionDisplay[] ParseAll(string strOptions)
    {
        if (string.IsNullOrEmpty(strOptions))
            return Array.Empty<EquippedOptionDisplay>();

        var pairs = strOptions.Split(';');
        var result = new EquippedOptionDisplay[pairs.Length];
        int count = 0;
        foreach (var pair in pairs)
        {
            var kv = pair.Split(',');
            if (kv.Length == 2
                && int.TryParse(kv[0], out int optId)
                && int.TryParse(kv[1], out int val))
            {
                result[count++] = new EquippedOptionDisplay { optionId = optId, value = val };
            }
        }
        if (count < result.Length)
            Array.Resize(ref result, count);
        return result;
    }
}

[Serializable]
public class OptionTemplateListWrapper
{
    public OptionTemplateDto[] options;
}
