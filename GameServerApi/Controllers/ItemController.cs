using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly GameDbContext _db;

        public ItemController(GameDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/item/templates
        /// Lấy danh sách tất cả item templates
        /// Không cần Authorization để client có thể load trước khi login
        /// </summary>
        [HttpGet("templates")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllItemTemplates()
        {
            Console.WriteLine($"[ItemController] 📥 GET /api/item/templates - Request received");
            try
            {
                var itemTemplates = await _db.ItemTemplates
                    .OrderBy(i => i.Id)
                    .ToListAsync();

                Console.WriteLine($"[ItemController] 📊 Found {itemTemplates.Count} item templates in database");

                // Convert sang format phù hợp cho Unity (DB v3.0 – LangLa schema)
                var response = itemTemplates.Select(item => new
                {
                    id          = item.Id,
                    name        = item.Name,
                    detail      = item.Detail,
                    isXepChong  = item.IsXepChong == "True",
                    gioiTinh    = item.GioiTinh,
                    type        = item.Type,
                    idClass     = item.IdClass,
                    idIcon      = item.IdIcon,
                    levelNeed   = item.LevelNeed,
                    taiPhuNeed  = item.TaiPhuNeed,
                    isLock      = item.IsLock,
                    sellPrice   = item.SellPrice
                }).ToList();

                Console.WriteLine($"[ItemController] ✅ Returning {response.Count} item templates");
                
                // Log first 5 items for debugging
                int logCount = Math.Min(5, response.Count);
                Console.WriteLine($"[ItemController] 📋 Sample items (first {logCount}):");
                for (int i = 0; i < logCount; i++)
                {
                    var item = response[i];
                    Console.WriteLine($"  [{i+1}] ID={item.id}, Name='{item.name}', type={item.type}, idIcon={item.idIcon}");
                }

                return Ok(new
                {
                    count = response.Count,
                    item_templates = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ItemController] ❌ Error in GetAllItemTemplates: {ex.Message}");
                Console.WriteLine($"[ItemController] Stack trace: {ex.StackTrace}");
                return BadRequest(new
                {
                    error = "Lỗi khi lấy item templates",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/item/templates/{id}
        /// Lấy thông tin chi tiết 1 item template theo ID
        /// </summary>
        [HttpGet("templates/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetItemTemplateById(int id)
        {
            try
            {
                var item = await _db.ItemTemplates.FindAsync(id);
                
                if (item == null)
                {
                    return NotFound(new
                    {
                        error = "Item template không tồn tại",
                        id = id
                    });
                }

                return Ok(new
                {
                    id         = item.Id,
                    name       = item.Name,
                    detail     = item.Detail,
                    isXepChong = item.IsXepChong == "True",
                    gioiTinh   = item.GioiTinh,
                    type       = item.Type,
                    idClass    = item.IdClass,
                    idIcon     = item.IdIcon,
                    levelNeed  = item.LevelNeed,
                    taiPhuNeed = item.TaiPhuNeed,
                    isLock     = item.IsLock,
                    sellPrice  = item.SellPrice
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Lỗi khi lấy item template",
                    message = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/item/debug/add-fusion-cores?playerId=X
        //  DEBUG ONLY — Thêm 10 Lõi Đột Biến theo hệ phụ của player
        //  vào túi đồ. Không dùng trên production.
        // ══════════════════════════════════════════════════════════════
        [HttpPost("debug/add-fusion-cores")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugAddFusionCores([FromQuery] int playerId)
        {
            // Mapping element → item_id (phải đồng bộ với GeneController)
            var elementItemMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Fire"]  = 47, ["Water"] = 48, ["Earth"] = 49,
                ["Metal"] = 50, ["Wood"]  = 51, ["Wind"]  = 52,
            };
            const int qty = 10;

            try
            {
                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                    return NotFound(new { error = "Player không tồn tại." });

                var info = player.GetInfoChar();
                string? secondary = info.SecondaryElement;

                // Nếu chưa chọn hệ phụ thì dùng hệ chính làm fallback
                string targetElement = secondary ?? info.ElementType;
                if (!elementItemMap.TryGetValue(targetElement, out int itemId))
                    return BadRequest(new { error = $"Không có lõi đột biến cho hệ {targetElement}." });

                var itemTemplate = await _db.ItemTemplates.FindAsync(itemId);

                // Parse inventory JSON (dùng cùng helper pattern của GeneController)
                var inventory = ParseInventory(player.InventoryJson);

                // Tìm slot đã có item này
                var existing = inventory.FirstOrDefault(s =>
                    s.ContainsKey("itemTemplateId") &&
                    Convert.ToInt32(s["itemTemplateId"]) == itemId);

                if (existing != null)
                {
                    existing["quantity"] = Convert.ToInt32(existing["quantity"]) + qty;
                }
                else
                {
                    // Tìm slotIndex trống (max + 1)
                    int nextSlot = inventory.Count == 0 ? 0
                        : inventory.Max(s => s.ContainsKey("slotIndex") ? Convert.ToInt32(s["slotIndex"]) : 0) + 1;

                    inventory.Add(new Dictionary<string, object>
                    {
                        ["slotIndex"]      = nextSlot,
                        ["itemTemplateId"] = itemId,
                        ["quantity"]       = qty,
                        ["itemCode"]       = itemTemplate?.Name ?? $"item_{itemId}",
                        ["iconId"]         = itemTemplate?.IdIcon.ToString() ?? "0",
                    });
                }

                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt     = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success       = true,
                    addedItemId   = itemId,
                    addedItemName = itemTemplate?.Name ?? $"Item #{itemId}",
                    element       = targetElement,
                    qty,
                    message       = $"[DEBUG] Đã thêm {qty}x {itemTemplate?.Name} (id={itemId}) vào túi player {playerId}.",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Helper (local, không share với GeneController) ───────────
        private static List<Dictionary<string, object>> ParseInventory(string json)
        {
            var result = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(json) || json == "[]") return result;
            try
            {
                var raw = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                if (raw == null) return result;
                foreach (var item in raw)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var kvp in item)
                        dict[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.Number => (object)kvp.Value.GetDouble(),
                            JsonValueKind.True   => true,
                            JsonValueKind.False  => false,
                            JsonValueKind.String => kvp.Value.GetString() ?? "",
                            JsonValueKind.Null   => null!,
                            _                   => kvp.Value.GetRawText()
                        };
                    result.Add(dict);
                }
            }
            catch { }
            return result;
        }
    }
}
