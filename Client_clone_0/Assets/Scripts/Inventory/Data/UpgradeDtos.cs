using System;
using System.Collections.Generic;

// ============================================================
// DTOs cho hệ thống nâng cấp trang bị (equipment upgrade)
// ============================================================

// Config 1 bậc nâng cấp – nhận từ GET /api/upgrade/config
[Serializable]
public class UpgradeConfigDto
{
    public int   targetLevel;       // bậc muốn đạt (+1 ~ +20)
    public int   silverCost;        // bạc cần
    public int   stoneId;           // item_template.id của đá nâng cấp chính
    public string stoneName;        // tên đá (hiển thị UI)
    public int   stoneNeeded;       // số đá để đạt tỉ lệ base đầy đủ
    public int   stoneMin;          // số đá tối thiểu (ít hơn = tỉ lệ 0)
    public float baseSuccessRate;   // 0.0 ~ 1.0 (không bao gồm đá may mắn)
    public int   failPolicy;        // 0=an toàn  1=giảm 1 bậc  2=về +0
}

// Request nâng cấp – gửi lên POST /api/upgrade/equipment
[Serializable]
public class UpgradeMaterialUsageDto
{
    public int slotIndex;
    public int count;
}

[Serializable]
public class UpgradeRequestDto
{
    public int                          playerId;
    public string                       slotKey;            // "weapon"/"helmet"/... hoặc slot index (từ inventory)
    public bool                         isFromInventory;    // true = item trong túi đồ, false = đang mặc
    public List<int>                    stoneSlotIndices;   // legacy fallback
    public List<int>                    charmSlotIndices;   // legacy fallback
    public List<UpgradeMaterialUsageDto> stoneUsages;       // slotIndex + count cho đá nâng cấp
    public List<UpgradeMaterialUsageDto> charmUsages;       // slotIndex + count cho đá may mắn
    public int                          clientRatePercent;  // tỉ lệ client tính (server so sánh để chống cheat)
}

// Response sau khi nâng cấp – nhận từ POST /api/upgrade/equipment
[Serializable]
public class UpgradeResponseDto
{
    public bool   success;
    public bool   downgraded;          // true = bị giảm bậc do thất bại
    public int    newUpgradeLevel;     // bậc mới của item
    public string updatedStrOptions;   // strOptions mới sau nâng cấp
    public int    silver;              // số bạc còn lại sau khi nâng cấp
    public int    actualRatePercent;   // tỉ lệ server đã dùng để roll
    public string message;             // thông báo từ server
    public FinalStats final_stats;      // base + equipment + potential — dùng update UI

    // Inventory đã cập nhật sau khi trừ đá
    public InventorySlotDto[] updatedInventory;
}

// Wrapper parse response từ GET /api/upgrade/options (option templates)
[Serializable]
public class OptionTemplatesResponse
{
    public OptionTemplateDto[] options;
}
