using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameServerApi.Auth;
using GameServerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]
    public class DungeonRewardController : ControllerBase
    {
        private readonly GameDbContext _db;

        public DungeonRewardController(GameDbContext db)
        {
            _db = db;
        }

        [HttpPost("grant")]
        public async Task<IActionResult> Grant([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("targetPlayerId", out var playerIdProp) || !playerIdProp.TryGetInt32(out int targetPlayerId) || targetPlayerId <= 0)
                return BadRequest("Thiếu targetPlayerId hợp lệ.");

            if (!body.TryGetProperty("items", out var itemsProp))
                return BadRequest("Thiếu danh sách items.");

            var player = await _db.PlayerData.FindAsync(targetPlayerId);
            if (player == null)
                return NotFound($"Player {targetPlayerId} không tồn tại.");

            int maxSlots = player.GetInfoChar().BagSlots > 0 ? player.GetInfoChar().BagSlots : 20;

            var inventory = new List<Dictionary<string, object>>();
            if (!string.IsNullOrEmpty(player.InventoryJson) && player.InventoryJson != "[]")
            {
                var current = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(player.InventoryJson);
                if (current != null)
                {
                    foreach (var item in current)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var entry in item)
                        {
                            dict[entry.Key] = entry.Value.ValueKind switch
                            {
                                JsonValueKind.Number => entry.Value.TryGetInt32(out int intValue) ? intValue : entry.Value.GetDouble(),
                                JsonValueKind.String => entry.Value.GetString(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => entry.Value.ToString()
                            };
                        }
                        inventory.Add(dict);
                    }
                }
            }

            int addedCount = 0;
            var items = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(itemsProp.GetRawText()) ?? new List<Dictionary<string, JsonElement>>();

            foreach (var item in items)
            {
                if (!item.TryGetValue("itemTemplateId", out var templateProp) || !templateProp.TryGetInt32(out int itemTemplateId) || itemTemplateId <= 0)
                    continue;

                if (!item.TryGetValue("quantity", out var quantityProp) || !quantityProp.TryGetInt32(out int quantity) || quantity <= 0)
                    continue;

                int upgradeLevel = item.TryGetValue("upgradeLevel", out var upgradeProp) && upgradeProp.TryGetInt32(out int parsedUpgrade)
                    ? parsedUpgrade
                    : 0;
                string strOptions = item.TryGetValue("strOptions", out var optionsProp)
                    ? optionsProp.GetString() ?? string.Empty
                    : string.Empty;

                var itemTemplate = await _db.ItemTemplates.FindAsync(itemTemplateId);
                bool isStackable = itemTemplate != null && string.Equals(itemTemplate.IsXepChong, "True", StringComparison.OrdinalIgnoreCase);

                if (isStackable && upgradeLevel == 0)
                {
                    var existingSlot = inventory.FirstOrDefault(slot =>
                        slot.TryGetValue("itemTemplateId", out var rawTemplate)
                        && Convert.ToInt32(rawTemplate) == itemTemplateId);

                    if (existingSlot != null)
                    {
                        int currentQty = existingSlot.TryGetValue("quantity", out var rawQty) ? Convert.ToInt32(rawQty) : 0;
                        existingSlot["quantity"] = currentQty + quantity;
                        addedCount++;
                        continue;
                    }
                }

                int emptySlotIndex = FindFirstEmptySlot(inventory, maxSlots);
                if (emptySlotIndex < 0)
                    continue;

                inventory.RemoveAll(slot => slot.TryGetValue("slotIndex", out var rawSlotIndex) && Convert.ToInt32(rawSlotIndex) == emptySlotIndex);
                inventory.Add(new Dictionary<string, object>
                {
                    ["slotIndex"] = emptySlotIndex,
                    ["itemTemplateId"] = itemTemplateId,
                    ["quantity"] = quantity,
                    ["upgradeLevel"] = upgradeLevel,
                    ["strOptions"] = strOptions
                });
                addedCount++;
            }

            player.InventoryJson = JsonSerializer.Serialize(inventory);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã phát {addedCount} item reward cho player {targetPlayerId}.",
                player_id = targetPlayerId,
                added = addedCount
            });
        }

        private static int FindFirstEmptySlot(List<Dictionary<string, object>> inventory, int maxSlots)
        {
            for (int i = 0; i < maxSlots; i++)
            {
                var existing = inventory.FirstOrDefault(slot =>
                    slot.TryGetValue("slotIndex", out var rawSlotIndex)
                    && Convert.ToInt32(rawSlotIndex) == i);

                if (existing == null)
                    return i;

                if (!existing.TryGetValue("quantity", out var rawQty) || Convert.ToInt32(rawQty) <= 0)
                    return i;
            }

            return -1;
        }
    }
}
