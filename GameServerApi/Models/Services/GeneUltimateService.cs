namespace GameServerApi.Models.Services
{
    // Logic kích hoạt Gene Tối Thượng (Ultimate Gene).
    // Luồng:
    // • Chỉ áp dụng cho player ĐÃ Dung hợp Hybrid (IsHybrid == true) và chưa tối thượng (IsUltimate == false).
    // • Mỗi lần nhận gene exp (giết quái hoặc dùng item) → cộng dồn vào UltimateGeneExp.
    // • Khi UltimateGeneExp ≥ ultimate_exp_required → bật IsUltimate, gán aura path, hồi đầy HP/MP.
    // Hệ số nhân chỉ số (x1.5) được áp dụng tại StatCalculator khi IsUltimate == true.
    public static class GeneUltimateService
    {
        // Lấy config Gene Tối Thượng cho hệ tương ứng từ GeneUltimateSettings (hardcode, KHÔNG đọc DB).
        // Luôn trả về config hợp lệ.
        public static GeneUltimateConfig GetConfig(string? elementType)
            => GeneUltimateSettings.Resolve(elementType);

        // Cộng dồn gene exp vào tiến trình Gene Tối Thượng và kích hoạt nếu đủ ngưỡng.
        // Chỉ tác động khi info đủ điều kiện (đã Hybrid, chưa tối thượng) và cfg != null.
        // Trả về: true nếu vừa kích hoạt Gene Tối Thượng ở lần gọi này.
        public static bool TryAccumulateAndActivate(InfoChar info, int expGain, GeneUltimateConfig? cfg)
        {
            if (cfg == null) return false;
            if (!info.IsHybrid) return false;
            if (info.IsUltimate) return false;
            if (expGain <= 0) return false;

            info.UltimateGeneExp += expGain;

            if (info.UltimateGeneExp >= cfg.UltimateExpRequired)
            {
                Activate(info, cfg);
                return true;
            }

            return false;
        }

        // Bật trạng thái Gene Tối Thượng: set IsUltimate, gán aura path, hồi đầy HP/MP base.
        // Hệ số x1.5 áp dụng riêng tại StatCalculator nên không sửa base stats ở đây.
        public static void Activate(InfoChar info, GeneUltimateConfig cfg)
        {
            info.IsUltimate      = true;
            info.UltimateAuraPath = cfg.AuraPrefabPath;
            info.Hp = info.MaxHp;
            info.Mp = info.MaxMp;
        }
    }
}
