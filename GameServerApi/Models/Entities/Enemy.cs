using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Bảng enemy - Lưu thông tin chi tiết của enemy (name, mô tả, chỉ số: hp, damage, exp reward, gold reward, level, etc.)
    /// </summary>
    public class Enemy
    {
        public int EnemyId { get; set; }
        public string EnemyName { get; set; } = "";
        public string? EnemyDescription { get; set; }
        public int Level { get; set; } = 1;
        public int BaseHp { get; set; } = 50;
        public int BaseMp { get; set; } = 0;
        public int BaseDamage { get; set; } = 5;
        public int BaseDefense { get; set; } = 0;
        public float MoveSpeed { get; set; } = 2.0f;
        public float AttackSpeed { get; set; } = 1.0f;
        public int ExpReward { get; set; } = 10;
        public int GoldReward { get; set; } = 5;
        public string? DropItemsJson { get; set; } // JSON: [{"item_id":1,"drop_rate":0.1}, ...]
        public string? ElementType { get; set; } // Fire, Water, Earth, Wood, Metal, None
        public string? EnemyType { get; set; } // Normal, Elite, Boss
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
