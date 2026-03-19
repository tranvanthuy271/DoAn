using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Bang enemy - Thong tin day du quai vat va boss
    /// (khang nguyen to, tang sat thuong, ky nang boss, giai doan boss)
    /// Pattern tu LangLa: Mob.java + BossTpl.java
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
        public int SilverReward { get; set; } = 20;
        public string? DropItemsJson { get; set; }
        public string? ElementType { get; set; }
        public string? EnemyType { get; set; }

        // Khang nguyen to (0-100%) - tu LangLa: khangHoa, khangThuy...
        public int KhangHoa { get; set; } = 0;
        public int KhangThuy { get; set; } = 0;
        public int KhangTho { get; set; } = 0;
        public int KhangMoc { get; set; } = 0;
        public int KhangKim { get; set; } = 0;
        public int KhangPhong { get; set; } = 0;

        // Tang sat thuong khi tan cong nhan vat nguyen to tuong ung (%)
        // Tu LangLa: tangDameLenHoa, tangDameLenThuy...
        public int TangDameHoa { get; set; } = 0;
        public int TangDameThuy { get; set; } = 0;
        public int TangDameTho { get; set; } = 0;
        public int TangDameMoc { get; set; } = 0;
        public int TangDameKim { get; set; } = 0;
        public int TangDamePhong { get; set; } = 0;

        // Chi so dac biet
        public int HpRegenPerSec { get; set; } = 0;  // LangLa: HoiHp
        public int EvasionRate { get; set; } = 0;     // LangLa: NeTranh (0-100%)
        public int CounterRate { get; set; } = 0;     // LangLa: PhanDon (0-100%)

        // Boss only
        /// <summary>JSON ky nang boss: [{"skill_id":"FIRE_BREATH","damage_multiplier":2.5,"cooldown_sec":8,"aoe":false}]</summary>
        public string? SkillsJson { get; set; }

        /// <summary>JSON giai doan boss: [{"hp_pct_threshold":50,"action":"summon","mob_id":6,"mob_count":2}]</summary>
        public string? PhasesJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
