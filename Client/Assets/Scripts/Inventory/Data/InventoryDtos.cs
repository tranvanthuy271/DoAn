using System;

// DTO mô phỏng dữ liệu nhận từ server cho item template (v3.0).
// Theo chuẩn LangLaServer – không có base_stat_json.
// Stat được tính từ option_template + upgradeLevel của từng item instance.
[Serializable]
public class ItemTemplateDto
{
    public int    id;
    public string name;
    public string detail;      // mô tả chi tiết
    public bool   isXepChong;  // có thể xếp chồng?
    public int    gioiTinh;    // 0=Male  1=Female  2=All
    public int    type;        // 0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring 21..30=misc
    public int    idClass;     // 0=All  1=Fire  2=Water  3=Earth  4=Metal  5=Wood (vũ khí)
    public int    idIcon;      // ID icon trong Unity Resources/ItemIcons/{idIcon}
    public int    levelNeed;
    public int    taiPhuNeed;
    public bool   isLock;      // loại item này bị khóa (VD: bạc khóa)
    public int    sellPrice;   // giá bán lại (đơn vị bạc)

    // Backward-compat aliases (v2 → v3)
    // Các property này chỉ dùng cho code cũ; không ảnh hưởng JSON deserialization.
    // description → detail
    public string description { get => detail; set => detail = value; }
    // icon_id (string) → idIcon (int)
    public string icon_id => idIcon > 0 ? idIcon.ToString() : null;
    // item_type → type
    public int item_type { get => type; set => type = value; }
    // stackable (bool) → isXepChong
    public bool stackable => isXepChong;
    // max_stack removed; default 99
    public int max_stack => 99;
    // category: 1=Equipment 2=Consumable 3=Material (tính từ type)
    public int category { get {
        if (type >= 0 && type <= 5) return 1;
        if (type == 22 || type == 23 || type == 24) return 2;
        return 3;
    } }
    // code removed from DB v3; synthesized as "ITEM_{id}"
    public string code => id > 0 ? $"ITEM_{id}" : null;
}

// DTO mô phỏng 1 ô trong túi đồ mà server gửi cho client (v3.0).
// upgradeLevel và strOptions chỉ có giá trị khi item là trang bị (type 0~5).
[Serializable]
public class InventorySlotDto
{
    public int    slotIndex;
    public int    id;           // item_template.id
    public int    amount;
    public bool   isEquipped;
    public bool   isLocked;     // item instance bị khóa (không thể drop/bán)
    public int    upgradeLevel; // bậc nâng cấp (+0~+20); 0 nếu không phải trang bị
    public string strOptions;   // "optId,value;..." ; "" nếu không phải trang bị

    // Backward-compat aliases (v2 → v3)
    // itemTemplateId → id
    public int itemTemplateId { get => id; set => id = value; }
    // quantity → amount
    public int quantity { get => amount; set => amount = value; }
    // itemCode: không còn trong server response; set bởi bridge layer để hiển thị
    public string itemCode;
    // iconId: không còn trong server response; set bởi bridge layer
    public string iconId;
}

