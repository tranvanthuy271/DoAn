using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Model cho bảng skill_template trong database.
    /// Chứa master-data của tất cả skills trong game.
    /// 
    /// levels_json: JSON array, mỗi phần tử là yêu cầu để lên level i+1:
    ///   [{"level_req":1,"sp_cost":1,"effect_value":1.2,"mp_cost":10,"desc":"..."},...]
    /// </summary>
    public class SkillTemplate
    {
        public int    SkillId       { get; set; }
        public string SkillCode     { get; set; } = "";
        public string SkillName     { get; set; } = "";
        public string? Description  { get; set; }
        /// <summary>NULL = Universal; "Fire" | "Water" | "Earth" | "Wood" | "Metal"</summary>
        public string? ElementType  { get; set; }
        public int    MaxLevel      { get; set; } = 5;
        /// <summary>Player level required to learn the skill (unlock level 1)</summary>
        public int    LevelToUnlock { get; set; } = 1;
        /// <summary>Gene tier required to unlock this skill (0 = no gene requirement)</summary>
        public int    GeneTierRequired { get; set; } = 0;
        /// <summary>JSON array [{level_req, sp_cost, effect_value, mp_cost, desc}]</summary>
        public string? LevelsJson   { get; set; }
        public string? IconId       { get; set; }
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    }
}
