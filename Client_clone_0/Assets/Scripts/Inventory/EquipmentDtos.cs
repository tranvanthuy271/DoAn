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
/// </summary>
[Serializable]
public class EquipmentItemDto
{
    public int itemTemplateId;
    public string itemCode;
    public string iconId;
    public string itemName;
    public int itemType;
    public string baseStatJson;
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
    /// Equipment items (category=1): item_type 1,2=Weapon, 3=Armor, 4=Helmet, 5=Pants, 6=Boots, 7=Accessory
    /// </summary>
    public static EquipmentSlotType? GetSlotTypeForItemType(int category, int itemType)
    {
        // Chỉ equipment (category=1) mới trang bị được
        if (category != 1) return null;

        switch (itemType)
        {
            case 1: return EquipmentSlotType.Weapon;    // Sword / Melee
            case 2: return EquipmentSlotType.Weapon;    // Bow / Ranged (cũng vào slot weapon)
            case 3: return EquipmentSlotType.Armor;     // Armor
            case 4: return EquipmentSlotType.Helmet;    // Helmet
            case 5: return EquipmentSlotType.Pants;     // Pants
            case 6: return EquipmentSlotType.Boots;     // Boots
            case 7: return EquipmentSlotType.Accessory; // Ring/Necklace
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
