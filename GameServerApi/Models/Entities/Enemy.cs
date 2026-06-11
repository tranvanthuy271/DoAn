using System;

namespace GameServerApi.Models
{
    // Bảng enemy — thông tin đầy đủ quái vật và boss.
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
        public int SilverReward { get; set; } = 20;
        public string? DropItemsJson { get; set; }
        public string? ElementType { get; set; }
        public string? EnemyType { get; set; }

        // JSON array skill của quái (áp dụng cả quái thường lẫn boss).
        public string? SkillsJson { get; set; }

        // Kháng nguyên tố (%)
        public int KhangHoa { get; set; } = 0;
        public int KhangThuy { get; set; } = 0;
        public int KhangTho { get; set; } = 0;
        public int KhangMoc { get; set; } = 0;
        public int KhangKim { get; set; } = 0;
        public int KhangPhong { get; set; } = 0;

        // Tăng sát thương nguyên tố (%)
        public int TangDameHoa { get; set; } = 0;
        public int TangDameThuy { get; set; } = 0;
        public int TangDameTho { get; set; } = 0;
        public int TangDameMoc { get; set; } = 0;
        public int TangDameKim { get; set; } = 0;
        public int TangDamePhong { get; set; } = 0;

        // Chỉ số phụ
        public int HpRegenPerSec { get; set; } = 0;
        public int EvasionRate { get; set; } = 0;
        public int CounterRate { get; set; } = 0;

        // JSON giai đoạn boss: [{"hp_pct_threshold":50,"action":"enrage",...}]
        public string? PhasesJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
