using System;
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
    [Authorize]
    public class PlayerController : ControllerBase
    {
        private readonly GameDbContext _db;

        public PlayerController(GameDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// POST /api/player/create
        /// Body: { "element_type": "Fire", "gender": "Male" }
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePlayer([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("element_type", out var elementProp))
            {
                return BadRequest("element_type là bắt buộc.");
            }

            var elementType = elementProp.GetString() ?? "Fire";
            
            // Lấy gender, mặc định là "Male"
            string gender = "Male";
            if (body.TryGetProperty("gender", out var genderProp))
            {
                gender = genderProp.GetString() ?? "Male";
            }
            
            // Lấy character_name
            string characterName = "";
            if (body.TryGetProperty("character_name", out var nameProp))
            {
                characterName = nameProp.GetString() ?? "";
            }
            
            // Validate gender
            if (gender != "Male" && gender != "Female")
            {
                return BadRequest("gender phải là 'Male' hoặc 'Female'.");
            }
            
            // Validate: Earth chỉ có nam
            if (elementType == "Earth" && gender != "Male")
            {
                return BadRequest("Hệ Earth chỉ có thể chọn giới tính Nam.");
            }
            
            // Validate character_name
            if (string.IsNullOrWhiteSpace(characterName))
            {
                return BadRequest("Tên nhân vật là bắt buộc.");
            }
            
            if (characterName.Length < 3 || characterName.Length > 20)
            {
                return BadRequest("Tên nhân vật phải có từ 3 đến 20 ký tự.");
            }

            // Lấy user_id từ JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            // Sử dụng AsNoTracking để tránh tracking issues và kiểm tra chính xác hơn
            var existing = await _db.PlayerData
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlayerId == userId);
            
            if (existing != null)
            {
                // Đã có player_data, cần attach và update
                var existingTracked = await _db.PlayerData.FindAsync(userId);
                if (existingTracked != null)
                {
                    existingTracked.ElementType = elementType;
                    existingTracked.Gender = gender;
                    existingTracked.CharacterName = characterName;
                    existingTracked.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    
                    // Trả về format đúng cho client
                    var response = new
                    {
                        player_id = existingTracked.PlayerId,
                        level = existingTracked.Level,
                        experience = existingTracked.Experience,
                        exp_required_for_next_level = 0,
                        gold = existingTracked.Gold,
                        map_id = existingTracked.MapId,
                        position_x = existingTracked.PositionX,
                        position_y = existingTracked.PositionY,
                        base_stats = new
                        {
                            hp = existingTracked.Hp,
                            max_hp = existingTracked.MaxHp,
                            mp = existingTracked.Mp,
                            max_mp = existingTracked.MaxMp,
                            attack = existingTracked.Attack
                        },
                        equipment = JsonSerializer.Deserialize<object>(existingTracked.EquipmentJson),
                        potential_stats = JsonSerializer.Deserialize<object>(existingTracked.PotentialStatsJson),
                        final_stats = new
                        {
                            hp = existingTracked.MaxHp,
                            max_hp = existingTracked.MaxHp,
                            mp = existingTracked.MaxMp,
                            max_mp = existingTracked.MaxMp,
                            attack = existingTracked.Attack,
                            move_speed = 5f
                        },
                        inventory = JsonSerializer.Deserialize<object>(existingTracked.InventoryJson),
                        skills = JsonSerializer.Deserialize<object>(existingTracked.SkillsJson),
                        skill_points_available = 0,
                        potential_points_available = 0,
                        element_type = existingTracked.ElementType,
                        gene_tier = existingTracked.GeneTier,
                        is_hybrid = existingTracked.IsHybrid,
                        gender = existingTracked.Gender,
                        character_name = existingTracked.CharacterName
                    };
                    
                    return Ok(response);
                }
            }

            // Tạo player mới với try-catch để xử lý race condition
            var playerData = new PlayerData
            {
                PlayerId = userId,
                Level = 1,
                Experience = 0,
                Gold = 0,
                MapId = 0,
                ElementType = elementType,
                Gender = gender,
                CharacterName = characterName,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _db.PlayerData.Add(playerData);
                await _db.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException != null && 
                                                                              ex.InnerException.Message.Contains("Duplicate entry"))
            {
                // Nếu bị duplicate key (race condition), load lại và update
                _db.Entry(playerData).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                
                var existingPlayer = await _db.PlayerData.FindAsync(userId);
                if (existingPlayer != null)
                {
                    existingPlayer.ElementType = elementType;
                    existingPlayer.Gender = gender;
                    existingPlayer.CharacterName = characterName;
                    existingPlayer.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    playerData = existingPlayer;
                }
                else
                {
                    return StatusCode(500, "Lỗi khi tạo player. Vui lòng thử lại.");
                }
            }

            // Trả về format đúng cho client
            var createResponse = new
            {
                player_id = playerData.PlayerId,
                level = playerData.Level,
                experience = playerData.Experience,
                exp_required_for_next_level = 0,
                gold = playerData.Gold,
                map_id = playerData.MapId,
                position_x = playerData.PositionX,
                position_y = playerData.PositionY,
                base_stats = new
                {
                    hp = playerData.Hp,
                    max_hp = playerData.MaxHp,
                    mp = playerData.Mp,
                    max_mp = playerData.MaxMp,
                    attack = playerData.Attack
                },
                equipment = JsonSerializer.Deserialize<object>(playerData.EquipmentJson),
                potential_stats = JsonSerializer.Deserialize<object>(playerData.PotentialStatsJson),
                final_stats = new
                {
                    hp = playerData.MaxHp,
                    max_hp = playerData.MaxHp,
                    mp = playerData.MaxMp,
                    max_mp = playerData.MaxMp,
                    attack = playerData.Attack,
                    move_speed = 5f
                },
                inventory = JsonSerializer.Deserialize<object>(playerData.InventoryJson),
                skills = JsonSerializer.Deserialize<object>(playerData.SkillsJson),
                skill_points_available = 0,
                potential_points_available = 0,
                element_type = playerData.ElementType,
                gene_tier = playerData.GeneTier,
                is_hybrid = playerData.IsHybrid,
                gender = playerData.Gender,
                character_name = playerData.CharacterName
            };

            return Ok(createResponse);
        }

        /// <summary>
        /// GET /api/player/{playerId}/data
        /// Trả về dữ liệu player theo format đã dùng trong Unity.
        /// Bước đầu trả dữ liệu đơn giản dựa trên PlayerData.
        /// </summary>
        [HttpGet("{playerId}/data")]
        [AllowAnonymous] // Có thể đổi sang [Authorize] khi client gửi JWT
        public async Task<IActionResult> GetPlayerData(int playerId)
        {
            var player = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null)
            {
                return NotFound("Player không tồn tại.");
            }

            // Tạm thời trả về dữ liệu đơn giản, có thể mở rộng dần cho khớp hoàn toàn tài liệu.
            var response = new
            {
                player_id = player.PlayerId,
                level = player.Level,
                experience = player.Experience,
                exp_required_for_next_level = 0,
                gold = player.Gold,
                map_id = player.MapId,
                position_x = player.PositionX,
                position_y = player.PositionY,
                base_stats = new
                {
                    hp = player.Hp,
                    max_hp = player.MaxHp,
                    mp = player.Mp,
                    max_mp = player.MaxMp,
                    attack = player.Attack
                },
                equipment = JsonSerializer.Deserialize<object>(player.EquipmentJson),
                potential_stats = JsonSerializer.Deserialize<object>(player.PotentialStatsJson),
                final_stats = new
                {
                    hp = player.MaxHp,
                    max_hp = player.MaxHp,
                    mp = player.MaxMp,
                    max_mp = player.MaxMp,
                    attack = player.Attack,
                    move_speed = 5f
                },
                inventory = JsonSerializer.Deserialize<object>(player.InventoryJson),
                skills = JsonSerializer.Deserialize<object>(player.SkillsJson),
                skill_points_available = 0,
                potential_points_available = 0,
                element_type = player.ElementType,
                gene_tier = player.GeneTier,
                is_hybrid = player.IsHybrid,
                gender = player.Gender,
                character_name = player.CharacterName
            };

            return Ok(response);
        }

        /// <summary>
        /// PUT /api/player/{playerId}/position
        /// Update position của player (khi out game hoặc disconnect)
        /// </summary>
        [HttpPut("{playerId}/position")]
        [AllowAnonymous] // Có thể đổi sang [Authorize] khi client gửi JWT
        public async Task<IActionResult> UpdatePlayerPosition(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                int mapId = body.GetProperty("map_id").GetInt32();
                float positionX = (float)body.GetProperty("position_x").GetDouble();
                float positionY = (float)body.GetProperty("position_y").GetDouble();

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                {
                    return NotFound("Player không tồn tại.");
                }

                // Update position
                player.MapId = mapId;
                player.PositionX = positionX;
                player.PositionY = positionY;
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Position updated successfully",
                    map_id = player.MapId,
                    position_x = player.PositionX,
                    position_y = player.PositionY
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi update position: {ex.Message}");
            }
        }

        /// <summary>
        /// PUT /api/player/{playerId}/data
        /// Update player data (batch update) - dùng cho batch save từ PlayerDataSaveService
        /// </summary>
        [HttpPut("{playerId}/data")]
        [AllowAnonymous] // Có thể đổi sang [Authorize] khi client gửi JWT
        public async Task<IActionResult> UpdatePlayerData(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                {
                    return NotFound("Player không tồn tại.");
                }

                // Update các field từ body (chỉ update field có trong request)
                if (body.TryGetProperty("level", out var levelProp))
                {
                    player.Level = levelProp.GetInt32();
                }

                if (body.TryGetProperty("experience", out var expProp))
                {
                    player.Experience = expProp.GetInt32();
                }

                if (body.TryGetProperty("gold", out var goldProp))
                {
                    player.Gold = goldProp.GetInt32();
                }

                if (body.TryGetProperty("hp", out var hpProp))
                {
                    player.Hp = hpProp.GetInt32();
                }

                if (body.TryGetProperty("max_hp", out var maxHpProp))
                {
                    player.MaxHp = maxHpProp.GetInt32();
                }

                if (body.TryGetProperty("mp", out var mpProp))
                {
                    player.Mp = mpProp.GetInt32();
                }

                if (body.TryGetProperty("max_mp", out var maxMpProp))
                {
                    player.MaxMp = maxMpProp.GetInt32();
                }

                if (body.TryGetProperty("attack", out var attackProp))
                {
                    player.Attack = attackProp.GetInt32();
                }

                if (body.TryGetProperty("map_id", out var mapIdProp))
                {
                    player.MapId = mapIdProp.GetInt32();
                }

                if (body.TryGetProperty("position_x", out var posXProp))
                {
                    player.PositionX = (float)posXProp.GetDouble();
                }

                if (body.TryGetProperty("position_y", out var posYProp))
                {
                    player.PositionY = (float)posYProp.GetDouble();
                }

                // Update JSON fields nếu có
                if (body.TryGetProperty("equipment", out var equipmentProp))
                {
                    player.EquipmentJson = equipmentProp.GetRawText();
                }

                if (body.TryGetProperty("inventory", out var inventoryProp))
                {
                    player.InventoryJson = inventoryProp.GetRawText();
                }

                if (body.TryGetProperty("skills", out var skillsProp))
                {
                    player.SkillsJson = skillsProp.GetRawText();
                }

                if (body.TryGetProperty("potential_stats", out var potentialProp))
                {
                    player.PotentialStatsJson = potentialProp.GetRawText();
                }

                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Player data updated successfully",
                    player_id = player.PlayerId,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi update player data: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/inventory/add
        /// Body: { "items": [{ "itemTemplateId": 1, "itemCode": "ITEM_ICON_121", "iconId": "client_icon_121", "quantity": 5 }] }
        /// Thêm item vào inventory của player
        /// </summary>
        [HttpPost("{playerId}/inventory/add")]
        public async Task<IActionResult> AddItemsToInventory(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                // Lấy user_id từ JWT
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
                if (userIdClaim == null)
                {
                    return Unauthorized();
                }

                var userId = int.Parse(userIdClaim.Value);

                // Kiểm tra quyền: chỉ được update player của chính mình
                if (playerId != userId)
                {
                    return Forbid();
                }

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                {
                    return NotFound($"Player với ID {playerId} không tồn tại.");
                }

                // Parse inventory hiện tại
                var inventory = new List<Dictionary<string, object>>();
                if (!string.IsNullOrEmpty(player.InventoryJson) && player.InventoryJson != "[]")
                {
                    var existingInventory = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(player.InventoryJson);
                    if (existingInventory != null)
                    {
                        foreach (var item in existingInventory)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (var kvp in item)
                            {
                                dict[kvp.Key] = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : kvp.Value.GetDouble(),
                                    JsonValueKind.String => kvp.Value.GetString(),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    _ => kvp.Value.ToString()
                                };
                            }
                            inventory.Add(dict);
                        }
                    }
                }

                // Parse items cần thêm
                if (!body.TryGetProperty("items", out var itemsProp))
                {
                    return BadRequest("Thiếu field 'items' trong request body.");
                }

                var itemsToAdd = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(itemsProp.GetRawText());
                if (itemsToAdd == null || itemsToAdd.Count == 0)
                {
                    return BadRequest("Danh sách items trống.");
                }

                int maxSlots = 20; // Số slot tối đa
                int addedCount = 0;

                foreach (var itemToAdd in itemsToAdd)
                {
                    if (!itemToAdd.TryGetValue("itemTemplateId", out var templateIdElem) ||
                        !itemToAdd.TryGetValue("itemCode", out var codeElem) ||
                        !itemToAdd.TryGetValue("iconId", out var iconIdElem) ||
                        !itemToAdd.TryGetValue("quantity", out var qtyElem))
                    {
                        continue; // Skip invalid item
                    }

                    int itemTemplateId = templateIdElem.GetInt32();
                    string itemCode = codeElem.GetString() ?? "";
                    string iconId = iconIdElem.GetString() ?? "";
                    int quantity = qtyElem.GetInt32();

                    // Tìm slot trống
                    int emptySlotIndex = -1;
                    for (int i = 0; i < maxSlots; i++)
                    {
                        var existingSlot = inventory.FirstOrDefault(s => 
                            s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == i);
                        
                        if (existingSlot == null || !existingSlot.ContainsKey("quantity") || Convert.ToInt32(existingSlot["quantity"]) == 0)
                        {
                            emptySlotIndex = i;
                            break;
                        }
                    }

                    if (emptySlotIndex == -1)
                    {
                        // Inventory đầy
                        continue;
                    }

                    // Thêm item vào slot trống
                    var newSlot = new Dictionary<string, object>
                    {
                        ["slotIndex"] = emptySlotIndex,
                        ["itemTemplateId"] = itemTemplateId,
                        ["itemCode"] = itemCode,
                        ["iconId"] = iconId,
                        ["quantity"] = quantity,
                        ["isEquipped"] = false
                    };

                    // Xóa slot cũ nếu có
                    inventory.RemoveAll(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == emptySlotIndex);
                    inventory.Add(newSlot);
                    addedCount++;
                }

                // Serialize và lưu lại
                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Đã thêm {addedCount} item(s) vào inventory",
                    player_id = playerId,
                    inventory = inventory,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi thêm items vào inventory: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/equipment/equip
        /// Body: { "inventorySlotIndex": 0 }
        /// Trang bị item từ inventory vào equipment slot tương ứng
        /// </summary>
        [HttpPost("{playerId}/equipment/equip")]
        [AllowAnonymous]
        public async Task<IActionResult> EquipItem(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("inventorySlotIndex", out var slotIndexProp))
                {
                    return BadRequest("Thiếu field 'inventorySlotIndex'.");
                }

                int inventorySlotIndex = slotIndexProp.GetInt32();

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                {
                    return NotFound("Player không tồn tại.");
                }

                // Parse inventory hiện tại
                var inventory = new List<Dictionary<string, object>>();
                if (!string.IsNullOrEmpty(player.InventoryJson) && player.InventoryJson != "[]")
                {
                    var existingInventory = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(player.InventoryJson);
                    if (existingInventory != null)
                    {
                        foreach (var item in existingInventory)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (var kvp in item)
                            {
                                dict[kvp.Key] = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : kvp.Value.GetDouble(),
                                    JsonValueKind.String => kvp.Value.GetString(),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    _ => kvp.Value.ToString()
                                };
                            }
                            inventory.Add(dict);
                        }
                    }
                }

                // Tìm item ở inventorySlotIndex
                var targetItem = inventory.FirstOrDefault(s =>
                    s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == inventorySlotIndex
                    && s.ContainsKey("quantity") && Convert.ToInt32(s["quantity"]) > 0);

                if (targetItem == null)
                {
                    return BadRequest($"Không tìm thấy item ở slot {inventorySlotIndex}.");
                }

                int itemTemplateId = targetItem.ContainsKey("itemTemplateId") ? Convert.ToInt32(targetItem["itemTemplateId"]) : 0;
                string itemCode = targetItem.ContainsKey("itemCode") ? targetItem["itemCode"]?.ToString() ?? "" : "";
                string iconId = targetItem.ContainsKey("iconId") ? targetItem["iconId"]?.ToString() ?? "" : "";

                // Lấy item template để xác định loại slot
                var itemTemplate = await _db.Set<ItemTemplate>().FirstOrDefaultAsync(t => t.Id == itemTemplateId);
                if (itemTemplate == null)
                {
                    return BadRequest($"Item template ID {itemTemplateId} không tồn tại.");
                }

                // Chỉ equipment (category=1) mới trang bị được
                if (itemTemplate.Category != 1)
                {
                    return BadRequest("Item này không phải trang bị (category != 1).");
                }

                // Xác định equipment slot dựa trên item_type
                string equipSlotName = itemTemplate.ItemType switch
                {
                    1 => "weapon",      // Sword / Melee
                    2 => "weapon",      // Bow / Ranged
                    3 => "armor",       // Armor
                    4 => "helmet",      // Helmet
                    5 => "pants",       // Pants
                    6 => "boots",       // Boots
                    7 => "accessory",   // Ring/Necklace
                    _ => null
                };

                if (equipSlotName == null)
                {
                    return BadRequest($"Không xác định được slot trang bị cho item_type={itemTemplate.ItemType}.");
                }

                // Parse equipment JSON hiện tại
                var equipment = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(player.EquipmentJson) && player.EquipmentJson != "{}")
                {
                    var existingEquip = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(player.EquipmentJson);
                    if (existingEquip != null)
                    {
                        foreach (var kvp in existingEquip)
                        {
                            if (kvp.Value.ValueKind == JsonValueKind.Null)
                            {
                                equipment[kvp.Key] = null;
                            }
                            else if (kvp.Value.ValueKind == JsonValueKind.Object)
                            {
                                var itemDict = new Dictionary<string, object>();
                                foreach (var prop in kvp.Value.EnumerateObject())
                                {
                                    itemDict[prop.Name] = prop.Value.ValueKind switch
                                    {
                                        JsonValueKind.Number => prop.Value.TryGetInt32(out var iv) ? iv : prop.Value.GetDouble(),
                                        JsonValueKind.String => prop.Value.GetString(),
                                        _ => prop.Value.ToString()
                                    };
                                }
                                equipment[kvp.Key] = itemDict;
                            }
                        }
                    }
                }

                // Nếu slot đã có item → tháo ra trước (đưa lại vào inventory)
                if (equipment.ContainsKey(equipSlotName) && equipment[equipSlotName] != null)
                {
                    var oldEquipItem = equipment[equipSlotName] as Dictionary<string, object>;
                    if (oldEquipItem != null)
                    {
                        // Tìm slot trống trong inventory để đưa item cũ vào
                        int emptySlot = -1;
                        int maxSlots = 20;
                        for (int i = 0; i < maxSlots; i++)
                        {
                            var existingSlot = inventory.FirstOrDefault(s =>
                                s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == i
                                && s.ContainsKey("quantity") && Convert.ToInt32(s["quantity"]) > 0);
                            if (existingSlot == null)
                            {
                                emptySlot = i;
                                break;
                            }
                        }

                        if (emptySlot == -1)
                        {
                            return BadRequest("Inventory đầy! Không thể tháo trang bị cũ.");
                        }

                        // Đưa item cũ vào inventory
                        var returnedItem = new Dictionary<string, object>
                        {
                            ["slotIndex"] = emptySlot,
                            ["itemTemplateId"] = oldEquipItem.ContainsKey("itemTemplateId") ? oldEquipItem["itemTemplateId"] : 0,
                            ["itemCode"] = oldEquipItem.ContainsKey("itemCode") ? oldEquipItem["itemCode"] : "",
                            ["iconId"] = oldEquipItem.ContainsKey("iconId") ? oldEquipItem["iconId"] : "",
                            ["quantity"] = 1,
                            ["isEquipped"] = false
                        };
                        inventory.RemoveAll(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == emptySlot);
                        inventory.Add(returnedItem);
                    }
                }

                // Xoá item khỏi inventory slot cũ
                inventory.RemoveAll(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == inventorySlotIndex);

                // Tạo equipment item data
                var equipItemData = new Dictionary<string, object>
                {
                    ["itemTemplateId"] = itemTemplateId,
                    ["itemCode"] = itemCode,
                    ["iconId"] = iconId,
                    ["itemName"] = itemTemplate.Name,
                    ["itemType"] = itemTemplate.ItemType,
                    ["baseStatJson"] = itemTemplate.BaseStatJson ?? "{}"
                };

                // Gán vào equipment slot
                equipment[equipSlotName] = equipItemData;

                // Đảm bảo tất cả 6 slot đều tồn tại
                string[] allSlots = { "weapon", "helmet", "armor", "pants", "boots", "accessory" };
                foreach (var slot in allSlots)
                {
                    if (!equipment.ContainsKey(slot))
                    {
                        equipment[slot] = null;
                    }
                }

                // Serialize và lưu
                player.EquipmentJson = JsonSerializer.Serialize(equipment);
                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Đã trang bị {itemTemplate.Name} vào slot {equipSlotName}",
                    player_id = playerId,
                    equipment_slot = equipSlotName,
                    equipment = equipment,
                    inventory = inventory,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi trang bị item: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/equipment/unequip
        /// Body: { "equipmentSlot": "weapon" }
        /// Tháo trang bị từ equipment slot và đưa lại vào inventory
        /// </summary>
        [HttpPost("{playerId}/equipment/unequip")]
        [AllowAnonymous]
        public async Task<IActionResult> UnequipItem(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("equipmentSlot", out var slotProp))
                {
                    return BadRequest("Thiếu field 'equipmentSlot'.");
                }

                string equipSlotName = slotProp.GetString() ?? "";
                string[] validSlots = { "weapon", "helmet", "armor", "pants", "boots", "accessory" };
                if (!validSlots.Contains(equipSlotName))
                {
                    return BadRequest($"Slot '{equipSlotName}' không hợp lệ. Các slot hợp lệ: {string.Join(", ", validSlots)}");
                }

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                {
                    return NotFound("Player không tồn tại.");
                }

                // Parse equipment JSON
                var equipment = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(player.EquipmentJson) && player.EquipmentJson != "{}")
                {
                    var existingEquip = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(player.EquipmentJson);
                    if (existingEquip != null)
                    {
                        foreach (var kvp in existingEquip)
                        {
                            if (kvp.Value.ValueKind == JsonValueKind.Null)
                            {
                                equipment[kvp.Key] = null;
                            }
                            else if (kvp.Value.ValueKind == JsonValueKind.Object)
                            {
                                var itemDict = new Dictionary<string, object>();
                                foreach (var prop in kvp.Value.EnumerateObject())
                                {
                                    itemDict[prop.Name] = prop.Value.ValueKind switch
                                    {
                                        JsonValueKind.Number => prop.Value.TryGetInt32(out var iv) ? iv : prop.Value.GetDouble(),
                                        JsonValueKind.String => prop.Value.GetString(),
                                        _ => prop.Value.ToString()
                                    };
                                }
                                equipment[kvp.Key] = itemDict;
                            }
                        }
                    }
                }

                // Kiểm tra slot có item không
                if (!equipment.ContainsKey(equipSlotName) || equipment[equipSlotName] == null)
                {
                    return BadRequest($"Slot {equipSlotName} đang trống, không có gì để tháo.");
                }

                var equipItem = equipment[equipSlotName] as Dictionary<string, object>;
                if (equipItem == null)
                {
                    return BadRequest($"Dữ liệu trang bị ở slot {equipSlotName} không hợp lệ.");
                }

                // Parse inventory
                var inventory = new List<Dictionary<string, object>>();
                if (!string.IsNullOrEmpty(player.InventoryJson) && player.InventoryJson != "[]")
                {
                    var existingInventory = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(player.InventoryJson);
                    if (existingInventory != null)
                    {
                        foreach (var item in existingInventory)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (var kvp in item)
                            {
                                dict[kvp.Key] = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : kvp.Value.GetDouble(),
                                    JsonValueKind.String => kvp.Value.GetString(),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    _ => kvp.Value.ToString()
                                };
                            }
                            inventory.Add(dict);
                        }
                    }
                }

                // Tìm slot trống trong inventory
                int emptySlot = -1;
                int maxSlots = 20;
                for (int i = 0; i < maxSlots; i++)
                {
                    var existingSlot = inventory.FirstOrDefault(s =>
                        s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == i
                        && s.ContainsKey("quantity") && Convert.ToInt32(s["quantity"]) > 0);
                    if (existingSlot == null)
                    {
                        emptySlot = i;
                        break;
                    }
                }

                if (emptySlot == -1)
                {
                    return BadRequest("Inventory đầy! Không thể tháo trang bị.");
                }

                // Đưa item vào inventory
                var returnedItem = new Dictionary<string, object>
                {
                    ["slotIndex"] = emptySlot,
                    ["itemTemplateId"] = equipItem.ContainsKey("itemTemplateId") ? equipItem["itemTemplateId"] : 0,
                    ["itemCode"] = equipItem.ContainsKey("itemCode") ? equipItem["itemCode"] : "",
                    ["iconId"] = equipItem.ContainsKey("iconId") ? equipItem["iconId"] : "",
                    ["quantity"] = 1,
                    ["isEquipped"] = false
                };
                inventory.RemoveAll(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == emptySlot);
                inventory.Add(returnedItem);

                // Xoá item khỏi equipment slot
                equipment[equipSlotName] = null;

                // Serialize và lưu
                player.EquipmentJson = JsonSerializer.Serialize(equipment);
                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                string itemName = equipItem.ContainsKey("itemName") ? equipItem["itemName"]?.ToString() ?? "" : "";

                return Ok(new
                {
                    message = $"Đã tháo {itemName} khỏi slot {equipSlotName}",
                    player_id = playerId,
                    equipment_slot = equipSlotName,
                    equipment = equipment,
                    inventory = inventory,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi tháo trang bị: {ex.Message}");
            }
        }

        /// <summary>
        /// GET /api/player/{playerId}/equipment
        /// Lấy thông tin trang bị hiện tại của player
        /// </summary>
        [HttpGet("{playerId}/equipment")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEquipment(int playerId)
        {
            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null)
            {
                return NotFound("Player không tồn tại.");
            }

            object equipment;
            try
            {
                equipment = JsonSerializer.Deserialize<object>(player.EquipmentJson);
            }
            catch
            {
                equipment = new { };
            }

            return Ok(new
            {
                player_id = playerId,
                equipment = equipment
            });
        }
    }
}

