using System;
using System.Collections.Generic;

// ============================================================
// DTOs cho hệ thống nâng cấp Gene
// Endpoint: GET /api/gene/config   POST /api/gene/upgrade
// ============================================================

/// <summary>
/// Config 1 bậc nâng cấp gene – nhận từ GET /api/gene/config
/// </summary>
[Serializable]
public class GeneConfigDto
{
    public int    tierFrom;           // gene tier hiện tại
    public int    tierTo;             // gene tier sau khi nâng cấp
    public string elementType;        // Fire / Water / Earth / Metal / Wood

    public int    geneExpRequired;    // gene_exp cần có
    public int    goldCost;           // vàng tiêu hao
    public int    itemId;             // item_template.id cần dùng
    public string itemName;           // tên item (hiển thị UI)
    public int    itemIcon;           // idIcon
    public int    itemsMin;           // số item tối thiểu
    public int    itemsNeeded;        // số item để đạt tỉ lệ tối đa
    public float  baseSuccessRate;    // tỉ lệ khi dùng đủ itemsNeeded

    public GeneStatBonus statBonus;   // chỉ số sẽ tăng khi thành công
    public GeneSkillUnlock[] skillsToUnlock; // skill sẽ mở khoá khi thành công
}

[Serializable]
public class GeneStatBonus
{
    public int hp;
    public int mp;
    public int attack;
    public int defense;
}

[Serializable]
public class GeneSkillUnlock
{
    public int    skillId;
    public string skillName;
    public string iconId;
}

/// <summary>
/// Request nâng cấp gene – gửi lên POST /api/gene/upgrade
/// </summary>
[Serializable]
public class GeneUpgradeRequest
{
    public int playerId;
    public int itemCount;    // số item muốn dùng (>= itemsMin)
}

/// <summary>
/// Response sau khi nâng cấp gene – nhận từ POST /api/gene/upgrade
/// </summary>
[Serializable]
public class GeneUpgradeResponse
{
    public bool   success;
    public int    newGeneTier;
    public int    newGeneExp;
    public int    gold;               // vàng còn lại sau khi trừ
    public string message;
    public GeneNewStats newStats;     // chỉ số mới (null nếu thất bại)
    public GeneSkillUnlock[] newlyUnlockedSkills;
}

[Serializable]
public class GeneNewStats
{
    public int maxHp;
    public int maxMp;
    public int attack;
    public int defense;
}
