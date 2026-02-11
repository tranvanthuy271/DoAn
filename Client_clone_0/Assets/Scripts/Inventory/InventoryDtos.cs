using System;

/// <summary>
/// DTO mô phỏng dữ liệu nhận từ server cho item template.
/// Unity dùng chủ yếu để hiển thị tên/mô tả nếu cần.
/// </summary>
[Serializable]
public class ItemTemplateDto
{
    public int id;
    public string code;
    public string name;
    public string description;
    public int category;
    public int itemType;
    public bool stackable;
    public int maxStack;
    public int rarity;
    public string iconId;
}

/// <summary>
/// DTO mô phỏng 1 ô trong túi đồ mà server gửi cho client.
/// Đây là struct mà UI Inventory sẽ dùng trực tiếp.
/// </summary>
[Serializable]
public class InventorySlotDto
{
    public int slotIndex;
    public int itemTemplateId;
    public string itemCode;
    public string iconId;
    public int quantity;
    public bool isEquipped;
}

