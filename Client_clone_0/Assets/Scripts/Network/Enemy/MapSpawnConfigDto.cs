using System;

// MapSpawnConfigDto — Data Transfer Objects cho endpoint GET /api/map/{mapId}/spawn-config.
// Dùng JsonUtility.FromJson&lt;MapSpawnConfigResponse&gt;(json) để deserialize.
// Tất cả class phải [Serializable] để JsonUtility hoạt động.
//  Root response

[Serializable]
public class MapSpawnConfigResponse
{
    // Map ID trả về từ server (dùng để verify đúng map).
    public int map_id;

    // Danh sách vị trí spawn — mỗi phần tử = 1 điểm spawn trên map.
    // Cùng enemy_id có thể xuất hiện nhiều lần (các vị trí khác nhau).
    public SpawnEntry[] spawns;

    // Skills + reward của từng loại quái — mỗi phần tử = 1 enemy_id.
    // Dữ liệu đến từ cột skills_json và reward_json trong bảng enemy.
    // Host dùng để set EnemySkillSet, EnemyItemDrop và HP ngay sau khi spawn.
    public EnemySkillsEntry[] enemy_skills;
}

//  Spawn entry — 1 điểm spawn (chỉ chứa vị trí + tham số spawn)

[Serializable]
public class SpawnEntry
{
    // ID loại quái — map sang EnemyPrefabManager.GetEnemyPrefab(enemy_id).
    public int enemy_id;

    // Tọa độ X (world space Unity) — vị trí spawn trên map.
    public float cx;

    // Tọa độ Y (world space Unity) — vị trí spawn trên map.
    public float cy;

    // True = kích hoạt chế độ Boss:
    // - Dùng BossAI thay vì EnemyAI thường
    // - Hiển thị Boss Health Bar
    // - Trigger nhạc boss khi vào vùng aggro
    public bool is_boss;

    // Số lượng enemy spawn tại điểm này. Mặc định 1 nếu thiếu.
    public int count = 1;

    // Giây chờ trước khi enemy hồi sinh tại vị trí này. Mặc định 30.
    public int respawn_time = 30;

    // Level hiển thị của enemy tại điểm spawn này.
    public int level = 1;

    // HP ghi đè từ map_spawn_config (legacy). 0 = dùng base_hp trong enemy_skills.
    public int override_hp;

    // EXP ghi đè từ map_spawn_config (legacy). 0 = dùng exp_reward trong enemy_skills.
    public int override_exp;
}

//  Drop item entry — 1 item trong danh sách drop

[Serializable]
public class DropItemEntry
{
    // ID item trong ItemManager / item_template table.
    public int item_id;

    // Tỉ lệ rơi theo hệ 0.0–1.0.
    // Ví dụ: 0.25 = 25%, 1.0 = 100%.
    public float rate;

    // Số lượng tối thiểu mỗi lần rơi. Phải ≥ 1.
    public int qty_min = 1;

    // Số lượng tối đa mỗi lần rơi. Phải ≥ qty_min.
    public int qty_max = 1;
}

//  Enemy skills entry — thông tin đầy đủ của 1 loại quái

[Serializable]
public class EnemySkillsEntry
{
    // ID loại quái — match với SpawnEntry.enemy_id.
    public int enemy_id;

    // Tên quái được load từ DB (dùng để hiển thị trong EnemyInfoPanel).
    public string enemy_name = "";

    // HP tối đa cơ bản — dùng để InitHealth ngay sau khi spawn.
    public int base_hp;

    // Sát thương cơ bản — tính damage_flat khi chỉ có damage_multiplier.
    public int base_damage;

    // Nguyên tố của quái (Fire/Water/Earth/Metal/Wood/Wind/None).
    public string element_type = "None";

    // EXP thưởng khi giết (từ reward_json.exp).
    public int exp_reward;

    // Vàng thưởng khi giết (từ reward_json.gold).
    public int gold_reward;

    // Bạc thưởng khi giết (từ reward_json.silver).
    public int silver_reward;

    // Danh sách drop item (từ reward_json.drops, đã chuẩn hóa rate 0–1).
    public DropItemEntry[] drops;

    // Danh sách skill của quái này.
    public SkillEntry[] skills;
}

//  Skill entry — 1 skill của enemy

[Serializable]
public class SkillEntry
{
    // ID nội bộ không có dấu cách, dùng để tra cooldown và animation trigger.
    // Ví dụ: "FIRE_BREATH", "WIND_SLASH", "VINE_SNARE"
    public string skill_id = "";

    // Nguyên tố của skill — có thể khác element_type của quái.
    public string element = "None";

    // Giây hồi chiêu (cooldown). Server-side enforced.
    public float cooldown_sec = 5f;

    // Tầm đánh tối đa (Unity units). Quái phải trong range mới cast được.
    public float range = 5f;

    // True = tấn công diện (AoE xung quanh enemy). False = bắn thẳng đến target.
    public bool aoe = false;

    // Bán kính AoE (Unity units). Chỉ dùng khi aoe = true.
    public float aoe_radius = 3f;

    // Key của projectile prefab trong EnemyAI.projectilePrefabs.
    // Rỗng = skill gây damage trực tiếp / melee / AoE, không spawn đạn.
    public string projectile_prefab_key = "";

    // Tốc độ bay của projectile (units/second).
    public float projectile_speed = 8f;

    // Thời gian tồn tại tối đa của projectile trước khi tự hủy.
    public float projectile_lifetime = 3f;

    // Offset spawn theo trục X, luôn tính về phía trước mặt enemy.
    public float projectile_spawn_offset_x = 0.6f;

    // Offset spawn theo trục Y so với vị trí bắn.
    public float projectile_spawn_offset_y = 0.25f;

    // Tên Animator trigger để play animation khi cast. Rỗng = không có animation riêng.
    public string animation_trigger = "";

    // Hiệu ứng trạng thái gây ra cho player khi trúng skill.
    // Giá trị: "burn", "freeze", "paralyze", "slow", "poison", "" (không có)
    public string status_effect = "";

    // Thời gian (giây) hiệu ứng trạng thái kéo dài.
    public float duration_sec = 0f;

    // Nếu skill là SUMMON_ADD: ID enemy cần triệu hồi thêm.
    // 0 = không triệu hồi.
    public int spawn_enemy_id = 0;

    // Số lượng enemy triệu hồi (dùng với SUMMON_ADD).
    public int spawn_count = 0;
}
