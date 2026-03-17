using System;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Services;
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
        /// Body: { "element_type": "Fire", "character_name": "TenNhanVat" }
        /// Gender được tự động suy ra từ element_type.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePlayer([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("element_type", out var elementProp))
            {
                return BadRequest("element_type là bắt buộc.");
            }

            var elementType = elementProp.GetString() ?? "Fire";

            // Validate element_type hợp lệ
            var validElements = new[] { "Metal", "Wood", "Water", "Fire", "Earth", "Wind" };
            if (!System.Array.Exists(validElements, e => e == elementType))
            {
                return BadRequest($"element_type không hợp lệ. Giá trị hợp lệ: {string.Join(", ", validElements)}");
            }

            // Tự động suy ra gender từ element_type (không cần client gửi lên)
            string gender = elementType switch
            {
                "Metal" => "Male",
                "Wood"  => "Female",
                "Water" => "Female",
                "Fire"  => "Male",
                "Earth" => "Male",
                "Wind"  => "Female",
                _       => "Male"
            };

            // Lấy character_name
            string characterName = "";
            if (body.TryGetProperty("character_name", out var nameProp))
            {
                characterName = nameProp.GetString() ?? "";
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
                return Conflict("Nhân vật đã được tạo cho tài khoản này.");
            }

            // Tạo player mới với try-catch để xử lý race condition
            var playerData = new PlayerData
            {
                PlayerId = userId,
                Gender = gender,
                CharacterName = characterName,
                UpdatedAt = DateTime.UtcNow
            };
            playerData.SetInfoChar(PlayerData.DefaultInfoChar(elementType));

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
                    var existingPlayerInfo = existingPlayer.GetInfoChar();
                    existingPlayerInfo.ElementType = elementType;
                    existingPlayer.SetInfoChar(existingPlayerInfo);
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
            var createInfo = playerData.GetInfoChar();
            var createFs = StatCalculator.Compute(createInfo, playerData.EquipmentJson, playerData.PotentialStatsJson);
            var createResponse = new
            {
                player_id = playerData.PlayerId,
                level = createInfo.Level,
                experience = createInfo.Experience,
                exp_required_for_next_level = 0,
                gold = createInfo.Gold,
                map_id = createInfo.MapId,
                position_x = createInfo.PositionX,
                position_y = createInfo.PositionY,
                base_stats = new
                {
                    hp = createInfo.Hp,
                    max_hp = createInfo.MaxHp,
                    mp = createInfo.Mp,
                    max_mp = createInfo.MaxMp,
                    attack = createInfo.Attack,
                    defense = createInfo.Defense
                },
                equipment = JsonSerializer.Deserialize<object>(playerData.EquipmentJson),
                potential_stats = JsonSerializer.Deserialize<object>(playerData.PotentialStatsJson),
                final_stats = new
                {
                    hp         = createFs.Hp,
                    max_hp     = createFs.MaxHp,
                    mp         = createFs.Mp,
                    max_mp     = createFs.MaxMp,
                    attack     = createFs.Attack,
                    defense    = createFs.Defense,
                    move_speed = createFs.MoveSpeed,
                },
                inventory = JsonSerializer.Deserialize<object>(playerData.InventoryJson),
                skills = JsonSerializer.Deserialize<object>(playerData.SkillsJson),
                skill_points_available = createInfo.SkillPoints,
                potential_points_available = createInfo.PotentialPoints,
                element_type = createInfo.ElementType,
                gene_tier = createInfo.GeneTier,
                gene_exp = createInfo.GeneExp,
                is_hybrid = createInfo.IsHybrid,
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

            // Compute final_stats = base (gene baked-in) + equipment bonus + potential bonus
            var info = player.GetInfoChar();

            // Xử lý level-up nếu có đủ EXP
            var (leveledUp, expAtCurrentLevel, expForNextLevel) = await ProcessLevelUpAsync(info);
            if (leveledUp)
            {
                player.SetInfoChar(info);
                player.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            var finalStats = StatCalculator.Compute(info, player.EquipmentJson, player.PotentialStatsJson);

            // ─── DEBUG LOG ────────────────────────────────────────────────────
            Console.WriteLine($"[PlayerCtrl] GetPlayerData playerId={playerId} level={info.Level} exp={info.Experience} expNextLv={expForNextLevel}");
            Console.WriteLine($"  InfoChar  → attack={info.Attack} maxHp={info.MaxHp} maxMp={info.MaxMp} defense={info.Defense}");
            // ──────────────────────────────────────────────────────────────────────────

            var response = new
            {
                player_id = player.PlayerId,
                level = info.Level,
                experience = info.Experience,
                exp_required_for_next_level = expForNextLevel,
                exp_at_current_level = expAtCurrentLevel,
                gold = info.Gold,
                silver = info.Silver,
                map_id = info.MapId,
                position_x = info.PositionX,
                position_y = info.PositionY,
                base_stats = new
                {
                    hp = info.Hp,
                    max_hp = info.MaxHp,
                    mp = info.Mp,
                    max_mp = info.MaxMp,
                    attack = info.Attack,
                    defense = info.Defense
                },
                equipment = JsonSerializer.Deserialize<object>(player.EquipmentJson),
                potential_stats = JsonSerializer.Deserialize<object>(player.PotentialStatsJson),
                final_stats = new
                {
                    hp         = finalStats.Hp,
                    max_hp     = finalStats.MaxHp,
                    mp         = finalStats.Mp,
                    max_mp     = finalStats.MaxMp,
                    attack     = finalStats.Attack,
                    defense    = finalStats.Defense,
                    move_speed = finalStats.MoveSpeed,
                },
                inventory = JsonSerializer.Deserialize<object>(player.InventoryJson),
                skills = JsonSerializer.Deserialize<object>(player.SkillsJson),
                skill_points_available = info.SkillPoints,
                potential_points_available = info.PotentialPoints,
                element_type = info.ElementType,
                gene_tier = info.GeneTier,
                gene_exp = info.GeneExp,
                is_hybrid = info.IsHybrid,
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

                var posInfo = player.GetInfoChar();
                posInfo.MapId = mapId;
                posInfo.PositionX = positionX;
                posInfo.PositionY = positionY;
                player.SetInfoChar(posInfo);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Position updated successfully",
                    map_id = posInfo.MapId,
                    position_x = posInfo.PositionX,
                    position_y = posInfo.PositionY
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
                var batchInfo = player.GetInfoChar();

                if (body.TryGetProperty("level", out var levelProp))
                    batchInfo.Level = levelProp.GetInt32();

                if (body.TryGetProperty("experience", out var expProp))
                    batchInfo.Experience = expProp.GetInt32();

                if (body.TryGetProperty("gold", out var goldProp))
                    batchInfo.Gold = goldProp.GetInt32();

                if (body.TryGetProperty("skill_points", out var spProp))
                    batchInfo.SkillPoints = spProp.GetInt32();

                if (body.TryGetProperty("potential_points", out var ppProp))
                    batchInfo.PotentialPoints = ppProp.GetInt32();

                if (body.TryGetProperty("hp", out var hpProp))
                    batchInfo.Hp = hpProp.GetInt32();

                if (body.TryGetProperty("max_hp", out var maxHpProp))
                    batchInfo.MaxHp = maxHpProp.GetInt32();

                if (body.TryGetProperty("mp", out var mpProp))
                    batchInfo.Mp = mpProp.GetInt32();

                if (body.TryGetProperty("max_mp", out var maxMpProp))
                    batchInfo.MaxMp = maxMpProp.GetInt32();

                if (body.TryGetProperty("attack", out var attackProp))
                    batchInfo.Attack = attackProp.GetInt32();

                if (body.TryGetProperty("defense", out var defenseProp))
                    batchInfo.Defense = defenseProp.GetInt32();

                if (body.TryGetProperty("gene_tier", out var gtProp))
                    batchInfo.GeneTier = gtProp.GetInt32();

                if (body.TryGetProperty("gene_exp", out var geProp))
                    batchInfo.GeneExp = geProp.GetInt32();

                if (body.TryGetProperty("element_type", out var etProp))
                    batchInfo.ElementType = etProp.GetString() ?? batchInfo.ElementType;

                if (body.TryGetProperty("map_id", out var mapIdProp))
                    batchInfo.MapId = mapIdProp.GetInt32();

                if (body.TryGetProperty("position_x", out var posXProp))
                    batchInfo.PositionX = (float)posXProp.GetDouble();

                if (body.TryGetProperty("position_y", out var posYProp))
                    batchInfo.PositionY = (float)posYProp.GetDouble();

                // Xử lý level-up server-side (safety net nếu client chưa xử lý)
                await ProcessLevelUpAsync(batchInfo);

                player.SetInfoChar(batchInfo);

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

                    // Đọc upgradeLevel và strOptions (tuỳ chọn)
                    int addUpgradeLevel = 0;
                    if (itemToAdd.TryGetValue("upgradeLevel", out var lvlElem))
                        addUpgradeLevel = lvlElem.TryGetInt32(out var lv) ? lv : 0;

                    string addStrOptions = "";
                    if (itemToAdd.TryGetValue("strOptions", out var strOptElem))
                        addStrOptions = strOptElem.GetString() ?? "";
                    if (string.IsNullOrEmpty(addStrOptions))
                        addStrOptions = GetDefaultStrOptions(itemTemplateId);

                    // Thêm item vào slot trống
                    var newSlot = new Dictionary<string, object>
                    {
                        ["slotIndex"]      = emptySlotIndex,
                        ["itemTemplateId"] = itemTemplateId,
                        ["itemCode"]       = itemCode,
                        ["iconId"]         = iconId,
                        ["quantity"]       = quantity,
                        ["isEquipped"]     = false,
                        ["upgradeLevel"]   = addUpgradeLevel,
                        ["strOptions"]     = addStrOptions
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
        /// POST /api/player/{playerId}/inventory/clear
        /// Xóa toàn bộ inventory và equipment của player (dùng cho debug/reset)
        /// </summary>
        [HttpPost("{playerId}/inventory/clear")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearInventory(int playerId)
        {
            try
            {
                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                    return NotFound($"Player với ID {playerId} không tồn tại.");

                player.InventoryJson = "[]";
                player.EquipmentJson = "{}";
                player.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new { message = "Đã xóa toàn bộ inventory và equipment.", player_id = playerId });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi clear inventory: {ex.Message}");
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
                int itemUpgradeLevel = targetItem.ContainsKey("upgradeLevel") ? Convert.ToInt32(targetItem["upgradeLevel"]) : 0;
                string itemStrOptions = targetItem.ContainsKey("strOptions") ? targetItem["strOptions"]?.ToString() ?? "" : "";
                if (string.IsNullOrEmpty(itemStrOptions))
                    itemStrOptions = GetDefaultStrOptions(itemTemplateId);

                // Lấy item template để xác định loại slot
                var itemTemplate = await _db.Set<ItemTemplate>().FirstOrDefaultAsync(t => t.Id == itemTemplateId);
                if (itemTemplate == null)
                {
                    return BadRequest($"Item template ID {itemTemplateId} không tồn tại.");
                }

                // Chỉ equipment (type 0-5) mới trang bị được
                if (itemTemplate.Type < 0 || itemTemplate.Type > 5)
                {
                    return BadRequest($"Item này không phải trang bị (type={itemTemplate.Type}).");
                }

                // Xác định equipment slot dựa trên type (DB v3.0)
                string equipSlotName = itemTemplate.Type switch
                {
                    0 => "helmet",
                    1 => "weapon",
                    2 => "armor",
                    3 => "pants",
                    4 => "boots",
                    5 => "accessory",
                    _ => null
                };

                if (equipSlotName == null)
                {
                    return BadRequest($"Không xác định được slot trang bị cho type={itemTemplate.Type}.");
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
                            ["slotIndex"]      = emptySlot,
                            ["itemTemplateId"] = oldEquipItem.ContainsKey("itemTemplateId") ? oldEquipItem["itemTemplateId"] : 0,
                            ["itemCode"]       = oldEquipItem.ContainsKey("itemCode")       ? oldEquipItem["itemCode"]       : "",
                            ["iconId"]         = oldEquipItem.ContainsKey("iconId")         ? oldEquipItem["iconId"]         : "",
                            ["quantity"]       = 1,
                            ["isEquipped"]     = false,
                            ["upgradeLevel"]   = oldEquipItem.ContainsKey("upgradeLevel")   ? oldEquipItem["upgradeLevel"]   : 0,
                            ["strOptions"]     = oldEquipItem.ContainsKey("strOptions")     ? oldEquipItem["strOptions"] ?? "" : ""
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
                    ["itemCode"]       = itemCode,
                    ["iconId"]         = itemTemplate.IdIcon.ToString(),
                    ["itemName"]       = itemTemplate.Name,
                    ["itemType"]       = itemTemplate.Type,
                    ["upgradeLevel"]   = itemUpgradeLevel,
                    ["strOptions"]     = itemStrOptions
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
                    ["slotIndex"]      = emptySlot,
                    ["itemTemplateId"] = equipItem.ContainsKey("itemTemplateId") ? equipItem["itemTemplateId"] : 0,
                    ["itemCode"]       = equipItem.ContainsKey("itemCode")       ? equipItem["itemCode"]       : "",
                    ["iconId"]         = equipItem.ContainsKey("iconId")         ? equipItem["iconId"]         : "",
                    ["quantity"]       = 1,
                    ["isEquipped"]     = false,
                    ["upgradeLevel"]   = equipItem.ContainsKey("upgradeLevel")   ? equipItem["upgradeLevel"]   : 0,
                    ["strOptions"]     = equipItem.ContainsKey("strOptions")     ? equipItem["strOptions"] ?? "" : ""
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

        // ──────────────────────────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// <summary>
        /// Xử lý level-up: tự động tăng level khi exp đủ mốc, cộng base stats + thưởng điểm.
        /// </summary>
        /// <returns>(changed, expAtCurrentLevel, expForNextLevel) — 
        ///   expAtCurrentLevel: cumulative EXP ngưỡng của level hiện tại;
        ///   expForNextLevel:   cumulative EXP ngưỡng của level tiếp theo (0 nếu đã max).</returns>
        private async Task<(bool changed, int expAtCurrentLevel, int expForNextLevel)> ProcessLevelUpAsync(InfoChar info)
        {
            bool changed = false;

            var configs = await _db.ExpRequirements
                .Where(e => e.Level >= info.Level)
                .OrderBy(e => e.Level)
                .ToListAsync();

            if (configs.Count == 0) return (false, 0, 0);

            int expAtCurrentLevel = configs.FirstOrDefault(c => c.Level == info.Level)?.ExpRequired ?? 0;
            var higherConfigs = configs.Where(c => c.Level > info.Level).ToList();

            while (higherConfigs.Count > 0 && info.Experience >= higherConfigs[0].ExpRequired)
            {
                var cfg = higherConfigs[0];
                higherConfigs.RemoveAt(0);

                info.Level           = cfg.Level;
                info.SkillPoints    += cfg.SkillPoints;
                info.PotentialPoints += cfg.PotentialPoints;
                expAtCurrentLevel    = cfg.ExpRequired;

                if (!string.IsNullOrWhiteSpace(cfg.BaseStatIncreaseJson) && cfg.BaseStatIncreaseJson != "{}")
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(cfg.BaseStatIncreaseJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("hp",      out var v1)) { info.MaxHp   += v1.GetInt32(); info.Hp = Math.Min(info.Hp + v1.GetInt32(), info.MaxHp); }
                        if (root.TryGetProperty("mp",      out var v2)) { info.MaxMp   += v2.GetInt32(); info.Mp = Math.Min(info.Mp + v2.GetInt32(), info.MaxMp); }
                        if (root.TryGetProperty("attack",  out var v3))   info.Attack  += v3.GetInt32();
                        if (root.TryGetProperty("defense", out var v4))   info.Defense += v4.GetInt32();
                    }
                    catch { }
                }

                Console.WriteLine($"[LevelUp] Player leveled up to {info.Level}! SkillPts={info.SkillPoints} PotPts={info.PotentialPoints}");
                changed = true;
            }

            int expForNextLevel = higherConfigs.Count > 0 ? higherConfigs[0].ExpRequired : 0;
            return (changed, expAtCurrentLevel, expForNextLevel);
        }

        /// strOptions mặc định ở bậc +0 cho item template.
        /// Format: "optId,value;..." (value = strOption[0] của option template)
        /// </summary>
        private static string GetDefaultStrOptions(int itemTemplateId) =>
            UpgradeController.DefaultStrOptions.TryGetValue(itemTemplateId, out var val) ? val : "";

        // ================================================================
        //  SKILL ENDPOINTS
        // ================================================================

        /// <summary>
        /// GET /api/player/{playerId}/skills
        /// Trả về tất cả skills từ skill_template kèm level hiện tại của player.
        /// </summary>
        [HttpGet("{playerId}/skills")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlayerSkills(int playerId)
        {
            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            // Parse player skills JSON → Dictionary<skill_id, current_level>
            var playerSkillLevels = new Dictionary<int, int>();
            if (!string.IsNullOrEmpty(player.SkillsJson) && player.SkillsJson != "[]")
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<List<JsonElement>>(player.SkillsJson);
                    if (arr != null)
                        foreach (var elem in arr)
                            if (elem.TryGetProperty("skill_id", out var idP) &&
                                elem.TryGetProperty("current_level", out var lvP))
                                playerSkillLevels[idP.GetInt32()] = lvP.GetInt32();
                }
                catch { /* ignore parse errors */ }
            }

            // Chỉ lấy skill của đúng hệ player hoặc universal (element_type IS NULL)
            var templates = await _db.SkillTemplates
                .Where(s => s.ElementType == null || s.ElementType == info.ElementType)
                .OrderBy(s => s.ElementType).ThenBy(s => s.SkillId)
                .ToListAsync();

            var skillList = templates.Select(t =>
            {
                int curLevel = playerSkillLevels.TryGetValue(t.SkillId, out var lvl) ? lvl : 0;
                int nextLevelPlayerReq = 0;
                int nextSpCost = 1;
                float nextEffectValue = 0;
                string nextDesc = "";
                bool canUpgrade = false;

                // Current-level runtime stats (cooldown, damage, mp) — client dùng để apply vào SkillData
                float currentCooldownSec = 3f;
                float currentEffectValue = 0f;
                int   currentMpCost      = 0;

                if (!string.IsNullOrEmpty(t.LevelsJson))
                {
                    try
                    {
                        var levels = JsonSerializer.Deserialize<List<JsonElement>>(t.LevelsJson);
                        if (levels != null)
                        {
                            // Lấy stats của level hiện tại (index = curLevel-1, min index 0)
                            int curIdx = curLevel > 0 ? curLevel - 1 : 0;
                            if (curIdx < levels.Count)
                            {
                                var cur = levels[curIdx];
                                if (cur.TryGetProperty("cooldown_sec",  out var cs)) currentCooldownSec = (float)cs.GetDouble();
                                if (cur.TryGetProperty("effect_value",  out var cv)) currentEffectValue = (float)cv.GetDouble();
                                if (cur.TryGetProperty("mp_cost",       out var cm)) currentMpCost      = cm.GetInt32();
                            }

                            // Lấy stats của level tiếp theo (để nâng cấp)
                            if (curLevel < t.MaxLevel && curLevel < levels.Count)
                            {
                                var nextData = levels[curLevel];
                                if (nextData.TryGetProperty("level_req",    out var lr)) nextLevelPlayerReq = lr.GetInt32();
                                if (nextData.TryGetProperty("sp_cost",      out var sc)) nextSpCost         = sc.GetInt32();
                                if (nextData.TryGetProperty("effect_value", out var ev)) nextEffectValue    = (float)ev.GetDouble();
                                if (nextData.TryGetProperty("desc",         out var dc)) nextDesc           = dc.GetString() ?? "";
                                canUpgrade = info.Level >= nextLevelPlayerReq
                                          && info.SkillPoints >= nextSpCost
                                          && info.GeneTier >= t.GeneTierRequired;
                            }
                        }
                    }
                    catch { }
                }

                return new
                {
                    skill_id              = t.SkillId,
                    skill_code            = t.SkillCode,
                    skill_name            = t.SkillName,
                    description           = t.Description,
                    element_type          = t.ElementType,
                    max_level             = t.MaxLevel,
                    level_to_unlock       = t.LevelToUnlock,
                    gene_tier_required    = t.GeneTierRequired,
                    current_level         = curLevel,
                    // Runtime stats cho client apply vào SkillData
                    current_cooldown_sec  = currentCooldownSec,
                    current_effect_value  = currentEffectValue,
                    current_mp_cost       = currentMpCost,
                    can_upgrade           = canUpgrade && curLevel < t.MaxLevel,
                    next_level_player_req = nextLevelPlayerReq,
                    next_level_sp_cost    = nextSpCost,
                    next_level_desc       = nextDesc,
                    icon_id               = t.IconId
                };
            }).ToList();

            return Ok(new
            {
                skill_points_available = info.SkillPoints,
                player_level           = info.Level,
                skills                 = skillList
            });
        }

        /// <summary>
        /// POST /api/player/{playerId}/skills/upgrade
        /// Body: { "skill_id": 1 }
        /// Nâng cấp skill lên 1 level (trừ skill_points).
        /// </summary>
        [HttpPost("{playerId}/skills/upgrade")]
        [AllowAnonymous]
        public async Task<IActionResult> UpgradeSkill(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("skill_id", out var skillIdProp))
                    return BadRequest("Thiếu field 'skill_id'.");

                int skillId = skillIdProp.GetInt32();

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                var template = await _db.SkillTemplates.FindAsync(skillId);
                if (template == null) return BadRequest($"Skill ID {skillId} không tồn tại.");

                var info = player.GetInfoChar();

                // Parse player's skills list
                var playerSkills = new List<Dictionary<string, object>>();
                if (!string.IsNullOrEmpty(player.SkillsJson) && player.SkillsJson != "[]")
                {
                    try
                    {
                        var rawArr = JsonSerializer.Deserialize<List<JsonElement>>(player.SkillsJson);
                        if (rawArr != null)
                            foreach (var elem in rawArr)
                            {
                                var d = new Dictionary<string, object>();
                                foreach (var prop in elem.EnumerateObject())
                                    d[prop.Name] = prop.Value.ValueKind switch
                                    {
                                        JsonValueKind.Number => prop.Value.TryGetInt32(out var iv) ? iv : (object)prop.Value.GetDouble(),
                                        JsonValueKind.String => prop.Value.GetString()!,
                                        JsonValueKind.True   => true,
                                        JsonValueKind.False  => false,
                                        _ => prop.Value.ToString()
                                    };
                                playerSkills.Add(d);
                            }
                    }
                    catch { }
                }

                // Find current level of this skill
                var existing = playerSkills.FirstOrDefault(s =>
                    s.ContainsKey("skill_id") && Convert.ToInt32(s["skill_id"]) == skillId);
                int curLevel = existing != null ? Convert.ToInt32(existing["current_level"]) : 0;

                if (curLevel >= template.MaxLevel)
                    return BadRequest($"{template.SkillName} đã đạt level tối đa ({template.MaxLevel}).");

                // Parse upgrade requirements for next level
                int nextLevelPlayerReq = 0;
                int spCost = 1;
                if (!string.IsNullOrEmpty(template.LevelsJson))
                {
                    try
                    {
                        var levels = JsonSerializer.Deserialize<List<JsonElement>>(template.LevelsJson);
                        if (levels != null && curLevel < levels.Count)
                        {
                            if (levels[curLevel].TryGetProperty("level_req", out var lr)) nextLevelPlayerReq = lr.GetInt32();
                            if (levels[curLevel].TryGetProperty("sp_cost",   out var sc)) spCost             = sc.GetInt32();
                        }
                    }
                    catch { }
                }

                if (info.Level < nextLevelPlayerReq)
                    return BadRequest($"Cần level nhân vật {nextLevelPlayerReq} để nâng {template.SkillName}. Hiện tại: {info.Level}.");

                if (info.SkillPoints < spCost)
                    return BadRequest($"Không đủ skill points. Cần {spCost}, có {info.SkillPoints}.");

                // Apply upgrade
                int newLevel = curLevel + 1;
                if (existing != null)
                    existing["current_level"] = newLevel;
                else
                    playerSkills.Add(new Dictionary<string, object> { ["skill_id"] = skillId, ["current_level"] = newLevel });

                info.SkillPoints -= spCost;
                player.SetInfoChar(info);
                player.SkillsJson = JsonSerializer.Serialize(playerSkills);
                player.UpdatedAt  = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message                = $"Đã nâng {template.SkillName} lên Lv.{newLevel}",
                    skill_id               = skillId,
                    skill_name             = template.SkillName,
                    new_level              = newLevel,
                    max_level              = template.MaxLevel,
                    skill_points_remaining = info.SkillPoints
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi nâng cấp skill: {ex.Message}");
            }
        }

        // ================================================================
        //  POTENTIAL ENDPOINTS
        // ================================================================

        // Giá trị mỗi điểm tiềm năng (hardcoded vì potential_stats_config đã archived)
        private static readonly Dictionary<string, (string DisplayName, float ValuePerPoint)> PotentialStatConfig = new()
        {
            ["attack"]  = ("Tấn Công",   5f),
            ["hp"]      = ("Máu (HP)",  50f),
            ["mp"]      = ("Mana (MP)", 30f),
            ["defense"] = ("Phòng Thủ",  3f),
            ["gene"]    = ("Gene",        1f)
        };

        /// <summary>
        /// GET /api/player/{playerId}/potential
        /// Trả về toàn bộ chỉ số tiềm năng và điểm tiềm năng còn lại.
        /// </summary>
        [HttpGet("{playerId}/potential")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlayerPotential(int playerId)
        {
            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            // Parse potential_stats JSON
            var potentialPoints = new Dictionary<string, int>
                { ["attack"] = 0, ["hp"] = 0, ["mp"] = 0, ["defense"] = 0, ["gene"] = 0 };

            if (!string.IsNullOrEmpty(player.PotentialStatsJson) && player.PotentialStatsJson != "{}")
            {
                try
                {
                    var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(player.PotentialStatsJson);
                    if (raw != null)
                        foreach (var kvp in raw)
                            if (potentialPoints.ContainsKey(kvp.Key))
                                potentialPoints[kvp.Key] = kvp.Value.TryGetInt32(out var v) ? v : 0;
                }
                catch { }
            }

            var stats = PotentialStatConfig.Select(cfg => new
            {
                stat_name       = cfg.Key,
                display_name    = cfg.Value.DisplayName,
                current_points  = potentialPoints.TryGetValue(cfg.Key, out var pts) ? pts : 0,
                value_per_point = cfg.Value.ValuePerPoint,
                total_value     = (potentialPoints.TryGetValue(cfg.Key, out var pts2) ? pts2 : 0) * cfg.Value.ValuePerPoint
            }).ToList();

            return Ok(new
            {
                potential_points_available = info.PotentialPoints,
                player_level               = info.Level,
                stats
            });
        }

        /// <summary>
        /// POST /api/player/{playerId}/potential/upgrade
        /// Body: { "stat_name": "attack" }
        /// Đầu tư 1 điểm tiềm năng vào chỉ số được chọn.
        /// </summary>
        [HttpPost("{playerId}/potential/upgrade")]
        [AllowAnonymous]
        public async Task<IActionResult> UpgradePotential(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("stat_name", out var statProp))
                    return BadRequest("Thiếu field 'stat_name'.");

                string statName = statProp.GetString() ?? "";
                if (!PotentialStatConfig.ContainsKey(statName))
                    return BadRequest($"Chỉ số '{statName}' không hợp lệ. Hợp lệ: {string.Join(", ", PotentialStatConfig.Keys)}");

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                var info = player.GetInfoChar();

                if (info.PotentialPoints <= 0)
                    return BadRequest("Không còn điểm tiềm năng để phân bổ.");

                // Parse potential_stats
                var potentialPoints = new Dictionary<string, int>
                    { ["attack"] = 0, ["hp"] = 0, ["mp"] = 0, ["defense"] = 0, ["gene"] = 0 };

                if (!string.IsNullOrEmpty(player.PotentialStatsJson) && player.PotentialStatsJson != "{}")
                {
                    try
                    {
                        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(player.PotentialStatsJson);
                        if (raw != null)
                            foreach (var kvp in raw)
                                if (potentialPoints.ContainsKey(kvp.Key))
                                    potentialPoints[kvp.Key] = kvp.Value.TryGetInt32(out var v) ? v : 0;
                    }
                    catch { }
                }

                // Add 1 point to chosen stat
                potentialPoints[statName] = (potentialPoints.TryGetValue(statName, out var cur) ? cur : 0) + 1;
                info.PotentialPoints--;

                player.SetInfoChar(info);
                player.PotentialStatsJson = JsonSerializer.Serialize(potentialPoints);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var cfg = PotentialStatConfig[statName];
                int newPoints = potentialPoints[statName];

                return Ok(new
                {
                    message                    = $"Đã tăng {cfg.DisplayName} lên {newPoints} điểm (+{cfg.ValuePerPoint * newPoints} tổng)",
                    stat_name                  = statName,
                    display_name               = cfg.DisplayName,
                    new_points                 = newPoints,
                    value_per_point            = cfg.ValuePerPoint,
                    total_value                = newPoints * cfg.ValuePerPoint,
                    potential_points_remaining = info.PotentialPoints
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi nâng chỉ số tiềm năng: {ex.Message}");
            }
        }
    }
}

