using System.Collections.Generic;

namespace GameServerApi.Models
{
    // Cấu hình Gene Tối Thượng (Ultimate Gene) — tầng đỉnh cao SAU khi Dung hợp Hybrid.
    // Khi player (is_hybrid=true) tích đủ ultimate_gene_exp, server bật is_ultimate=true:
    // • Toàn bộ final_stats được nhân StatMultiplier (mặc định 1.5).
    // • Spawn aura sau lưng từ AuraPrefabPath.
    // Đây là config THUẦN (hardcode trong code), KHÔNG đọc từ DB.
    // Muốn chỉnh thì sửa trực tiếp các hằng số trong GeneUltimateSettings.
    public class GeneUltimateConfig
    {
        // Hệ áp dụng. "ALL" = config dùng chung cho mọi hệ.
        public string ElementType { get; set; } = "ALL";

        // Tổng ultimate_gene_exp cần tích để kích hoạt Gene Tối Thượng.
        public int UltimateExpRequired { get; set; } = GeneUltimateSettings.DefaultExpRequired;

        // Hệ số nhân toàn bộ final_stats khi đã tối thượng (mặc định 1.5).
        public float StatMultiplier { get; set; } = GeneUltimateSettings.DefaultStatMultiplier;

        // Resources path prefab aura sau lưng (không có Assets/ prefix và không có .prefab).
        // Ví dụ: "Prefabs/Player/Aura/UltimateAura".
        public string AuraPrefabPath { get; set; } = GeneUltimateSettings.DefaultAuraPrefabPath;
    }

    // Nơi tập trung TOÀN BỘ tham số Gene Tối Thượng. Chỉnh ở đây thay vì DB.
    public static class GeneUltimateSettings
    {
        // Giá trị mặc định dùng chung cho mọi hệ
        public const int    DefaultExpRequired    = 1_000_000;
        public const float  DefaultStatMultiplier = 1.5f;
        public const string DefaultAuraPrefabPath = "Prefabs/Player/Aura/UltimateAura";

        // Override riêng theo từng hệ (nếu cần). Để trống = mọi hệ dùng giá trị mặc định.
        // Ví dụ thêm: ["Fire"] = new GeneUltimateConfig { ElementType = "Fire", UltimateExpRequired = 800_000 }.
        private static readonly Dictionary<string, GeneUltimateConfig> Overrides
            = new(System.StringComparer.OrdinalIgnoreCase)
        {
            // (trống — thêm config theo hệ tại đây nếu muốn)
        };

        // Lấy config cho hệ tương ứng. Ưu tiên override theo hệ, fallback về giá trị mặc định ("ALL").
        // Luôn trả về một config hợp lệ (không bao giờ null).
        public static GeneUltimateConfig Resolve(string? elementType)
        {
            if (!string.IsNullOrWhiteSpace(elementType) &&
                Overrides.TryGetValue(elementType, out var cfg))
            {
                return cfg;
            }

            return new GeneUltimateConfig
            {
                ElementType         = "ALL",
                UltimateExpRequired = DefaultExpRequired,
                StatMultiplier      = DefaultStatMultiplier,
                AuraPrefabPath      = DefaultAuraPrefabPath,
            };
        }
    }
}
