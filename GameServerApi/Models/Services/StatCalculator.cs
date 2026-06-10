using System;
using System.Text.Json;

namespace GameServerApi.Models.Services
{
    /// <summary>
    /// Kết quả sau khi tính toán final_stats
    /// </summary>
    public class FinalStats
    {
        public int   MaxHp     { get; set; }
        public int   Hp        { get; set; }
        public int   MaxMp     { get; set; }
        public int   Mp        { get; set; }
        public int   Attack    { get; set; }
        public int   Defense   { get; set; }
        public float MoveSpeed { get; set; }
    }

    /// <summary>
    /// Tính toán final_stats = base_stats (đã bao gồm gene bonus) + equipment bonus + potential bonus
    ///
    /// Công thức:
    ///   final.max_hp    = info.MaxHp    + equip_bonus.max_hp    + potential.max_hp
    ///   final.max_mp    = info.MaxMp    + equip_bonus.max_mp    + potential.max_mp
    ///   final.attack    = info.Attack   + equip_bonus.attack    + potential.attack
    ///   final.defense   = info.Defense  + equip_bonus.defense   + potential.defense
    ///   final.move_speed= 5.0           + equip_bonus.move_speed+ potential.move_speed
    ///   final.hp        = min(info.Hp,  final.max_hp)  (HP hiện tại không vượt max sau khi buff)
    ///
    /// Equipment strOptions format: "optId,value;optId,value"  (value đã tính sẵn theo upgrade level)
    ///   optId 1 → attack       optId 5 → attack (secondary)
    ///   optId 2 → defense      optId 6 → defense (secondary)
    ///   optId 3 → max_hp
    ///   optId 4 → move_speed   (đơn vị nguyên, ví dụ 5 = +5 tốc)
    ///
    /// PotentialStats JSON format: { "max_hp": int, "max_mp": int, "attack": int, "defense": int, "move_speed": float }
    /// </summary>
    public static class StatCalculator
    {
        /// <summary>Hệ số nhân mặc định cho Gene Tối Thượng khi không truyền giá trị từ config.</summary>
        public const float DefaultUltimateMultiplier = 1.5f;

        public static FinalStats Compute(InfoChar baseInfo, string equipmentJson, string potentialStatsJson,
            float ultimateMultiplier = DefaultUltimateMultiplier)
        {
            var (eqHp, eqMp, eqAtk, eqDef, eqSpd) = ParseEquipBonus(equipmentJson);
            var (ptHp, ptMp, ptAtk, ptDef, ptSpd)  = ParsePotentialBonus(potentialStatsJson);

            int maxHp  = baseInfo.MaxHp  + eqHp  + ptHp;
            int maxMp  = baseInfo.MaxMp             + ptMp; // equipment thường không có MP bonus
            int attack = baseInfo.Attack + eqAtk + ptAtk;
            int def    = baseInfo.Defense + eqDef + ptDef;
            float spd  = 5f              + eqSpd + ptSpd;

            // ── Gene Tối Thượng: nhân toàn bộ final_stats (HP/MP/ATK/DEF) một lần ──
            if (baseInfo.IsUltimate && ultimateMultiplier > 0f)
            {
                maxHp  = (int)MathF.Round(maxHp  * ultimateMultiplier);
                maxMp  = (int)MathF.Round(maxMp  * ultimateMultiplier);
                attack = (int)MathF.Round(attack * ultimateMultiplier);
                def    = (int)MathF.Round(def    * ultimateMultiplier);
            }

            return new FinalStats
            {
                MaxHp     = maxHp,
                Hp        = Math.Min(baseInfo.Hp, maxHp),
                MaxMp     = maxMp,
                Mp        = Math.Min(baseInfo.Mp, maxMp),
                Attack    = attack,
                Defense   = def,
                MoveSpeed = MathF.Round(spd, 2),
            };
        }

        // ─── Equipment bonus ────────────────────────────────────────
        private static (int hp, int mp, int atk, int def, float spd)
            ParseEquipBonus(string equipmentJson)
        {
            int hp = 0, mp = 0, atk = 0, def = 0; float spd = 0f;
            if (string.IsNullOrWhiteSpace(equipmentJson) || equipmentJson == "{}") return (hp, mp, atk, def, spd);

            try
            {
                using var doc = JsonDocument.Parse(equipmentJson);
                foreach (var slot in doc.RootElement.EnumerateObject())
                {
                    if (slot.Value.ValueKind != JsonValueKind.Object) continue;
                    if (!slot.Value.TryGetProperty("strOptions", out var strOptProp)) continue;

                    string strOpts = strOptProp.GetString() ?? "";
                    if (string.IsNullOrEmpty(strOpts)) continue;

                    foreach (var pair in strOpts.Split(';'))
                    {
                        var kv = pair.Split(',');
                        if (kv.Length != 2) continue;
                        if (!int.TryParse(kv[0], out int optId)) continue;
                        if (!int.TryParse(kv[1], out int val))   continue;

                        switch (optId)
                        {
                            case 1: case 5: atk += val; break;
                            case 2: case 6: def += val; break;
                            case 3:          hp  += val; break;
                            case 4:          spd += val; break;
                        }
                    }
                }
            }
            catch { /* malformed JSON → ignore */ }

            return (hp, mp, atk, def, spd);
        }

        // ─── Potential bonus ─────────────────────────────────────────
        // JSON lưu số điểm đã đầu tư với key: "hp", "mp", "attack", "defense", "gene"
        // Mỗi điểm có giá trị: attack=5, hp=50, mp=30, defense=3
        private const int PtValueAtk = 5;
        private const int PtValueHp  = 50;
        private const int PtValueMp  = 30;
        private const int PtValueDef = 3;

        private static (int hp, int mp, int atk, int def, float spd)
            ParsePotentialBonus(string potentialJson)
        {
            int hp = 0, mp = 0, atk = 0, def = 0; float spd = 0f;
            if (string.IsNullOrWhiteSpace(potentialJson) || potentialJson == "{}") return (hp, mp, atk, def, spd);

            try
            {
                using var doc = JsonDocument.Parse(potentialJson);
                var root = doc.RootElement;

                // Key lưu là "hp"/"mp" (số điểm), nhân với value_per_point để ra bonus thực tế
                if (root.TryGetProperty("hp",      out var v1)) hp  = v1.GetInt32() * PtValueHp;
                if (root.TryGetProperty("mp",      out var v2)) mp  = v2.GetInt32() * PtValueMp;
                if (root.TryGetProperty("attack",  out var v3)) atk = v3.GetInt32() * PtValueAtk;
                if (root.TryGetProperty("defense", out var v4)) def = v4.GetInt32() * PtValueDef;
                // "gene" chỉ ảnh hưởng gene_exp, không cộng vào stat chiến đấu
            }
            catch { }

            return (hp, mp, atk, def, spd);
        }
    }
}
