namespace GameServerApi.Models.Entities
{
    // Bảng map_enemy_drop — Tỉ lệ drop item ghi đè theo từng map.
    // Khi enemy chết ở một map cụ thể, server sẽ dùng bảng này thay vì
    // drop_items_json mặc định trong bảng enemy.
    // Pattern từ LangLa: Map.DropConfig + BossDropConfig
    public class MapEnemyDrop
    {
        public int Id { get; set; }

        // Map áp dụng tỉ lệ
        public int MapId { get; set; }

        // FK → enemy.enemy_id
        public int EnemyId { get; set; }

        // FK → item_template.id
        public int ItemId { get; set; }

        // Tỉ lệ rơi (0.0 – 1.0). Ví dụ: 0.05 = 5%
        public float DropChance { get; set; } = 0.1f;

        public int QtyMin { get; set; } = 1;
        public int QtyMax { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
