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
    /// Quy tắc drop item theo enemy_id — mỗi phần tử = 1 loại quái.
    /// Tách riêng khỏi spawns để tránh lặp dữ liệu.
    /// </summary>
    public DropEntry[] drops;

    /// <summary>
    /// Skills của từng loại quái — mỗi phần tử = 1 enemy_id với danh sách skill.
    /// Dữ liệu đến từ cột skills_json trong bảng enemy.
    /// Host dùng để set EnemySkillSet ngay sau khi spawn enemy.
    /// </summary>
    public EnemySkillsEntry[] enemy_skills;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Spawn entry — 1 điểm spawn
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class SpawnEntry
{
    /// <summary>ID loại quái — map sang EnemyPrefabManager.GetEnemyPrefab(enemy_id).</summary>
    public int enemy_id;

    /// <summary>
    /// HP ghi đè (overwrite) của quái tại vị trí này.
    /// = 0 → HostSpawnConfigLoader tự fallback về base_hp trong prefab/enemy table.
    /// </summary>
    public int hp;

    /// <summary>EXP thưởng khi giết quái này. = 0 → fallback về exp_reward mặc định.</summary>
    public int exp;

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

    /// <summary>Level của enemy. Hiển thị trong EnemyInfoPanel. Mặc định 1.</summary>
    public int level = 1;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Drop entry — tỉ lệ rơi cho 1 loại quái
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class DropEntry
{
    /// <summary>ID loại quái — match với SpawnEntry.enemy_id.</summary>
    public int enemy_id;

    /// <summary>Danh sách item có thể rơi khi quái này chết.</summary>
    public DropItemEntry[] items;
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
    /// HostSpawnConfigLoader clamp về [0, 1] nếu ngoài range.
    /// </summary>
    public float rate;

    /// <summary>Số lượng tối thiểu mỗi lần rơi. Phải ≥ 1.</summary>
    public int qty_min = 1;

    /// <summary>Số lượng tối đa mỗi lần rơi. Phải ≥ qty_min.</summary>
    public int qty_max = 1;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Enemy skills entry — skill set của 1 loại quái
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class EnemySkillsEntry
{
    /// <summary>ID loại quái — match với SpawnEntry.enemy_id.</summary>
    public int enemy_id;

    /// <summary>Tên quái được load từ DB (dùng để hiển thị trong EnemyInfoPanel).</summary>
    public string enemy_name = "";

    /// <summary>Sát thương cơ bản của quái — dùng để tính damage_flat khi chỉ có damage_multiplier.</summary>
    public int base_damage;

    /// <summary>Nguyên tố của quái (Fire/Water/Earth/Metal/Wood/Wind/None).</summary>
    public string element_type = "None";

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

    /// <summary>
    /// Sát thương tuyệt đối của skill này (đơn vị điểm HP).
    /// Nếu > 0: dùng trực tiếp giá trị này.
    /// Nếu = 0: tính từ base_damage × damage_multiplier.
    /// </summary>
    public int flat_damage = 0;

    /// <summary>
    /// Hệ số nhân lên base_damage của enemy. Chỉ dùng khi flat_damage = 0.
    /// Ví dụ: 2.5 = gây 2.5× base_damage.
    /// </summary>
    public float damage_multiplier = 1.0f;

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
