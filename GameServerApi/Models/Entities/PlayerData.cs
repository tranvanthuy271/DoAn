using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApi.Models
{
    // ActiveBuff : 1 buff đang active, lưu trong player_data.active_buffs
    public class ActiveBuff
    {
        [JsonPropertyName("effectType")]  public string EffectType  { get; set; } = string.Empty;
        [JsonPropertyName("value")]       public int    Value       { get; set; }
        [JsonPropertyName("iconId")]      public int    IconId      { get; set; }
        [JsonPropertyName("name")]        public string Name        { get; set; } = string.Empty;
        [JsonPropertyName("detail")]      public string Detail      { get; set; } = string.Empty;
        // UTC expiry thời điểm; null nếu instant (đã apply rồi).
        [JsonPropertyName("expireAt")]    public DateTime? ExpireAt { get; set; }
    }

    public class BagEquippedItemInfo
    {
        [JsonPropertyName("quick_slot_index")] public int QuickSlotIndex { get; set; }
        [JsonPropertyName("item_template_id")] public int ItemTemplateId { get; set; }
        [JsonPropertyName("item_code")]        public string ItemCode { get; set; } = string.Empty;
        [JsonPropertyName("item_name")]        public string ItemName { get; set; } = string.Empty;
        [JsonPropertyName("icon_id")]          public int IconId { get; set; }
        [JsonPropertyName("upgrade_level")]    public int UpgradeLevel { get; set; }
        [JsonPropertyName("str_options")]      public string StrOptions { get; set; } = string.Empty;
        [JsonPropertyName("slot_bonus")]       public int SlotBonus { get; set; } = 5;
        [JsonPropertyName("is_locked")]        public bool IsLocked { get; set; }
    }


    // InfoChar : tất cả chỉ số & trạng thái nhân vật được pack vào 1 cột JSON.
    // Mapping với cột  player_data.info_char  (LONGTEXT).
    public class InfoChar
    {
        // Chỉ số tiến trình nhân vật như level, kinh nghiệm và tiền tệ.
        [JsonPropertyName("level")]            public int    Level            { get; set; } = 1;
        [JsonPropertyName("experience")]       public int    Experience       { get; set; } = 0;
        [JsonPropertyName("gold")]             public int    Gold             { get; set; } = 0;
        [JsonPropertyName("silver")]           public int    Silver           { get; set; } = 0;
        [JsonPropertyName("skill_points")]     public int    SkillPoints      { get; set; } = 0;
        [JsonPropertyName("potential_points")] public int    PotentialPoints  { get; set; } = 0;

        // Thông tin hệ nguyên tố và gene hiện tại của nhân vật.
        [JsonPropertyName("element_type")]        public string  ElementType       { get; set; } = "Fire";
        [JsonPropertyName("gene_tier")]           public int     GeneTier          { get; set; } = 1;
        [JsonPropertyName("gene_exp")]            public int     GeneExp           { get; set; } = 0;
        [JsonPropertyName("is_hybrid")]              public bool          IsHybrid             { get; set; } = false;
        [JsonPropertyName("secondary_element")]      public string?       SecondaryElement      { get; set; } = null;
        [JsonPropertyName("secondary_gene_tier")]    public int?          SecondaryGeneTier     { get; set; } = null;
        [JsonPropertyName("secondary_gene_exp")]     public int?          SecondaryGeneExp      { get; set; } = null;
        // Dữ liệu dung hợp gene hybrid và các hiệu ứng đi kèm.
        [JsonPropertyName("hybrid_element_a")]       public string?       HybridElementA        { get; set; } = null;
        [JsonPropertyName("hybrid_element_b")]       public string?       HybridElementB        { get; set; } = null;
        // CSV hệ bị sát thương tăng 50%, e.g. "Earth,Fire"
        [JsonPropertyName("hybrid_bonus_targets")]   public string?       HybridBonusTargets    { get; set; } = null;
        // CSV hệ không còn khắc được player, e.g. "Water,Metal"
        [JsonPropertyName("hybrid_immune_elements")] public string?       HybridImmuneElements  { get; set; } = null;
        [JsonPropertyName("hybrid_atk_bonus_pct")]   public float         HybridAtkBonusPct     { get; set; } = 0f;
        [JsonPropertyName("hybrid_id")]              public int?          HybridId              { get; set; } = null;
        [JsonPropertyName("hybrid_prefab_path")]     public string?       HybridPrefabPath      { get; set; } = null;

        // Gene Tối Thượng (Ultimate Gene)
        // Kích hoạt sau khi đã Dung hợp Hybrid. Khi tích đủ ultimate_gene_exp, server bật
        // is_ultimate = true → toàn bộ final_stats được nhân hệ số (mặc định x1.5) và spawn aura sau lưng.
        [JsonPropertyName("is_ultimate")]            public bool          IsUltimate            { get; set; } = false;
        [JsonPropertyName("ultimate_gene_exp")]      public int           UltimateGeneExp       { get; set; } = 0;
        // Resources path cho prefab aura (ví dụ "Prefabs/Player/Aura/UltimateAura"), lấy từ gene_ultimate_config.
        [JsonPropertyName("ultimate_aura_path")]     public string?       UltimateAuraPath      { get; set; } = null;

        // Chỉ số máu, năng lượng và chiến đấu cơ bản.
        [JsonPropertyName("hp")]      public int Hp      { get; set; } = 100;
        [JsonPropertyName("max_hp")]  public int MaxHp   { get; set; } = 100;
        [JsonPropertyName("mp")]      public int Mp      { get; set; } = 50;
        [JsonPropertyName("max_mp")]  public int MaxMp   { get; set; } = 50;
        [JsonPropertyName("attack")]  public int Attack  { get; set; } = 10;
        [JsonPropertyName("defense")] public int Defense { get; set; } = 0;

        // Bag
        [JsonPropertyName("bag_slots")]          public int BagSlots { get; set; } = 20;
        [JsonPropertyName("bag_equipped_items")] public List<BagEquippedItemInfo> BagEquippedItems { get; set; } = new();

        // Position
        [JsonPropertyName("map_id")]     public int   MapId     { get; set; } = 0;
        [JsonPropertyName("zone_id")]    public int   ZoneId    { get; set; } = 0;
        [JsonPropertyName("position_x")] public float PositionX { get; set; } = 0f;
        [JsonPropertyName("position_y")] public float PositionY { get; set; } = 0f;

        // Wave Dungeon Daily Tracking
        // Số lần đã tham gia phó bản wave trong ngày (UTC).
        // Reset tự động khi daily_wave_date khác ngày hôm nay.
        // Managed in-memory by WaveSessionManager; persisted here for reference only.
        [JsonPropertyName("daily_wave_entries")] public int    DailyWaveEntries { get; set; } = 0;
        // Ngày (UTC) ghi nhận daily_wave_entries, định dạng "yyyy-MM-dd".
        [JsonPropertyName("daily_wave_date")]    public string DailyWaveDate    { get; set; } = "";

        // Level lock
        // Khoá cấp nhân vật — khi true, nhân vật không lên cấp kể cả khi đủ kinh nghiệm.
        // Được bật/tắt qua chức năng "Khoá cấp nhân vật" tại NPC.
        [JsonPropertyName("is_level_locked")]    public bool   IsLevelLocked    { get; set; } = false;

        // Leaderboard tracking
        // Số ngày đăng nhập (chúyên cần), tăng 1 lần/ngày khi login.
        [JsonPropertyName("attendance_count")]      public int    AttendanceCount      { get; set; } = 0;
        // Ngày điểm danh gần nhất ("yyyy-MM-dd" UTC), tránh đếm 2 lần trong ngày.
        [JsonPropertyName("last_attendance_date")] public string LastAttendanceDate   { get; set; } = "";
        // Số nhiệm vụ đã hoàn thành.
        [JsonPropertyName("quest_completed_count")] public int   QuestCompletedCount  { get; set; } = 0;
        // Kỷ lục phó bản: key=dungeonId, value=wave cao nhất đạt được.
        [JsonPropertyName("dungeon_best_waves")]    public Dictionary<int, int> DungeonBestWaves { get; set; } = new();

        // Quest progress (lưu tại đây, không có bảng player_quest)
        // ID quest đang làm (-1 = không có quest active).
        [JsonPropertyName("active_quest_id")]    public int ActiveQuestId    { get; set; } = -1;
        // Bước hiện tại (step index) của quest đang làm.
        [JsonPropertyName("quest_step")]         public int QuestStep        { get; set; } = 0;
        // Tiến trình từng bước: key=stepIndex, value=số đã thực hiện.
        [JsonPropertyName("quest_progress")]     public Dictionary<string, int> QuestProgress   { get; set; } = new();
        // Danh sách id quest đã hoàn thành.
        [JsonPropertyName("completed_quests")]   public List<int>               CompletedQuests { get; set; } = new();
    }

    // PlayerData : ORM model cho bảng player_data.
    // Các chỉ số nhân vật có thể thay đổi được lưu trong InfoCharJson.
    public interface IPlayerDataRecord
    {
        int PlayerId { get; set; }
        string CharacterName { get; set; }
        string Gender { get; set; }
        string InfoCharJson { get; set; }
        string EquipmentJson { get; set; }
        string InventoryJson { get; set; }
        string SkillsJson { get; set; }
        string PotentialStatsJson { get; set; }
        string ActiveBuffsJson { get; set; }
        DateTime UpdatedAt { get; set; }
        InfoChar GetInfoChar();
        void SetInfoChar(InfoChar ic);
        List<ActiveBuff> GetActiveBuffs();
        void SetActiveBuffs(List<ActiveBuff> buffs);
    }

    public class PlayerData : IPlayerDataRecord
    {
        public int    PlayerId      { get; set; }   // PK, FK -> users.user_id
        public string CharacterName { get; set; } = "";
        public string Gender        { get; set; } = "Male";

        // JSON columns
        // Serialised InfoChar object (cot info_char).
        public string InfoCharJson      { get; set; } = "{}";
        public string EquipmentJson     { get; set; } = "{}";
        public string InventoryJson     { get; set; } = "[]";
        public string SkillsJson        { get; set; } = "[]";
        public string PotentialStatsJson{ get; set; } = "{}";

        // JSON array of active timed buffs (ActiveBuff[]).
        public string ActiveBuffsJson   { get; set; } = "[]";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Deserialise info_char column → InfoChar object.
        public InfoChar GetInfoChar()
        {
            if (string.IsNullOrWhiteSpace(InfoCharJson) || InfoCharJson == "{}")
                return new InfoChar();
            try   { return JsonSerializer.Deserialize<InfoChar>(InfoCharJson, _opts) ?? new InfoChar(); }
            catch { return new InfoChar(); }
        }

        // Serialise InfoChar object → info_char column.
        public void SetInfoChar(InfoChar ic)
        {
            InfoCharJson = JsonSerializer.Serialize(ic);
        }

        // ActiveBuffs helpers
        public List<ActiveBuff> GetActiveBuffs()
        {
            if (string.IsNullOrWhiteSpace(ActiveBuffsJson) || ActiveBuffsJson == "[]")
                return new List<ActiveBuff>();
            try { return JsonSerializer.Deserialize<List<ActiveBuff>>(ActiveBuffsJson, _opts) ?? new List<ActiveBuff>(); }
            catch { return new List<ActiveBuff>(); }
        }

        public void SetActiveBuffs(List<ActiveBuff> buffs)
        {
            // Loại bỏ buff đã hết hạn trước khi lưu
            buffs.RemoveAll(b => b.ExpireAt.HasValue && b.ExpireAt.Value <= DateTime.UtcNow);
            ActiveBuffsJson = JsonSerializer.Serialize(buffs);
        }

        // Build a default InfoChar for a brand-new player.
        public static InfoChar DefaultInfoChar(string elementType) => new InfoChar
        {
            Level = 1, Experience = 0, Gold = 0, Silver = 0,
            SkillPoints = 0, PotentialPoints = 5,
            ElementType = elementType,
            GeneTier = 1, GeneExp = 0,
            IsHybrid = false,
            SecondaryElement = null, SecondaryGeneTier = null, SecondaryGeneExp = null,
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
