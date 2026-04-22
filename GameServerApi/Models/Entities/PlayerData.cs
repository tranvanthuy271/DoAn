using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApi.Models
{
    // ----------------------------------------------------------------
    // ActiveBuff : 1 buff đang active, lưu trong player_data.active_buffs
    // ----------------------------------------------------------------
    public class ActiveBuff
    {
        [JsonPropertyName("effectType")]  public string EffectType  { get; set; } = string.Empty;
        [JsonPropertyName("value")]       public int    Value       { get; set; }
        [JsonPropertyName("iconId")]      public int    IconId      { get; set; }
        [JsonPropertyName("name")]        public string Name        { get; set; } = string.Empty;
        [JsonPropertyName("detail")]      public string Detail      { get; set; } = string.Empty;
        /// <summary>UTC expiry thời điểm; null nếu instant (đã apply rồi).</summary>
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


    // ----------------------------------------------------------------
    // InfoChar : tất cả chỉ số & trạng thái nhân vật được pack vào 1 cột JSON.
    // Mapping với cột  player_data.info_char  (LONGTEXT).
    // ----------------------------------------------------------------
    public class InfoChar
    {
        // ---- Progression ----
        [JsonPropertyName("level")]            public int    Level            { get; set; } = 1;
        [JsonPropertyName("experience")]       public int    Experience       { get; set; } = 0;
        [JsonPropertyName("gold")]             public int    Gold             { get; set; } = 0;
        [JsonPropertyName("silver")]           public int    Silver           { get; set; } = 0;
        [JsonPropertyName("skill_points")]     public int    SkillPoints      { get; set; } = 0;
        [JsonPropertyName("potential_points")] public int    PotentialPoints  { get; set; } = 0;

        // ---- Element / Gene ----
        [JsonPropertyName("element_type")]        public string  ElementType       { get; set; } = "Fire";
        [JsonPropertyName("gene_tier")]           public int     GeneTier          { get; set; } = 1;
        [JsonPropertyName("gene_exp")]            public int     GeneExp           { get; set; } = 0;
        [JsonPropertyName("is_hybrid")]              public bool          IsHybrid             { get; set; } = false;
        [JsonPropertyName("secondary_element")]      public string?       SecondaryElement      { get; set; } = null;
        [JsonPropertyName("secondary_gene_tier")]    public int?          SecondaryGeneTier     { get; set; } = null;
        [JsonPropertyName("secondary_gene_exp")]     public int?          SecondaryGeneExp      { get; set; } = null;
        // ---- Hybrid Gene Fusion ----
        [JsonPropertyName("hybrid_element_a")]       public string?       HybridElementA        { get; set; } = null;
        [JsonPropertyName("hybrid_element_b")]       public string?       HybridElementB        { get; set; } = null;
        // CSV hệ bị sát thương tăng 50%, e.g. "Earth,Fire"
        [JsonPropertyName("hybrid_bonus_targets")]   public string?       HybridBonusTargets    { get; set; } = null;
        // CSV hệ không còn khắc được player, e.g. "Water,Metal"
        [JsonPropertyName("hybrid_immune_elements")] public string?       HybridImmuneElements  { get; set; } = null;
        [JsonPropertyName("hybrid_atk_bonus_pct")]   public float         HybridAtkBonusPct     { get; set; } = 0f;
        [JsonPropertyName("hybrid_id")]              public int?          HybridId              { get; set; } = null;
        [JsonPropertyName("hybrid_prefab_path")]     public string?       HybridPrefabPath      { get; set; } = null;

        // ---- HP / MP / Combat ----
        [JsonPropertyName("hp")]      public int Hp      { get; set; } = 100;
        [JsonPropertyName("max_hp")]  public int MaxHp   { get; set; } = 100;
        [JsonPropertyName("mp")]      public int Mp      { get; set; } = 50;
        [JsonPropertyName("max_mp")]  public int MaxMp   { get; set; } = 50;
        [JsonPropertyName("attack")]  public int Attack  { get; set; } = 10;
        [JsonPropertyName("defense")] public int Defense { get; set; } = 0;

        // ---- Bag ----
        [JsonPropertyName("bag_slots")]          public int BagSlots { get; set; } = 20;
        [JsonPropertyName("bag_equipped_items")] public List<BagEquippedItemInfo> BagEquippedItems { get; set; } = new();

        // ---- Position ----
        [JsonPropertyName("map_id")]     public int   MapId     { get; set; } = 0;
        [JsonPropertyName("zone_id")]    public int   ZoneId    { get; set; } = 0;
        [JsonPropertyName("position_x")] public float PositionX { get; set; } = 0f;
        [JsonPropertyName("position_y")] public float PositionY { get; set; } = 0f;

        // ---- Wave Dungeon Daily Tracking ----
        /// <summary>
        /// Số lần đã tham gia phó bản wave trong ngày (UTC).
        /// Reset tự động khi daily_wave_date khác ngày hôm nay.
        /// Managed in-memory by WaveSessionManager; persisted here for reference only.
        /// </summary>
        [JsonPropertyName("daily_wave_entries")] public int    DailyWaveEntries { get; set; } = 0;
        /// <summary>Ngày (UTC) ghi nhận daily_wave_entries, định dạng "yyyy-MM-dd".</summary>
        [JsonPropertyName("daily_wave_date")]    public string DailyWaveDate    { get; set; } = "";
    }

    // ----------------------------------------------------------------
    // PlayerData : ORM model cho bảng player_data.
    // Các chỉ số nhân vật có thể thay đổi được lưu trong InfoCharJson.
    // ----------------------------------------------------------------
    public class PlayerData
    {
        public int    PlayerId      { get; set; }   // PK, FK -> users.user_id
        public string CharacterName { get; set; } = "";
        public string Gender        { get; set; } = "Male";

        // ---- JSON columns ----
        /// <summary>Serialised InfoChar object (cot info_char).</summary>
        public string InfoCharJson      { get; set; } = "{}";
        public string EquipmentJson     { get; set; } = "{}";
        public string InventoryJson     { get; set; } = "[]";
        public string SkillsJson        { get; set; } = "[]";
        public string PotentialStatsJson{ get; set; } = "{}";

        /// <summary>JSON array of active timed buffs (ActiveBuff[]).</summary>
        public string ActiveBuffsJson   { get; set; } = "[]";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Helpers ----
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Deserialise info_char column → InfoChar object.</summary>
        public InfoChar GetInfoChar()
        {
            if (string.IsNullOrWhiteSpace(InfoCharJson) || InfoCharJson == "{}")
                return new InfoChar();
            try   { return JsonSerializer.Deserialize<InfoChar>(InfoCharJson, _opts) ?? new InfoChar(); }
            catch { return new InfoChar(); }
        }

        /// <summary>Serialise InfoChar object → info_char column.</summary>
        public void SetInfoChar(InfoChar ic)
        {
            InfoCharJson = JsonSerializer.Serialize(ic);
        }

        // ---- ActiveBuffs helpers ----
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

        /// <summary>Build a default InfoChar for a brand-new player.</summary>
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
