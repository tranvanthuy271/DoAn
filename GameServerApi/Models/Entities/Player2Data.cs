using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    /// <summary>
    /// Player2Data: ORM model cho bảng player2_data.
    /// Lưu toàn bộ dữ liệu hệ gene thứ 2: skill, tiềm năng, kinh nghiệm, trang bị, inventory.
    /// Chỉ được tạo khi player_data.info_char.secondary_element != null.
    /// </summary>
    [Table("player2_data")]
    public class Player2Data
    {
        public int    PlayerId      { get; set; }   // PK + FK → player_data.player_id
        public string CharacterName { get; set; } = "";
        public string Gender        { get; set; } = "Male";

        // ---- JSON columns ----
        public string InfoCharJson       { get; set; } = "{}";
        public string EquipmentJson      { get; set; } = "{}";
        public string InventoryJson      { get; set; } = "[]";
        public string SkillsJson         { get; set; } = "[]";
        public string PotentialStatsJson { get; set; } = "{}";
        public string ActiveBuffsJson    { get; set; } = "[]";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Helpers ----
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public InfoChar GetInfoChar()
        {
            if (string.IsNullOrWhiteSpace(InfoCharJson) || InfoCharJson == "{}")
                return new InfoChar();
            try   { return JsonSerializer.Deserialize<InfoChar>(InfoCharJson, _opts) ?? new InfoChar(); }
            catch { return new InfoChar(); }
        }

        public void SetInfoChar(InfoChar ic)
        {
            InfoCharJson = JsonSerializer.Serialize(ic);
        }

        public List<ActiveBuff> GetActiveBuffs()
        {
            if (string.IsNullOrWhiteSpace(ActiveBuffsJson) || ActiveBuffsJson == "[]")
                return new List<ActiveBuff>();
            try { return JsonSerializer.Deserialize<List<ActiveBuff>>(ActiveBuffsJson, _opts) ?? new List<ActiveBuff>(); }
            catch { return new List<ActiveBuff>(); }
        }

        public void SetActiveBuffs(List<ActiveBuff> buffs)
        {
            buffs.RemoveAll(b => b.ExpireAt.HasValue && b.ExpireAt.Value <= DateTime.UtcNow);
            ActiveBuffsJson = JsonSerializer.Serialize(buffs);
        }

        /// <summary>Build InfoChar mặc định cho nhân vật hệ gene 2 mới tạo.</summary>
        public static InfoChar DefaultInfoChar(string elementType, string primaryElement) => new InfoChar
        {
            Level = 1, Experience = 0, Gold = 0, Silver = 0,
            SkillPoints = 0, PotentialPoints = 5,
            ElementType = elementType,
            GeneTier = 1, GeneExp = 0,
            IsHybrid = false,
            SecondaryElement = primaryElement,  // hệ phụ của gene2 chính là hệ chính của gene1
            SecondaryGeneTier = null, SecondaryGeneExp = null,
            HybridElementA = null, HybridElementB = null,
            HybridBonusTargets = null, HybridImmuneElements = null, HybridAtkBonusPct = 0f,
            BagSlots = 20,
            Hp = 100, MaxHp = 100,
            Mp = 50,  MaxMp = 50,
            Attack = 10, Defense = 0,
            MapId = 0, PositionX = 0f, PositionY = 0f
        };
    }
}
