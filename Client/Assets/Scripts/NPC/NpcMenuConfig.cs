using System.Collections.Generic;

/// <summary>
/// Config menu động cho từng NPC — định nghĩa hoàn toàn trong C#, KHÔNG lưu DB.
///
/// Giống LangLa (Npc.java switch/case theo npc.id).
/// Mỗi entry là chuỗi "label:action_type" cách nhau bằng dấu ";".
///
/// action_type được xử lý tại NpcInteraction.SelectMenuItemServerRpc (server-side Unity):
///   open_shop           — mở cửa hàng (fetch npc_shop_item từ DB theo npc_id)
///   open_blacksmith     — mở giao diện thợ rèn (nâng cấp trang bị)
///   open_dungeon        — mở giao diện phó bản
///   reset_potential     — Tẩy tiềm năng (kiểm tra item/bạc → API → trả kết quả)
///   reset_skill         — Tẩy kỹ năng
///   learn_skill         — Luyện bí kíp
///   exchange_skill      — Đổi bí kíp
///   exchange_charm      — Đổi bùa nổ
///   lock_level          — Khoá / mở cấp nhân vật
///   close               — Đóng menu (Cáo từ)
///
/// Để thêm NPC mới: thêm case vào GetByNpcId.
/// Để thêm action_type mới: thêm case vào NpcInteraction.SelectMenuItemServerRpc VÀ NpcAction.Execute.
/// </summary>
public static class NpcMenuConfig
{
    // ─────────────────────────────────────────────────────────────────────
    //  Entry point chính — gọi từ NpcInteraction (server side)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về chuỗi "label:action_type;..." cho NPC cụ thể.
    /// Rỗng → không dùng dynamic menu, fallback về NpcMenuUI cũ.
    /// </summary>
    public static string GetMenuItems(int npcId, string npcType)
    {
        string byId = GetByNpcId(npcId);
        if (!string.IsNullOrEmpty(byId)) return byId;
        return GetByNpcType(npcType);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Config theo npc_id  (ưu tiên hơn npc_type)
    //  Dựa trên dữ liệu npc_config thực tế trong DB
    // ─────────────────────────────────────────────────────────────────────
    private static string GetByNpcId(int npcId)
    {
        switch (npcId)
        {
            // ── npc_id=1  Dược Phẩm (shop) ─────────────────────────────
            case 1:
                return "Mua dược phẩm:open_shop;" +
                       "Cáo từ:close";

            // ── npc_id=2  Đại Tướng Lan (quest) ────────────────────────
            //    Quest NPC — không có dynamic menu, dùng hội thoại
            case 2:
                return string.Empty;   // fallback về NpcMenuUI (dialogue)

            // ── npc_id=3  Thợ Rèn Hắc Long (blacksmith) ────────────────
            case 3:
                return "Cường Hóa Trang Bị:open_blacksmith;" +
                       "Nâng Tier Gene Chính:open_gene_upgrade;" +
                       "Chọn Hệ Thứ 2:open_secondary_select;" +
                       "Cường Hóa Tier Hệ Thứ 2:open_secondary_upgrade;" +
                       "Hợp Nhất Hybrid:open_hybrid_fusion;" +
                       "Cáo từ:close";

            // ── npc_id=5  Binh Khí (shop — map 0) ──────────────────────
            case 5:
                return "Mua binh khí:open_shop;" +
                       "Cáo từ:close";

            // ── npc_id=7  Trang Bị (shop — map 0) ──────────────────────
            case 7:
                return "Mua trang bị:open_shop;" +
                       "Cáo từ:close";

            // ── npc_id=8  Tiên Dược (shop — map 0) ─────────────────────
            case 8:
                return "Mua tiên dược:open_shop;" +
                       "Tẩy tiềm năng:reset_potential;" +
                       "Tẩy kỹ năng:reset_skill;" +
                       "Luyện bí kíp:learn_skill;" +
                       "Đổi bí kíp:exchange_skill;" +
                       "Đổi bùa nổ:exchange_charm;" +
                       "Khoá cấp nhân vật:lock_level;" +
                       "Cáo từ:close";

            // ── npc_id=12  Thương Nhân Cánh Đồng (shop — map 1) ─────────
            case 12:
                return "Mua đồ:open_shop;" +
                       "Cáo từ:close";

            // ── npc_id=13  Thợ Rèn Cánh Đồng (blacksmith — map 1) ───────
            case 13:
                return "Cường Hóa Trang Bị:open_blacksmith;" +
                       "Nâng Tier Gene Chính:open_gene_upgrade;" +
                       "Chọn Hệ Thứ 2:open_secondary_select;" +
                       "Cường Hóa Tier Hệ Thứ 2:open_secondary_upgrade;" +
                       "Hợp Nhất Hybrid:open_hybrid_fusion;" +
                       "Cáo từ:close";

            // ── npc_id=14  Hướng Dẫn Viên (quest — map 1) ─────────────
            case 14:
                return string.Empty;   // quest NPC, dùng hội thoại

            // ── npc_id=15  Thủ Môn Phó Bản (dungeon — map 0) ───────────
            case 15:
                return "Vào phó bản:open_dungeon;" +
                       "Cáo từ:close";

            default:
                return string.Empty;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Fallback theo npc_type
    // ─────────────────────────────────────────────────────────────────────
    private static string GetByNpcType(string npcType)
    {
        if (string.IsNullOrEmpty(npcType)) return string.Empty;

        switch (npcType.ToLowerInvariant())
        {
            case "shop":
                return "Mua đồ:open_shop;" +
                       "Cáo từ:close";

            case "blacksmith":
                return "Nâng cấp trang bị:open_blacksmith;" +
                       "Cáo từ:close";

            case "dungeon":
                return "Vào phó bản:open_dungeon;" +
                       "Cáo từ:close";

            case "quest":
            case "exchange":
            case "event":
            default:
                return string.Empty;
        }
    }
}
