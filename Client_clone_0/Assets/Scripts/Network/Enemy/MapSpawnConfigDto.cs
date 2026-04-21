using System;

/// <summary>
/// MapSpawnConfigDto — Data Transfer Objects cho endpoint GET /api/map/{mapId}/spawn-config.
///
/// Dùng JsonUtility.FromJson&lt;MapSpawnConfigResponse&gt;(json) để deserialize.
/// Tất cả class phải [Serializable] để JsonUtility hoạt động.
/// </summary>
/// 
// ─────────────────────────────────────────────────────────────────────────────
//  Root response
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class MapSpawnConfigResponse
{
    /// <summary>Map ID trả về từ server (dùng để verify đúng map).</summary>
    public int map_id;

    /// <summary>
    /// Danh sách vị trí spawn — mỗi phần tử = 1 điểm spawn trên map.
    /// Cùng enemy_id có thể xuất hiện nhiều lần (các vị trí khác nhau).
    /// </summary>
    public SpawnEntry[] spawns;

    /// <summary>
    /// Skills + reward của từng loại quái — mỗi phần tử = 1 enemy_id.
    /// Dữ liệu đến từ cột skills_json và reward_json trong bảng enemy.
    /// Host dùng để set EnemySkillSet, EnemyItemDrop và HP ngay sau khi spawn.
    /// </summary>
    public EnemySkillsEntry[] enemy_skills;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Spawn entry — 1 điểm spawn (chỉ chứa vị trí + tham số spawn)
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class SpawnEntry
{
    /// <summary>ID loại quái — map sang EnemyPrefabManager.GetEnemyPrefab(enemy_id).</summary>
    public int enemy_id;

    /// <summary>Tọa độ X (world space Unity) — vị trí spawn trên map.</summary>
    public float cx;

    /// <summary>Tọa độ Y (world space Unity) — vị trí spawn trên map.</summary>
    public float cy;

    /// <summary>
    /// True = kích hoạt chế độ Boss:
    /// - Dùng BossAI thay vì EnemyAI thường
    /// - Hiển thị Boss Health Bar
    /// - Trigger nhạc boss khi vào vùng aggro
    /// </summary>
    public bool is_boss;

    /// <summary>Số lượng enemy spawn tại điểm này. Mặc định 1 nếu thiếu.</summary>
    public int count = 1;

    /// <summary>Giây chờ trước khi enemy hồi sinh tại vị trí này. Mặc định 30.</summary>
    public int respawn_time = 30;

    /// <summary>Level hiển thị của enemy tại điểm spawn này.</summary>
    public int level = 1;

    /// <summary>HP ghi đè từ map_spawn_config (legacy). 0 = dùng base_hp trong enemy_skills.</summary>
    public int override_hp;

    /// <summary>EXP ghi đè từ map_spawn_config (legacy). 0 = dùng exp_reward trong enemy_skills.</summary>
    public int override_exp;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Drop item entry — 1 item trong danh sách drop
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class DropItemEntry
{
    /// <summary>ID item trong ItemManager / item_template table.</summary>
    public int item_id;

    /// <summary>
    /// Tỉ lệ rơi theo hệ 0.0–1.0.
    /// Ví dụ: 0.25 = 25%, 1.0 = 100%.
    /// </summary>
    public float rate;

    /// <summary>Số lượng tối thiểu mỗi lần rơi. Phải ≥ 1.</summary>
    public int qty_min = 1;

    /// <summary>Số lượng tối đa mỗi lần rơi. Phải ≥ qty_min.</summary>
    public int qty_max = 1;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Enemy skills entry — thông tin đầy đủ của 1 loại quái
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class EnemySkillsEntry
{
    /// <summary>ID loại quái — match với SpawnEntry.enemy_id.</summary>
    public int enemy_id;

    /// <summary>Tên quái được load từ DB (dùng để hiển thị trong EnemyInfoPanel).</summary>
    public string enemy_name = "";

    /// <summary>HP tối đa cơ bản — dùng để InitHealth ngay sau khi spawn.</summary>
    public int base_hp;

    /// <summary>Sát thương cơ bản — tính damage_flat khi chỉ có damage_multiplier.</summary>
    public int base_damage;

    /// <summary>Nguyên tố của quái (Fire/Water/Earth/Metal/Wood/Wind/None).</summary>
    public string element_type = "None";

    /// <summary>EXP thưởng khi giết (từ reward_json.exp).</summary>
    public int exp_reward;

    /// <summary>Vàng thưởng khi giết (từ reward_json.gold).</summary>
    public int gold_reward;

    /// <summary>Bạc thưởng khi giết (từ reward_json.silver).</summary>
    public int silver_reward;

    /// <summary>Danh sách drop item (từ reward_json.drops, đã chuẩn hóa rate 0–1).</summary>
    public DropItemEntry[] drops;

    /// <summary>Danh sách skill của quái này.</summary>
    public SkillEntry[] skills;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Skill entry — 1 skill của enemy
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class SkillEntry
{
    /// <summary>
    /// ID nội bộ không có dấu cách, dùng để tra cooldown và animation trigger.
    /// Ví dụ: "FIRE_BREATH", "WIND_SLASH", "VINE_SNARE"
    /// </summary>
    public string skill_id = "";

    /// <summary>Nguyên tố của skill — có thể khác element_type của quái.</summary>
    public string element = "None";

    /// <summary>Giây hồi chiêu (cooldown). Server-side enforced.</summary>
    public float cooldown_sec = 5f;

    /// <summary>Tầm đánh tối đa (Unity units). Quái phải trong range mới cast được.</summary>
    public float range = 5f;

    /// <summary>True = tấn công diện (AoE xung quanh enemy). False = bắn thẳng đến target.</summary>
    public bool aoe = false;

    /// <summary>Bán kính AoE (Unity units). Chỉ dùng khi aoe = true.</summary>
    public float aoe_radius = 3f;

    /// <summary>
    /// Key của projectile prefab trong EnemyAI.projectilePrefabs.
    /// Rỗng = skill gây damage trực tiếp / melee / AoE, không spawn đạn.
    /// </summary>
    public string projectile_prefab_key = "";

    /// <summary>Tốc độ bay của projectile (units/second).</summary>
    public float projectile_speed = 8f;

    /// <summary>Thời gian tồn tại tối đa của projectile trước khi tự hủy.</summary>
    public float projectile_lifetime = 3f;

    /// <summary>Offset spawn theo trục X, luôn tính về phía trước mặt enemy.</summary>
    public float projectile_spawn_offset_x = 0.6f;

    /// <summary>Offset spawn theo trục Y so với vị trí bắn.</summary>
    public float projectile_spawn_offset_y = 0.25f;

    /// <summary>Tên Animator trigger để play animation khi cast. Rỗng = không có animation riêng.</summary>
    public string animation_trigger = "";

    /// <summary>
    /// Hiệu ứng trạng thái gây ra cho player khi trúng skill.
    /// Giá trị: "burn", "freeze", "paralyze", "slow", "poison", "" (không có)
    /// </summary>
    public string status_effect = "";

    /// <summary>Thời gian (giây) hiệu ứng trạng thái kéo dài.</summary>
    public float duration_sec = 0f;

    /// <summary>
    /// Nếu skill là SUMMON_ADD: ID enemy cần triệu hồi thêm.
    /// 0 = không triệu hồi.
    /// </summary>
    public int spawn_enemy_id = 0;

    /// <summary>Số lượng enemy triệu hồi (dùng với SUMMON_ADD).</summary>
    public int spawn_count = 0;
}
