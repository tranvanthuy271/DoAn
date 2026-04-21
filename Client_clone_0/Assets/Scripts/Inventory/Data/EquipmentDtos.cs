using System;

/// <summary>
/// Enum cho các loại slot trang bị
/// 1 Vũ khí + 5 Phụ kiện = 6 slots
/// </summary>
public enum EquipmentSlotType
{
    Weapon = 0,     // Vũ khí
    Helmet = 1,     // Mũ
    Armor = 2,      // Giáp
    Pants = 3,      // Quần
    Boots = 4,      // Giày
    Accessory = 5   // Phụ kiện (nhẫn/vòng cổ)
}

/// <summary>
/// DTO cho 1 ô trang bị đã equip
/// Khớp với cấu trúc JSON trong DB:
///   {"id":200,"upgradeLevel":5,"strOptions":"1,65;3,20;8,15"}
/// strOptions = "optionId,value;..." – value = strOption[upgradeLevel] từ option_template
/// </summary>
[Serializable]
public class EquipmentItemDto
{
    // Primary fields — must be public fields (not properties) for JsonUtility to deserialize
    public int    itemTemplateId;  // matches server JSON key "itemTemplateId"
    public int    upgradeLevel;
    public string strOptions;
    public string itemCode;
    public string iconId;
    public string itemName;
    public int    itemType;        // matches server JSON key "itemType" (0=Helmet,1=Weapon,...)

    // Backward-compat alias for any code using .id
    public int id { get => itemTemplateId; set => itemTemplateId = value; }
}

/// <summary>
/// DTO chứa toàn bộ trang bị của player (6 slots)
/// Tương ứng với equipment JSON trong DB
/// </summary>
[Serializable]
public class PlayerEquipmentDto
{
    public EquipmentItemDto weapon;
    public EquipmentItemDto helmet;
    public EquipmentItemDto armor;
    public EquipmentItemDto pants;
    public EquipmentItemDto boots;
    public EquipmentItemDto accessory;

    /// <summary>
    /// Lấy item theo slot type
    /// </summary>
    public EquipmentItemDto GetSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return weapon;
            case EquipmentSlotType.Helmet: return helmet;
            case EquipmentSlotType.Armor: return armor;
            case EquipmentSlotType.Pants: return pants;
            case EquipmentSlotType.Boots: return boots;
            case EquipmentSlotType.Accessory: return accessory;
            default: return null;
        }
    }

    /// <summary>
    /// Gán item vào slot type
    /// </summary>
    public void SetSlot(EquipmentSlotType slotType, EquipmentItemDto item)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: weapon = item; break;
            case EquipmentSlotType.Helmet: helmet = item; break;
            case EquipmentSlotType.Armor: armor = item; break;
            case EquipmentSlotType.Pants: pants = item; break;
            case EquipmentSlotType.Boots: boots = item; break;
            case EquipmentSlotType.Accessory: accessory = item; break;
        }
    }

    /// <summary>
    /// Xác định slot type dựa trên item_type từ DB
    /// Xác định slot type dựa trên item.type từ DB (v3.0)
    /// type: 0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring
    /// </summary>
    public static EquipmentSlotType? GetSlotTypeForItemType(int itemType)
    {
        switch (itemType)
        {
            case 0: return EquipmentSlotType.Helmet;
            case 1: return EquipmentSlotType.Weapon;
            case 2: return EquipmentSlotType.Armor;
            case 3: return EquipmentSlotType.Pants;
            case 4: return EquipmentSlotType.Boots;
            case 5: return EquipmentSlotType.Accessory;
            default: return null;
        }
    }

    /// <summary>
    /// Lấy tên hiển thị của slot
    /// </summary>
    public static string GetSlotDisplayName(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return "Vũ khí";
            case EquipmentSlotType.Helmet: return "Mũ";
            case EquipmentSlotType.Armor: return "Giáp";
            case EquipmentSlotType.Pants: return "Quần";
            case EquipmentSlotType.Boots: return "Giày";
            case EquipmentSlotType.Accessory: return "Phụ kiện";
            default: return "???";
        }
    }
}

/// <summary>
/// Request gửi lên server khi equip item
/// </summary>
[Serializable]
public class EquipItemRequest
{
    public int inventorySlotIndex;
}

/// <summary>
/// Request gửi lên server khi unequip item
/// </summary>
[Serializable]
public class UnequipItemRequest
{
    public string equipmentSlot; // "weapon", "helmet", "armor", "pants", "boots", "accessory"
}

/// <summary>
/// Response từ server sau khi equip/unequip
/// </summary>
[Serializable]
public class EquipmentResponse
{
    public string message;
    public PlayerEquipmentDto equipment;
}

public static class EquipmentPayloadParser
{
    public static PlayerEquipmentDto Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var wrapped = UnityEngine.JsonUtility.FromJson<EquipmentResponse>(json);
        if (wrapped != null && (wrapped.equipment != null || json.Contains("\"equipment\"")))
            return wrapped.equipment ?? new PlayerEquipmentDto();

        return UnityEngine.JsonUtility.FromJson<PlayerEquipmentDto>(json);
    }
}
