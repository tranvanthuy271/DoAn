using GameServerApi.Data;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/npc")]
    public class NpcController : ControllerBase
    {
        private readonly GameDbContext _db;

        public NpcController(GameDbContext db) => _db = db;

        // ══════════════════════════════════════════════════════════════
        //  GET /api/npc/list?mapId=0
        //  Lấy danh sách NPC active trên một map
        // ══════════════════════════════════════════════════════════════
        [HttpGet("list")]
        public async Task<IActionResult> GetNpcList([FromQuery] int mapId = 0)
        {
            var npcs = await _db.NpcConfigs
                .Where(n => n.MapId == mapId && n.IsActive)
                .Select(n => new
                {
                    npc_id       = n.NpcId,
                    npc_name     = n.NpcName,
                    npc_type     = n.NpcType,
                    pos_x        = n.PosX,
                    pos_y        = n.PosY,
                    icon_id      = n.IconId,
                    dialogue_key = n.DialogueKey,
                })
                .ToListAsync();

            return Ok(npcs);
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/npc/interact
        //  Body: { "playerId": 1, "npcId": 1 }
        //  Trả về dialogue node đầu tiên và action của NPC
        // ══════════════════════════════════════════════════════════════
        [HttpPost("interact")]
        public async Task<IActionResult> Interact([FromBody] JsonElement body)
        {
            if (!TryGetIntProperty(body, "playerId", "player_id", out int requestedPlayerId) ||
                !TryGetIntProperty(body, "npcId", "npc_id", out int npcId))
                return BadRequest("Thiếu playerId hoặc npcId.");

            if (requestedPlayerId <= 0 || npcId <= 0)
                return BadRequest("playerId hoặc npcId không hợp lệ.");

            int playerId = requestedPlayerId;
            string? playerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? User.FindFirstValue("sub");
            if (int.TryParse(playerIdClaim, out int tokenPlayerId) && tokenPlayerId > 0)
                playerId = tokenPlayerId;

            var npc = await _db.NpcConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.NpcId == npcId && n.IsActive);
            if (npc == null)
                return NotFound("NPC không tồn tại hoặc đã bị vô hiệu hóa.");

            bool playerExists = await _db.PlayerData
                .AsNoTracking()
                .AnyAsync(p => p.PlayerId == playerId);
            if (!playerExists)
                return NotFound("Player không tồn tại.");

            // Lấy dialogue node khởi đầu
            NpcDialogue? dialogue = null;
            if (!string.IsNullOrEmpty(npc.DialogueKey))
            {
                dialogue = await _db.NpcDialogues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.NpcId == npcId && d.DialogueKey == npc.DialogueKey);
            }

            string dialogueText = dialogue?.TextVi ?? string.Empty;

            return Ok(new
            {
                npcId    = npc.NpcId,
                npcName  = npc.NpcName,
                npcType  = npc.NpcType,
                dialogue_text = dialogueText,
                dialogue = dialogue == null ? null : new
                {
                    key        = dialogue.DialogueKey,
                    text       = dialogueText,
                    nextKey    = dialogue.NextKey,
                    actionType = dialogue.ActionType,
                },
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/npc/dialogue/next
        //  Body: { "npcId": 1, "dialogueKey": "quest_intro" }
        //  Lấy node kế tiếp trong cây hội thoại
        // ══════════════════════════════════════════════════════════════
        [HttpPost("dialogue/next")]
        public async Task<IActionResult> NextDialogue([FromBody] System.Text.Json.JsonElement body)
        {
            if (!body.TryGetProperty("npcId",        out var nidProp) ||
                !body.TryGetProperty("dialogueKey",  out var dkProp))
                return BadRequest("Thiếu npcId hoặc dialogueKey.");

            int    npcId       = nidProp.GetInt32();
            string dialogueKey = dkProp.GetString() ?? "";

            var node = await _db.NpcDialogues
                .FirstOrDefaultAsync(d => d.NpcId == npcId && d.DialogueKey == dialogueKey);

            if (node == null)
                return NotFound("Không tìm thấy dialogue node.");

            return Ok(new
            {
                key        = node.DialogueKey,
                text       = node.TextVi,
                nextKey    = node.NextKey,
                actionType = node.ActionType,
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/npc/shop?npcId=1&playerId=1
        //  Lấy danh sách item của shop NPC, kèm giá và level yêu cầu
        // ══════════════════════════════════════════════════════════════
        [HttpGet("shop")]
        public async Task<IActionResult> GetShop([FromQuery] int npcId, [FromQuery] int playerId)
        {
            var npc = await _db.NpcConfigs.FindAsync(npcId);
            if (npc == null || !npc.IsActive)
                return NotFound("NPC không tồn tại.");
            if (npc.NpcType != "shop" && npc.NpcType != "blacksmith")
                return BadRequest("NPC này không phải cửa hàng.");

            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null)
                return NotFound("Player không tồn tại.");

            var info        = player.GetInfoChar();
            int playerLevel = info.Level;

            var rawItems = await _db.NpcShopItems
                .Where(s => s.NpcId == npcId)
                .Join(_db.ItemTemplates,
                      s => s.ItemTemplateId,
                      t => t.Id,
                      (s, t) => new
                      {
                          ShopItemId    = s.Id,
                          s.ItemTemplateId,
                          ItemName      = t.Name,
                          IconId        = t.IdIcon,
                          s.PriceSilver,
                          s.PriceGold,
                          s.Stock,
                          s.RequiredLevel,
                      })
                .ToListAsync();

            // Trả về JSON array với snake_case để JsonUtility (Unity) parse được.
            // ShowShop() phía client bọc thành {"items":[...]} trước khi parse ShopListWrapper.
            return Ok(rawItems.Select(i => new
            {
                shop_item_id     = i.ShopItemId,
                item_template_id = i.ItemTemplateId,
                item_name        = i.ItemName,
                item_detail      = "",
                icon_id          = i.IconId,
                price_silver     = i.PriceSilver,
                price_gold       = i.PriceGold,
                stock            = i.Stock,
                required_level   = i.RequiredLevel,
                can_afford       = i.PriceGold > 0 ? info.Gold >= i.PriceGold
                                                   : info.Silver >= i.PriceSilver,
                meets_level      = playerLevel >= i.RequiredLevel,
            }));
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/npc/shop/buy
        //  Headers: Authorization: Bearer <JWT>
        //  Body: { "npcId": 1, "shopItemId": 1, "quantity": 1 }
        //  Mua item từ shop NPC — server-authoritative
        //  playerId lấy từ JWT claim (không tin body)
        // ══════════════════════════════════════════════════════════════
        [Authorize]
        [HttpPost("shop/buy")]
        public async Task<IActionResult> BuyItem([FromBody] System.Text.Json.JsonElement body)
        {
            // Lấy playerId từ JWT claim thay vì tin vào body
            var playerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue("sub");
            if (!int.TryParse(playerIdClaim, out int playerId))
                return Unauthorized("Token không hợp lệ.");

            if (!body.TryGetProperty("npcId",      out var nidProp)  ||
                !body.TryGetProperty("shopItemId", out var siProp))
                return BadRequest("Thiếu npcId hoặc shopItemId.");

            int npcId      = nidProp.GetInt32();
            int shopItemId = siProp.GetInt32();
            int quantity   = body.TryGetProperty("quantity", out var qProp) ? qProp.GetInt32() : 1;
            if (quantity <= 0) quantity = 1;

            var npc = await _db.NpcConfigs.FindAsync(npcId);
            if (npc == null || !npc.IsActive)
                return NotFound("NPC không tồn tại.");

            var shopItem = await _db.NpcShopItems
                .Include(s => s.ItemTemplate)
                .FirstOrDefaultAsync(s => s.Id == shopItemId && s.NpcId == npcId);
            if (shopItem == null)
                return NotFound("Item không tồn tại trong shop.");

            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null)
                return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            // Kiểm tra level
            if (info.Level < shopItem.RequiredLevel)
                return BadRequest($"Yêu cầu level {shopItem.RequiredLevel}.");

            // Kiểm tra tồn kho
            if (shopItem.Stock != -1 && shopItem.Stock < quantity)
                return BadRequest($"Chỉ còn {shopItem.Stock} trong kho.");

            int totalSilver = shopItem.PriceSilver * quantity;
            int totalGold   = shopItem.PriceGold   * quantity;

            // Kiểm tra tiền
            if (totalGold > 0 && info.Gold < totalGold)
                return BadRequest($"Không đủ vàng. Cần {totalGold:N0}, có {info.Gold:N0}.");
            if (totalSilver > 0 && totalGold == 0 && info.Silver < totalSilver)
                return BadRequest($"Không đủ bạc. Cần {totalSilver:N0}, có {info.Silver:N0}.");

            // Trừ tiền
            if (totalGold > 0)
                info.Gold -= totalGold;
            else
                info.Silver -= totalSilver;

            // Trừ tồn kho
            if (shopItem.Stock != -1)
                shopItem.Stock -= quantity;

            // Thêm item vào inventory
            var inventory = ParseJsonList(player.InventoryJson);
            var existing = inventory.FirstOrDefault(s =>
                s.ContainsKey("itemTemplateId") &&
                Convert.ToInt32(s["itemTemplateId"]) == shopItem.ItemTemplateId &&
                !(s.ContainsKey("isEquipped") && Convert.ToBoolean(s["isEquipped"])));

            if (existing != null)
            {
                int cur = existing.ContainsKey("quantity") ? Convert.ToInt32(existing["quantity"]) : 1;
                existing["quantity"] = cur + quantity;
            }
            else
            {
                // Tìm slotIndex tiếp theo chưa bị chiếm
                var usedSlots = new System.Collections.Generic.HashSet<int>(
                    inventory
                        .Where(s => s.ContainsKey("slotIndex"))
                        .Select(s => Convert.ToInt32(s["slotIndex"]))
                );
                int nextSlot = 0;
                while (usedSlots.Contains(nextSlot)) nextSlot++;

                inventory.Add(new Dictionary<string, object>
                {
                    ["slotIndex"]      = nextSlot,
                    ["itemTemplateId"] = shopItem.ItemTemplateId,
                    ["itemCode"]       = shopItem.ItemTemplate?.Name ?? "",
                    ["iconId"]         = shopItem.ItemTemplate?.IdIcon.ToString() ?? "",
                    ["quantity"]       = quantity,
                    ["isEquipped"]     = false,
                    ["upgradeLevel"]   = 0,
                });
            }

            player.SetInfoChar(info);
            player.InventoryJson = System.Text.Json.JsonSerializer.Serialize(inventory);
            player.UpdatedAt     = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success      = true,
                playerGold   = info.Gold,
                playerSilver = info.Silver,
                message      = $"Mua thành công {quantity}x {shopItem.ItemTemplate?.Name}.",
            });
        }

        // ── Helpers ──────────────────────────────────────────────────
        private static bool TryGetIntProperty(JsonElement body, string primaryName, string alternateName, out int value)
        {
            if (TryReadInt(body, primaryName, out value))
                return true;

            if (!string.IsNullOrWhiteSpace(alternateName) && TryReadInt(body, alternateName, out value))
                return true;

            value = 0;
            return false;
        }

        private static bool TryReadInt(JsonElement body, string propertyName, out int value)
        {
            if (body.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out value))
                    return true;

                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value))
                    return true;
            }

            value = 0;
            return false;
        }

        private static List<Dictionary<string, object>> ParseJsonList(string? json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]" || json == "{}")
                return new();
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<
                    List<Dictionary<string, System.Text.Json.JsonElement>>>(json);
                if (arr == null) return new();
                return arr
                    .Select(d => d.ToDictionary(
                        kv => kv.Key,
                        kv => (object)(kv.Value.ValueKind == System.Text.Json.JsonValueKind.Number
                            ? (object)kv.Value.GetDecimal()
                            : kv.Value.ValueKind == System.Text.Json.JsonValueKind.True  ? true
                            : kv.Value.ValueKind == System.Text.Json.JsonValueKind.False ? false
                            : (object?)kv.Value.GetString() ?? "")))
                    .ToList();
            }
            catch { return new(); }
        }
    }
}
