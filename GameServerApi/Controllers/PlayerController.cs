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
        private const int StartMapId = 0;
        private const int StartZoneId = 0;
        private const float StartPositionX = 0f;
        private const float StartPositionY = 0f;
        private const int DefaultBagSlots = 20;
        private const int BagExpandBy = 5;
        private const int MaxEquippedBagQuickSlots = 3;

        private readonly GameDbContext _db;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(GameDbContext db, ILogger<PlayerController> logger)
        {
            _db = db;
            _logger = logger;
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

            // Áp dụng HpBuff / MpBuff đang active lên finalStats
            var activeBuffsForStats = player.GetActiveBuffs()
                .Where(b => b.ExpireAt == null || b.ExpireAt > DateTime.UtcNow);
            foreach (var buff in activeBuffsForStats)
            {
                if (buff.EffectType == "HpBuff")
                    finalStats.MaxHp = (int)(finalStats.MaxHp * (1.0 + buff.Value / 100.0));
                else if (buff.EffectType == "MpBuff")
                    finalStats.MaxMp = (int)(finalStats.MaxMp * (1.0 + buff.Value / 100.0));
            }
            finalStats.Hp = Math.Min(finalStats.Hp, finalStats.MaxHp);
            finalStats.Mp = Math.Min(finalStats.Mp, finalStats.MaxMp);

            // ─── DEBUG LOG ────────────────────────────────────────────────────
            _logger.LogDebug("[PlayerCtrl] GetPlayerData playerId={PlayerId} level={Level} exp={Exp} expNextLv={ExpNextLv}",
                playerId, info.Level, info.Experience, expForNextLevel);
            _logger.LogDebug("[PlayerCtrl] InfoChar attack={Attack} maxHp={MaxHp} maxMp={MaxMp} defense={Defense}",
                info.Attack, info.MaxHp, info.MaxMp, info.Defense);
            // ──────────────────────────────────────────────────────────────────────────

            var response = new
            {
                player_id = player.PlayerId,
                user_id   = player.PlayerId,   // alias: player_id == user_id (FK)
                level = info.Level,
                experience = info.Experience,
                exp_required_for_next_level = expForNextLevel,
                exp_at_current_level = expAtCurrentLevel,
                gold = info.Gold,
                silver = info.Silver,
                map_id = info.MapId,
                zone_id = info.ZoneId,
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
                character_name = player.CharacterName,
                bag_slots = info.BagSlots,
                bag_equipped_items = BuildBagEquippedItemsResponse(info.BagEquippedItems),
                // ── Hybrid Gene fields ──────────────────────────────
                secondary_element      = info.SecondaryElement,
                secondary_gene_tier    = info.SecondaryGeneTier,
                secondary_gene_exp     = info.SecondaryGeneExp,
                hybrid_id              = info.HybridId ?? 0,
                hybrid_element_a       = info.HybridElementA,
                hybrid_element_b       = info.HybridElementB,
                hybrid_bonus_targets   = info.HybridBonusTargets,
                hybrid_immune_elements = info.HybridImmuneElements,
                hybrid_atk_bonus_pct   = info.HybridAtkBonusPct,
                hybrid_prefab_path     = info.HybridPrefabPath
            };

            return Ok(response);
        }

        /// <summary>
        /// PUT /api/player/{playerId}/position
        /// Update position của player (khi out game hoặc disconnect).
        /// Chấp nhận cả player JWT (Bearer) và game server X-Zone-Api-Key.
        /// </summary>
        [HttpPut("{playerId}/position")]
        public async Task<IActionResult> UpdatePlayerPosition(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                // Game server dùng X-Zone-Api-Key → role "GameServer", dùng playerId từ URL.
                // Player dùng JWT Bearer → lấy user_id từ claim để đảm bảo chỉ sửa của mình.
                int targetPlayerId;
                if (User.IsInRole("GameServer"))
                {
                    targetPlayerId = playerId;
                }
                else
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
                    if (userIdClaim == null) return Unauthorized();
                    targetPlayerId = int.Parse(userIdClaim.Value);
                }

                bool resetToStartMap = body.TryGetProperty("reset_to_start_map", out var resetProp)
                    && resetProp.ValueKind == JsonValueKind.True;

                int mapId = StartMapId;
                int zoneId = StartZoneId;
                float positionX = StartPositionX;
                float positionY = StartPositionY;

                if (!resetToStartMap)
                {
                    if (!body.TryGetProperty("map_id", out var mapProp) || mapProp.ValueKind != JsonValueKind.Number)
                        return BadRequest("Thiếu map_id hợp lệ.");

                    if (!body.TryGetProperty("position_x", out var posXProp) || posXProp.ValueKind != JsonValueKind.Number)
                        return BadRequest("Thiếu position_x hợp lệ.");

                    if (!body.TryGetProperty("position_y", out var posYProp) || posYProp.ValueKind != JsonValueKind.Number)
                        return BadRequest("Thiếu position_y hợp lệ.");

                    mapId = mapProp.GetInt32();
                    positionX = (float)posXProp.GetDouble();
                    positionY = (float)posYProp.GetDouble();
                    zoneId = body.TryGetProperty("zone_id", out var zp) ? zp.GetInt32() : StartZoneId;
                }

                var player = await _db.PlayerData.FindAsync(targetPlayerId);
                if (player == null)
                {
                    return NotFound("Player không tồn tại.");
                }

                var posInfo = player.GetInfoChar();
                posInfo.MapId = mapId;
                posInfo.ZoneId = zoneId;
                posInfo.PositionX = positionX;
                posInfo.PositionY = positionY;
                player.SetInfoChar(posInfo);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = resetToStartMap ? "Player returned to start map successfully" : "Position updated successfully",
                    map_id = posInfo.MapId,
                    zone_id = posInfo.ZoneId,
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
        public async Task<IActionResult> UpdatePlayerData(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
                if (userIdClaim == null) return Unauthorized();
                int targetPlayerId = int.Parse(userIdClaim.Value);

                var player = await _db.PlayerData.FindAsync(targetPlayerId);
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
                // Lấy user_id từ JWT (authoritative – không tin vào URL param để chống giả mạo)
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
                if (userIdClaim == null)
                {
                    return Unauthorized();
                }

                var userId = int.Parse(userIdClaim.Value);

                // Dùng userId từ JWT làm player ID thực sự.
                // URL playerId chỉ mang tính routing; nếu khác nhau → vẫn được phép
                // miễn là token hợp lệ (game server tự gọi cho client đúng).
                int targetPlayerId = userId;

                var player = await _db.PlayerData.FindAsync(targetPlayerId);
                if (player == null)
                {
                    return NotFound($"Player với ID {targetPlayerId} không tồn tại.");
                }

                // Parse inventory hiện tại
                int maxSlots = ResolveBagSlotLimit(player.GetInfoChar());

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

                // NORMALIZE: Gán slotIndex cho các item cũ không có slotIndex
                // (tránh tình trạng format cũ + mới trộn lẫn gây lỗi load)
                {
                    int maxSlotsNorm = maxSlots;
                    int autoSlot = 0;
                    foreach (var item in inventory)
                    {
                        if (!item.ContainsKey("slotIndex"))
                        {
                            while (autoSlot < maxSlotsNorm &&
                                   inventory.Any(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == autoSlot))
                                autoSlot++;
                            if (autoSlot < maxSlotsNorm)
                            {
                                item["slotIndex"] = autoSlot;
                                autoSlot++;
                            }
                        }
                        // Xóa các field dư thừa cũ
                        item.Remove("iconId");
                        item.Remove("isEquipped");
                        item.Remove("itemCode");
                        // Đảm bảo strOptions có mặt
                        if (!item.ContainsKey("strOptions"))
                            item["strOptions"] = "";
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

                int addedCount = 0;

                foreach (var itemToAdd in itemsToAdd)
                {
                    if (!itemToAdd.TryGetValue("itemTemplateId", out var templateIdElem) ||
                        !itemToAdd.TryGetValue("quantity", out var qtyElem))
                    {
                        continue; // Skip: thiếu field bắt buộc
                    }

                    int itemTemplateId = templateIdElem.GetInt32();
                    int quantity = qtyElem.GetInt32();
                    if (itemTemplateId <= 0 || quantity <= 0) continue;

                    // Đọc upgradeLevel và strOptions (tuỳ chọn)
                    int addUpgradeLevel = 0;
                    if (itemToAdd.TryGetValue("upgradeLevel", out var lvlElem))
                        addUpgradeLevel = lvlElem.TryGetInt32(out var lv) ? lv : 0;

                    string addStrOptions = "";
                    if (itemToAdd.TryGetValue("strOptions", out var strOptElem))
                        addStrOptions = strOptElem.GetString() ?? "";
                    if (string.IsNullOrEmpty(addStrOptions))
                        addStrOptions = GetDefaultStrOptions(itemTemplateId);

                    // Kiểm tra isXepChong từ item_template
                    var itemTemplate = await _db.ItemTemplates.FindAsync(itemTemplateId);
                    bool isStackable = itemTemplate != null &&
                        string.Equals(itemTemplate.IsXepChong, "True", StringComparison.OrdinalIgnoreCase);

                    // Nếu stackable: thử gộp vào slot đã có
                    if (isStackable && addUpgradeLevel == 0)
                    {
                        var existingSlot = inventory.FirstOrDefault(s =>
                            s.ContainsKey("itemTemplateId") &&
                            Convert.ToInt32(s["itemTemplateId"]) == itemTemplateId);

                        if (existingSlot != null)
                        {
                            int currentQty = existingSlot.ContainsKey("quantity")
                                ? Convert.ToInt32(existingSlot["quantity"]) : 0;
                            existingSlot["quantity"] = currentQty + quantity;
                            addedCount++;
                            continue; // Không cần tạo slot mới
                        }
                    }

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

                    // Thêm item vào slot mới — chỉ lưu các field cần thiết, không lưu isEquipped/iconId
                    var newSlot = new Dictionary<string, object>
                    {
                        ["slotIndex"]      = emptySlotIndex,
                        ["itemTemplateId"] = itemTemplateId,
                        ["quantity"]       = quantity,
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
        /// POST /api/player/{playerId}/inventory/sort
        /// Sắp xếp lại inventory: gom các item về phía trước, loại bỏ ô trống giữa.
        /// </summary>
        [HttpPost("{playerId}/inventory/sort")]
        public async Task<IActionResult> SortInventory(int playerId)
        {
            try
            {
                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                    return NotFound($"Player với ID {playerId} không tồn tại.");

                if (string.IsNullOrEmpty(player.InventoryJson) || player.InventoryJson == "[]")
                    return Ok(new { message = "Inventory đã rỗng.", player_id = playerId, inventory = Array.Empty<object>() });

                var rawItems = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(player.InventoryJson)
                               ?? new List<Dictionary<string, JsonElement>>();

                // Lọc slot có item (quantity > 0), re-index từ 0
                int newIndex = 0;
                var sortedInventory = new List<Dictionary<string, object>>();
                foreach (var item in rawItems)
                {
                    if (!item.TryGetValue("quantity", out var qtyEl) ||
                        !qtyEl.TryGetInt32(out int qty) || qty <= 0)
                        continue;

                    var slot = new Dictionary<string, object>();
                    foreach (var kvp in item)
                        slot[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.Number => kvp.Value.TryGetInt32(out var iv) ? iv : kvp.Value.GetDouble(),
                            JsonValueKind.String => (object)(kvp.Value.GetString() ?? ""),
                            JsonValueKind.True   => true,
                            JsonValueKind.False  => false,
                            _                    => kvp.Value.ToString()
                        };
                    slot["slotIndex"] = newIndex++;
                    sortedInventory.Add(slot);
                }

                player.InventoryJson = JsonSerializer.Serialize(sortedInventory);
                player.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Đã sắp xếp inventory. {sortedInventory.Count} item(s).",
                    player_id = playerId,
                    inventory = sortedInventory,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi sắp xếp inventory: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/inventory/use-item
        /// Body: { "slotIndex": 0 }
        /// Sử dụng item trong túi đồ:
        ///   - type 30 (túi đồ mở rộng): tăng bag_slots thêm 5, xóa 1 item.
        ///   - type 21-29 (tiêu thụ): giảm số lượng (effects xử lý client-side hoặc extend sau).
        /// </summary>
        [HttpPost("{playerId}/inventory/use-item")]
        public async Task<IActionResult> UseInventoryItem(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                // Dùng JWT userId làm authoritative (giống AddItemsToInventory)
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
                if (userIdClaim == null) return Unauthorized();
                int userId = int.Parse(userIdClaim.Value);
                int targetPlayerId = userId;

                if (!body.TryGetProperty("slotIndex", out var slotProp))
                    return BadRequest("Thiếu field 'slotIndex'.");

                int slotIndex = slotProp.GetInt32();

                var player = await _db.PlayerData.FindAsync(targetPlayerId);
                if (player == null)
                    return NotFound($"Player với ID {targetPlayerId} không tồn tại.");

                var rawItems = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
                    string.IsNullOrEmpty(player.InventoryJson) ? "[]" : player.InventoryJson)
                    ?? new List<Dictionary<string, JsonElement>>();

                // Tìm item ở slot
                var targetRaw = rawItems.FirstOrDefault(item =>
                    item.TryGetValue("slotIndex", out var si) && si.TryGetInt32(out int idx) && idx == slotIndex);

                if (targetRaw == null)
                    return BadRequest($"Không tìm thấy item ở slot {slotIndex}.");

                int qty = targetRaw.TryGetValue("quantity", out var qEl) && qEl.TryGetInt32(out int q) ? q : 0;
                if (qty <= 0)
                    return BadRequest($"Slot {slotIndex} không có item.");

                int itemTemplateId = targetRaw.TryGetValue("itemTemplateId", out var tplEl) && tplEl.TryGetInt32(out int tplId) ? tplId : 0;
                var itemTemplate   = itemTemplateId > 0 ? await _db.ItemTemplates.FindAsync(itemTemplateId) : null;
                int itemType       = itemTemplate?.Type ?? -1;
                string itemName    = itemTemplate?.Name ?? $"Item {itemTemplateId}";

                const int BagItemType = 32;         // type 32 = túi mở rộng; type 30 = vật liệu (KHÔNG mở túi)
                const int WaveTicketItemType = 31;
                const int WaveTicketPlusOneItemId = 409;
                const int WaveTicketPlusTwoItemId = 410;

                var info = player.GetInfoChar();
                string effectMsg;
                int hpRestore = 0, mpRestore = 0;
                int waveEntryBonusAdded = 0;
                var newBuffsAdded = new List<object>();

                if (itemType == BagItemType)
                {
                    info.BagEquippedItems ??= new List<BagEquippedItemInfo>();

                    int quickSlotIndex = FindFirstAvailableBagQuickSlotIndex(info.BagEquippedItems, MaxEquippedBagQuickSlots);
                    if (quickSlotIndex < 0)
                        return BadRequest($"Chỉ có thể gắn tối đa {MaxEquippedBagQuickSlots} item mở rộng túi.");

                    info.BagSlots = ResolveBagSlotLimit(info) + BagExpandBy;
                    info.BagEquippedItems.Add(new BagEquippedItemInfo
                    {
                        QuickSlotIndex = quickSlotIndex,
                        ItemTemplateId = itemTemplateId,
                        ItemCode = targetRaw.TryGetValue("itemCode", out var bagCodeProp) ? bagCodeProp.GetString() ?? "" : "",
                        ItemName = itemName,
                        IconId = itemTemplate?.IdIcon ?? 0,
                        UpgradeLevel = targetRaw.TryGetValue("upgradeLevel", out var bagLvlProp) && bagLvlProp.TryGetInt32(out int bagUpgradeLevel)
                            ? bagUpgradeLevel
                            : 0,
                        StrOptions = targetRaw.TryGetValue("strOptions", out var bagOptProp)
                            ? bagOptProp.GetString() ?? ""
                            : "",
                        SlotBonus = BagExpandBy,
                        IsLocked = targetRaw.TryGetValue("isLocked", out var bagLockProp) && bagLockProp.ValueKind == JsonValueKind.True
                    });
                    effectMsg = $"Mở rộng túi đồ thành công! Số ô túi: {info.BagSlots}";
                }
                else if (itemType == WaveTicketItemType)
                {
                    waveEntryBonusAdded = itemTemplateId switch
                    {
                        WaveTicketPlusOneItemId => 1,
                        WaveTicketPlusTwoItemId => 2,
                        _ => 0
                    };

                    if (waveEntryBonusAdded <= 0)
                        return BadRequest($"Item vé phó bản '{itemName}' (templateId={itemTemplateId}) chưa được cấu hình số lượt cộng thêm.");

                    effectMsg = $"Đã sử dụng {itemName}. Cộng thêm {waveEntryBonusAdded} lượt Phó Bản Sóng cho hôm nay.";
                }
                else if (itemType >= 21 && itemType <= 29)
                {
                    // Đọc effects từ item_effect_template
                    var effects = await _db.ItemEffectTemplates
                        .Where(e => e.ItemTemplateId == itemTemplateId)
                        .OrderBy(e => e.SortOrder)
                        .ToListAsync();

                    if (effects.Count == 0)
                    {
                        effects = GetLegacyConsumableEffects(itemTemplateId);
                        if (effects.Count == 0)
                            return BadRequest($"Item consumable '{itemName}' (templateId={itemTemplateId}, type={itemType}) chưa có cấu hình trong item_effect_template.");

                        _logger.LogWarning(
                            "[UseInventoryItem] Dùng fallback effect config cho itemTemplateId={ItemTemplateId}, itemName={ItemName}",
                            itemTemplateId, itemName);
                    }

                    var activeBuffs = player.GetActiveBuffs();
                    var newBuffList = new List<GameServerApi.Models.ActiveBuff>();

                    foreach (var eff in effects)
                    {
                        if (eff.EffectType == "HpRestore")
                        {
                            if (eff.DurationSec > 0)
                            {
                                // Hồi HP theo thời gian – hiện icon buff, client tick value/s
                                var existing = activeBuffs.FirstOrDefault(b => b.EffectType == "HpRestoreOverTime");
                                if (existing != null)
                                {
                                    existing.ExpireAt = DateTime.UtcNow.AddSeconds(eff.DurationSec);
                                    existing.Value    = eff.Value;
                                    existing.IconId   = eff.IconId;
                                    existing.Name     = eff.DisplayName;
                                    existing.Detail   = eff.Detail;
                                }
                                else
                                {
                                    var newBuff = new GameServerApi.Models.ActiveBuff
                                    {
                                        EffectType = "HpRestoreOverTime",
                                        Value      = eff.Value,
                                        IconId     = eff.IconId,
                                        Name       = eff.DisplayName,
                                        Detail     = eff.Detail,
                                        ExpireAt   = DateTime.UtcNow.AddSeconds(eff.DurationSec)
                                    };
                                    activeBuffs.Add(newBuff);
                                    newBuffList.Add(newBuff);
                                }
                            }
                            else
                            {
                                // Hồi HP ngay lập tức
                                int restored = eff.Value >= 9999
                                    ? info.MaxHp - info.Hp
                                    : Math.Min(eff.Value, info.MaxHp - info.Hp);
                                info.Hp   = Math.Min(info.MaxHp, info.Hp + eff.Value);
                                hpRestore += restored;
                            }
                        }
                        else if (eff.EffectType == "MpRestore")
                        {
                            if (eff.DurationSec > 0)
                            {
                                // Hồi MP theo thời gian – hiện icon buff, client tick value/s
                                var existing = activeBuffs.FirstOrDefault(b => b.EffectType == "MpRestoreOverTime");
                                if (existing != null)
                                {
                                    existing.ExpireAt = DateTime.UtcNow.AddSeconds(eff.DurationSec);
                                    existing.Value    = eff.Value;
                                    existing.IconId   = eff.IconId;
                                    existing.Name     = eff.DisplayName;
                                    existing.Detail   = eff.Detail;
                                }
                                else
                                {
                                    var newBuff = new GameServerApi.Models.ActiveBuff
                                    {
                                        EffectType = "MpRestoreOverTime",
                                        Value      = eff.Value,
                                        IconId     = eff.IconId,
                                        Name       = eff.DisplayName,
                                        Detail     = eff.Detail,
                                        ExpireAt   = DateTime.UtcNow.AddSeconds(eff.DurationSec)
                                    };
                                    activeBuffs.Add(newBuff);
                                    newBuffList.Add(newBuff);
                                }
                            }
                            else
                            {
                                // Hồi MP ngay lập tức
                                int restored = Math.Min(eff.Value, info.MaxMp - info.Mp);
                                info.Mp   = Math.Min(info.MaxMp, info.Mp + eff.Value);
                                mpRestore += restored;
                            }
                        }
                        else if (eff.EffectType == "GeneExpAdd")
                        {
                            // Tính gene_exp thêm vào, có áp dụng GeneExpBuff đang active
                            double geneExpMult = 1.0;
                            var geneExpBuffs = activeBuffs.Where(b => b.EffectType == "GeneExpBuff"
                                && (b.ExpireAt == null || b.ExpireAt > DateTime.UtcNow));
                            foreach (var gb in geneExpBuffs)
                                geneExpMult += gb.Value / 100.0;
                            int geneExpGain = (int)(eff.Value * geneExpMult);
                            info.GeneExp += geneExpGain;
                        }
                        else if (eff.DurationSec > 0)
                        {
                            // Timed buff – nếu đã có buff cùng loại thì THAY THẾ hoàn toàn
                            // (reset thời gian về eff.DurationSec tính từ NOW, cập nhật tất cả fields).
                            var existing = activeBuffs.FirstOrDefault(b => b.EffectType == eff.EffectType);
                            if (existing != null)
                            {
                                existing.ExpireAt = DateTime.UtcNow.AddSeconds(eff.DurationSec);
                                existing.Value    = eff.Value;
                                existing.IconId   = eff.IconId;
                                existing.Name     = eff.DisplayName;
                                existing.Detail   = eff.Detail;
                            }
                            else
                            {
                                var newBuff = new GameServerApi.Models.ActiveBuff
                                {
                                    EffectType  = eff.EffectType,
                                    Value       = eff.Value,
                                    IconId      = eff.IconId,
                                    Name        = eff.DisplayName,
                                    Detail      = eff.Detail,
                                    ExpireAt    = DateTime.UtcNow.AddSeconds(eff.DurationSec)
                                };
                                activeBuffs.Add(newBuff);
                                newBuffList.Add(newBuff);
                            }
                        }
                    }

                    player.SetActiveBuffs(activeBuffs);

                    effectMsg = effects.Count > 0
                        ? $"Đã sử dụng {itemName}."
                        : $"Đã sử dụng {itemName}. (Chưa cấu hình effect)";

                    // Serialize newBuffs cho response
                    if (newBuffList.Count > 0)
                    {
                        newBuffsAdded = newBuffList.Select(b => (object)new
                        {
                            effectType = b.EffectType,
                            value      = b.Value,
                            iconId     = b.IconId,
                            name       = b.Name,
                            detail     = b.Detail,
                            expireAt   = b.ExpireAt?.ToString("o")
                        }).ToList();
                    }
                }
                else
                {
                    return BadRequest($"Item này không thể sử dụng theo cách này (type={itemType}).");
                }

                // Chuyển sang mutable, giảm số lượng
                var inventory = rawItems.Select(item =>
                {
                    var d = new Dictionary<string, object>();
                    foreach (var kvp in item)
                        d[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.Number => kvp.Value.TryGetInt32(out var iv) ? iv : kvp.Value.GetDouble(),
                            JsonValueKind.String => (object)(kvp.Value.GetString() ?? ""),
                            JsonValueKind.True   => true,
                            JsonValueKind.False  => false,
                            _                    => kvp.Value.ToString()
                        };
                    return d;
                }).ToList();

                var mutableTarget = inventory.FirstOrDefault(d =>
                    d.TryGetValue("slotIndex", out var si) && Convert.ToInt32(si) == slotIndex);

                if (mutableTarget != null)
                {
                    int newQty = qty - 1;
                    if (newQty <= 0)
                        inventory.RemoveAll(d => d.TryGetValue("slotIndex", out var si) && Convert.ToInt32(si) == slotIndex);
                    else
                        mutableTarget["quantity"] = newQty;
                }

                player.SetInfoChar(info);
                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Serialize active_buffs đầy đủ cho client
                var activeBuffsForResponse = player.GetActiveBuffs().Select(b => new
                {
                    effectType = b.EffectType,
                    value      = b.Value,
                    iconId     = b.IconId,
                    name       = b.Name,
                    detail     = b.Detail,
                    expireAt   = b.ExpireAt?.ToString("o")
                }).ToArray();

                return Ok(new
                {
                    message    = effectMsg,
                    player_id  = targetPlayerId,
                    item_template_id = itemTemplateId,
                    wave_entry_bonus_added = waveEntryBonusAdded,
                    bag_slots  = info.BagSlots,
                    bag_equipped_items = BuildBagEquippedItemsResponse(info.BagEquippedItems),
                    hp_restore = hpRestore,
                    mp_restore = mpRestore,
                    current_hp = info.Hp,
                    current_mp = info.Mp,
                    gene_exp   = info.GeneExp,
                    active_buffs = activeBuffsForResponse,
                    new_buffs    = newBuffsAdded,
                    inventory,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi sử dụng item: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/bag/unequip
        /// Body: { "quickSlotIndex": 0 }
        /// Tháo item mở rộng túi đang gắn ở quick slot, trả lại vào inventory
        /// và giảm số ô túi tương ứng.
        /// </summary>
        [HttpPost("{playerId}/bag/unequip")]
        public async Task<IActionResult> UnequipBagItem(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id");
                if (userIdClaim == null) return Unauthorized();
                int targetPlayerId = int.Parse(userIdClaim.Value);

                if (!body.TryGetProperty("quickSlotIndex", out var slotProp))
                    return BadRequest("Thiếu field 'quickSlotIndex'.");

                int quickSlotIndex = slotProp.GetInt32();

                var player = await _db.PlayerData.FindAsync(targetPlayerId);
                if (player == null)
                    return NotFound($"Player với ID {targetPlayerId} không tồn tại.");

                var info = player.GetInfoChar();
                info.BagEquippedItems ??= new List<BagEquippedItemInfo>();

                var equippedBag = info.BagEquippedItems.FirstOrDefault(item => item.QuickSlotIndex == quickSlotIndex);
                if (equippedBag == null)
                    return BadRequest($"Không tìm thấy item túi đang gắn ở quick slot {quickSlotIndex}.");

                var inventory = ParseMutableInventory(player.InventoryJson);
                var itemTemplate = equippedBag.ItemTemplateId > 0
                    ? await _db.ItemTemplates.FindAsync(equippedBag.ItemTemplateId)
                    : null;

                int slotBonus = equippedBag.SlotBonus > 0 ? equippedBag.SlotBonus : BagExpandBy;
                int newBagSlots = Math.Max(DefaultBagSlots, ResolveBagSlotLimit(info) - slotBonus);

                if (!TryStoreBagItemBackToInventory(inventory, equippedBag, itemTemplate, newBagSlots, out var updatedInventory))
                {
                    return BadRequest($"Không đủ chỗ trống để tháo {equippedBag.ItemName}. Hãy dọn bớt đồ trong túi trước.");
                }

                info.BagSlots = newBagSlots;
                info.BagEquippedItems.Remove(equippedBag);

                player.SetInfoChar(info);
                player.InventoryJson = JsonSerializer.Serialize(updatedInventory);
                player.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Đã tháo {equippedBag.ItemName} khỏi túi mở rộng.",
                    player_id = targetPlayerId,
                    bag_slots = info.BagSlots,
                    bag_equipped_items = BuildBagEquippedItemsResponse(info.BagEquippedItems),
                    inventory = updatedInventory,
                    updated_at = player.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi tháo item túi: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/equipment/equip
        /// Body: { "inventorySlotIndex": 0 }
        /// Trang bị item từ inventory vào equipment slot tương ứng
        /// </summary>
        [HttpPost("{playerId}/equipment/equip")]
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
                        int maxSlots = ResolveBagSlotLimit(player.GetInfoChar());
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
                int maxSlots = ResolveBagSlotLimit(player.GetInfoChar());
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
        /// GET /api/player/{playerId}/active-buffs
        /// Lấy danh sách buff đang active của player.
        /// </summary>
        [HttpGet("{playerId}/active-buffs")]
        public async Task<IActionResult> GetActiveBuffs(int playerId)
        {
            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var active_buffs = player.GetActiveBuffs()
                .Where(b => b.ExpireAt == null || b.ExpireAt > DateTime.UtcNow)
                .Select(b => new
                {
                    effectType = b.EffectType,
                    value      = b.Value,
                    iconId     = b.IconId,
                    name       = b.Name,
                    detail     = b.Detail,
                    expireAt   = b.ExpireAt?.ToString("o")
                }).ToArray();

            return Ok(new { active_buffs });
        }

        private static List<GameServerApi.Models.Entities.ItemEffectTemplate> GetLegacyConsumableEffects(int itemTemplateId)
        {
            return itemTemplateId switch
            {
                11 => new List<GameServerApi.Models.Entities.ItemEffectTemplate>
                {
                    new() { ItemTemplateId = 11, EffectType = "HpRestore", Value = 200, DurationSec = 30, IconId = 531, DisplayName = "Hồi máu", Detail = "+200 HP/s trong 30 giây", SortOrder = 1 }
                },
                12 => new List<GameServerApi.Models.Entities.ItemEffectTemplate>
                {
                    new() { ItemTemplateId = 12, EffectType = "HpRestore", Value = 500, DurationSec = 30, IconId = 532, DisplayName = "Hồi máu", Detail = "+500 HP/s trong 30 giây", SortOrder = 1 }
                },
                13 => new List<GameServerApi.Models.Entities.ItemEffectTemplate>
                {
                    new() { ItemTemplateId = 13, EffectType = "HpRestore", Value = 1200, DurationSec = 30, IconId = 533, DisplayName = "Hồi máu", Detail = "+1200 HP/s trong 30 giây", SortOrder = 1 }
                },
                14 => new List<GameServerApi.Models.Entities.ItemEffectTemplate>
                {
                    new() { ItemTemplateId = 14, EffectType = "MpRestore", Value = 150, DurationSec = 30, IconId = 538, DisplayName = "Hồi linh", Detail = "+150 MP/s trong 30 giây", SortOrder = 2 }
                },
                15 => new List<GameServerApi.Models.Entities.ItemEffectTemplate>
                {
                    new() { ItemTemplateId = 15, EffectType = "MpRestore", Value = 400, DurationSec = 3, IconId = 539, DisplayName = "Hồi linh", Detail = "+400 MP/s trong 3 giây", SortOrder = 2 }
                },
                16 => new List<GameServerApi.Models.Entities.ItemEffectTemplate>
                {
                    new() { ItemTemplateId = 16, EffectType = "MpRestore", Value = 1000, DurationSec = 3, IconId = 540, DisplayName = "Hồi linh", Detail = "+1000 MP/s trong 3 giây", SortOrder = 2 }
                },
                _ => new List<GameServerApi.Models.Entities.ItemEffectTemplate>()
            };
        }

        /// <summary>
        /// GET /api/player/{playerId}/equipment
        /// Lấy thông tin trang bị hiện tại của player
        /// </summary>
        [HttpGet("{playerId}/equipment")]
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

                _logger.LogInformation("[LevelUp] Player leveled up to {Level}. SkillPts={SkillPts} PotPts={PotPts}",
                    info.Level, info.SkillPoints, info.PotentialPoints);
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

        private static int ResolveBagSlotLimit(InfoChar? info)
        {
            // Giới hạn tối đa = DefaultBagSlots + MaxEquippedBagQuickSlots × BagExpandBy = 20 + 3×5 = 35
            int maxPossible = DefaultBagSlots + MaxEquippedBagQuickSlots * BagExpandBy;
            int actual = info?.BagSlots > 0 ? info.BagSlots : DefaultBagSlots;
            return Math.Min(actual, maxPossible);
        }

        private static int FindFirstAvailableBagQuickSlotIndex(List<BagEquippedItemInfo>? bagItems, int maxQuickSlots)
        {
            for (int i = 0; i < maxQuickSlots; i++)
            {
                if (bagItems == null || bagItems.All(item => item.QuickSlotIndex != i))
                    return i;
            }

            return -1;
        }

        private static object[] BuildBagEquippedItemsResponse(List<BagEquippedItemInfo>? bagItems)
        {
            if (bagItems == null || bagItems.Count == 0)
                return Array.Empty<object>();

            return bagItems
                .OrderBy(item => item.QuickSlotIndex)
                .Select(item => (object)new
                {
                    quick_slot_index = item.QuickSlotIndex,
                    item_template_id = item.ItemTemplateId,
                    item_code = item.ItemCode,
                    item_name = item.ItemName,
                    icon_id = item.IconId > 0 ? item.IconId.ToString() : "",
                    upgrade_level = item.UpgradeLevel,
                    str_options = item.StrOptions ?? "",
                    slot_bonus = item.SlotBonus > 0 ? item.SlotBonus : BagExpandBy,
                    is_locked = item.IsLocked
                })
                .ToArray();
        }

        private static List<Dictionary<string, object>> ParseMutableInventory(string inventoryJson)
        {
            var inventory = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(inventoryJson) || inventoryJson == "[]")
                return inventory;

            var existingInventory = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(inventoryJson);
            if (existingInventory == null)
                return inventory;

            foreach (var item in existingInventory)
            {
                var dict = new Dictionary<string, object>();
                foreach (var kvp in item)
                {
                    dict[kvp.Key] = kvp.Value.ValueKind switch
                    {
                        JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : kvp.Value.GetDouble(),
                        JsonValueKind.String => kvp.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => kvp.Value.ToString()
                    };
                }
                inventory.Add(dict);
            }

            return inventory;
        }

        private static bool TryStoreBagItemBackToInventory(
            List<Dictionary<string, object>> inventory,
            BagEquippedItemInfo bagItem,
            ItemTemplate? itemTemplate,
            int maxSlots,
            out List<Dictionary<string, object>> updatedInventory)
        {
            updatedInventory = CompactInventorySlots(inventory);

            bool isStackable = itemTemplate != null &&
                string.Equals(itemTemplate.IsXepChong, "True", StringComparison.OrdinalIgnoreCase) &&
                bagItem.UpgradeLevel <= 0 &&
                string.IsNullOrEmpty(bagItem.StrOptions);

            if (isStackable)
            {
                var existingSlot = updatedInventory.FirstOrDefault(slot =>
                    slot.TryGetValue("itemTemplateId", out var rawTemplateId) &&
                    Convert.ToInt32(rawTemplateId) == bagItem.ItemTemplateId &&
                    (!slot.TryGetValue("upgradeLevel", out var rawUpgradeLevel) || Convert.ToInt32(rawUpgradeLevel) == 0) &&
                    (!slot.TryGetValue("strOptions", out var rawStrOptions) || string.IsNullOrEmpty(rawStrOptions?.ToString())));

                if (existingSlot != null)
                {
                    int currentQuantity = existingSlot.TryGetValue("quantity", out var rawQuantity)
                        ? Convert.ToInt32(rawQuantity)
                        : 0;
                    existingSlot["quantity"] = currentQuantity + 1;
                    return true;
                }
            }

            if (updatedInventory.Count >= maxSlots)
                return false;

            updatedInventory.Add(new Dictionary<string, object>
            {
                ["slotIndex"] = updatedInventory.Count,
                ["itemTemplateId"] = bagItem.ItemTemplateId,
                ["quantity"] = 1,
                ["upgradeLevel"] = bagItem.UpgradeLevel,
                ["strOptions"] = bagItem.StrOptions ?? "",
                ["isLocked"] = bagItem.IsLocked
            });

            return true;
        }

        private static List<Dictionary<string, object>> CompactInventorySlots(List<Dictionary<string, object>> inventory)
        {
            var compacted = inventory
                .Where(slot => slot.TryGetValue("quantity", out var rawQuantity) && Convert.ToInt32(rawQuantity) > 0)
                .OrderBy(slot => slot.TryGetValue("slotIndex", out var rawSlotIndex) ? Convert.ToInt32(rawSlotIndex) : int.MaxValue)
                .Select(slot => new Dictionary<string, object>(slot))
                .ToList();

            for (int i = 0; i < compacted.Count; i++)
            {
                compacted[i]["slotIndex"] = i;
            }

            return compacted;
        }

        // ================================================================
        //  SKILL ENDPOINTS
        // ================================================================

        /// <summary>
        /// GET /api/player/{playerId}/skills
        /// Trả về tất cả skills từ skill_template kèm level hiện tại của player.
        /// </summary>
        [HttpGet("{playerId}/skills")]
        public async Task<IActionResult> GetPlayerSkills(int playerId)
        {
            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            // Tính final_stats để cộng attack vào skill damage
            var finalStats     = StatCalculator.Compute(info, player.EquipmentJson, player.PotentialStatsJson);
            int playerFinalAtk = finalStats.Attack;

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

            // Lọc skills:
            // 1. hybrid_id IS NOT NULL → chỉ hiện với player is_hybrid=true có hybrid_id khớp
            // 2. hybrid_id IS NULL && element_type IS NULL → universal (hiện cho tất cả, ví dụ DASH)
            // 3. element_type IS NOT NULL → chỉ hiện với hệ tương ứng
            var templates = await _db.SkillTemplates
                .Where(s =>
                    (s.HybridId == null && (s.ElementType == null || s.ElementType == info.ElementType)) ||
                    (s.HybridId != null && info.IsHybrid && s.HybridId == info.HybridId))
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
                player_final_attack    = playerFinalAtk,
                skills                 = skillList
            });
        }

        /// <summary>
        /// POST /api/player/{playerId}/skills/upgrade
        /// Body: { "skill_id": 1 }
        /// Nâng cấp skill lên 1 level (trừ skill_points).
        /// </summary>
        [HttpPost("{playerId}/skills/upgrade")]
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

        /// <summary>
        /// POST /api/player/{playerId}/potential/allocate
        /// Body: { "allocations": [ {"stat_name":"attack","points":3}, {"stat_name":"hp","points":2} ] }
        /// Phân bổ nhiều điểm tiềm năng cùng lúc. Server validate đủ điểm trước khi ghi DB.
        /// </summary>
        [HttpPost("{playerId}/potential/allocate")]
        public async Task<IActionResult> AllocatePotential(int playerId, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("allocations", out var allocProp) ||
                    allocProp.ValueKind != JsonValueKind.Array)
                    return BadRequest("Thiếu mảng 'allocations'.");

                // Parse input allocations
                var requested = new Dictionary<string, int>();
                foreach (var item in allocProp.EnumerateArray())
                {
                    if (!item.TryGetProperty("stat_name", out var sn) ||
                        !item.TryGetProperty("points",    out var pts))
                        return BadRequest("Mỗi phần tử cần có 'stat_name' và 'points'.");

                    string stat = sn.GetString() ?? "";
                    int    pts2 = pts.TryGetInt32(out var v) ? v : 0;

                    if (!PotentialStatConfig.ContainsKey(stat))
                        return BadRequest($"Chỉ số '{stat}' không hợp lệ.");
                    if (pts2 <= 0) continue;  // bỏ qua 0 hoặc âm

                    requested[stat] = requested.TryGetValue(stat, out var cur) ? cur + pts2 : pts2;
                }

                if (requested.Count == 0)
                    return BadRequest("Không có điểm nào để phân bổ.");

                int totalNeeded = requested.Values.Sum();

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                var info = player.GetInfoChar();

                if (info.PotentialPoints < totalNeeded)
                    return BadRequest(
                        $"Không đủ điểm tiềm năng. Cần {totalNeeded}, còn {info.PotentialPoints}.");

                // Parse existing potential_stats
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
                                    potentialPoints[kvp.Key] = kvp.Value.TryGetInt32(out var vv) ? vv : 0;
                    }
                    catch { }
                }

                // Apply all allocations
                foreach (var (stat, pts2) in requested)
                    potentialPoints[stat] += pts2;

                info.PotentialPoints -= totalNeeded;
                player.SetInfoChar(info);
                player.PotentialStatsJson = JsonSerializer.Serialize(potentialPoints);
                player.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var updatedStats = PotentialStatConfig.Select(cfg => new
                {
                    stat_name       = cfg.Key,
                    display_name    = cfg.Value.DisplayName,
                    new_points      = potentialPoints[cfg.Key],
                    value_per_point = cfg.Value.ValuePerPoint,
                    total_value     = potentialPoints[cfg.Key] * cfg.Value.ValuePerPoint
                }).ToList();

                return Ok(new
                {
                    message                    = $"Đã phân bổ {totalNeeded} điểm tiềm năng thành công.",
                    potential_points_remaining = info.PotentialPoints,
                    updated_stats              = updatedStats
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi phân bổ tiềm năng: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/player/{playerId}/gain-exp
        /// Body: { "amount": 50 }
        /// Cộng thêm EXP vào player (delta, không set tuyệt đối).
        /// Tự động xử lý level-up nếu đủ EXP.
        /// Được gọi bởi server khi player kill quái.
        /// </summary>
        [HttpPost("{playerId}/gain-exp")]
        public async Task<IActionResult> GainExp(int playerId, [FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("amount", out var amtProp))
                return BadRequest("Thiếu field 'amount'.");

            if (!amtProp.TryGetInt32(out int amount) || amount <= 0)
                return BadRequest("'amount' phải là số nguyên dương.");

            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();
            info.Experience += amount;

            var (leveledUp, expAtCurrent, expForNext) = await ProcessLevelUpAsync(info);

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogDebug("[PlayerCtrl] GainExp playerId={PlayerId} amount={Amount} totalExp={TotalExp} level={Level} leveledUp={LeveledUp}",
                playerId, amount, info.Experience, info.Level, leveledUp);

            return Ok(new
            {
                success     = true,
                experience  = info.Experience,
                level       = info.Level,
                leveled_up  = leveledUp,
                exp_at_current_level = expAtCurrent,
                exp_for_next_level   = expForNext
            });
        }

        /// <summary>
        /// GET /api/player/by-user/{userId}
        /// Trả về thông tin tóm tắt nhân vật của một người dùng khác (dùng cho Friend Profile).
        /// Chỉ trả về thông tin công khai: tên, level, nguyên tố, trang bị, kỹ năng, tiềm năng.
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetPlayerByUserId(int userId)
        {
            _logger.LogInformation("[PlayerController] GetPlayerByUserId requested userId={UserId}", userId);

            var player = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == userId);
            if (player == null)
            {
                _logger.LogWarning("[PlayerController] GetPlayerByUserId failed userId={UserId}: player not found", userId);
                return NotFound("Người chơi chưa tạo nhân vật.");
            }

            var info = player.GetInfoChar();
            var finalStats = StatCalculator.Compute(info, player.EquipmentJson, player.PotentialStatsJson);

            object equipment;
            try
            {
                equipment = JsonSerializer.Deserialize<object>(player.EquipmentJson) ?? new { };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlayerController] GetPlayerByUserId equipment parse failed userId={UserId}", userId);
                equipment = new { };
            }

            var skillRows = await _db.PlayerSkillRecords
                .Where(s => s.PlayerId == player.PlayerId)
                .Join(
                    _db.SkillTemplates,
                    record => record.SkillId,
                    template => template.SkillId,
                    (record, template) => new { record, template })
                .OrderBy(x => x.template.ElementType)
                .ThenBy(x => x.template.SkillId)
                .ToListAsync();

            var skills = skillRows.Select(row =>
            {
                float currentCooldownSec = 3f;
                float currentEffectValue = 0f;
                int currentMpCost = 0;
                string currentDesc = row.template.Description ?? string.Empty;

                if (!string.IsNullOrEmpty(row.template.LevelsJson))
                {
                    try
                    {
                        var levels = JsonSerializer.Deserialize<List<JsonElement>>(row.template.LevelsJson);
                        if (levels != null && levels.Count > 0)
                        {
                            int index = Math.Clamp(row.record.SkillLevel - 1, 0, levels.Count - 1);
                            var current = levels[index];
                            if (current.TryGetProperty("cooldown_sec", out var cooldownProp))
                                currentCooldownSec = (float)cooldownProp.GetDouble();
                            if (current.TryGetProperty("effect_value", out var effectProp))
                                currentEffectValue = (float)effectProp.GetDouble();
                            if (current.TryGetProperty("mp_cost", out var mpProp))
                                currentMpCost = mpProp.GetInt32();
                            if (current.TryGetProperty("desc", out var descProp))
                                currentDesc = descProp.GetString() ?? currentDesc;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[PlayerController] GetPlayerByUserId skill levels parse failed userId={UserId} skillId={SkillId}",
                            userId,
                            row.template.SkillId);
                    }
                }

                return new
                {
                    skill_id = row.template.SkillId,
                    skill_code = row.template.SkillCode,
                    skill_name = row.template.SkillName,
                    description = row.template.Description,
                    element_type = row.template.ElementType,
                    max_level = row.template.MaxLevel,
                    level_to_unlock = row.template.LevelToUnlock,
                    gene_tier_required = row.template.GeneTierRequired,
                    current_level = row.record.SkillLevel,
                    current_cooldown_sec = currentCooldownSec,
                    current_effect_value = currentEffectValue,
                    current_mp_cost = currentMpCost,
                    can_upgrade = false,
                    next_level_player_req = 0,
                    next_level_sp_cost = 0,
                    next_level_desc = currentDesc,
                    icon_id = row.template.IconId
                };
            }).ToList();

            var potentialPoints = new Dictionary<string, int>
            {
                ["attack"] = 0,
                ["hp"] = 0,
                ["mp"] = 0,
                ["defense"] = 0,
                ["gene"] = 0
            };

            try
            {
                if (!string.IsNullOrEmpty(player.PotentialStatsJson) && player.PotentialStatsJson != "{}")
                {
                    var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(player.PotentialStatsJson);
                    if (raw != null)
                    {
                        foreach (var kvp in raw)
                        {
                            if (potentialPoints.ContainsKey(kvp.Key))
                                potentialPoints[kvp.Key] = kvp.Value.TryGetInt32(out var value) ? value : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlayerController] GetPlayerByUserId potential parse failed userId={UserId}", userId);
            }

            var potentialStats = PotentialStatConfig.Select(cfg => new
            {
                stat_name = cfg.Key,
                display_name = cfg.Value.DisplayName,
                current_points = potentialPoints.TryGetValue(cfg.Key, out var points) ? points : 0,
                value_per_point = cfg.Value.ValuePerPoint,
                total_value = (potentialPoints.TryGetValue(cfg.Key, out var totalPoints) ? totalPoints : 0) * cfg.Value.ValuePerPoint
            }).ToList();

            _logger.LogInformation(
                "[PlayerController] GetPlayerByUserId success userId={UserId} playerId={PlayerId} skills={SkillCount} potentialStats={PotentialCount}",
                userId,
                player.PlayerId,
                skills.Count,
                potentialStats.Count);

            return Ok(new
            {
                player_id = player.PlayerId,
                user_id = userId,
                character_name = player.CharacterName,
                element_type = info.ElementType,
                gender = player.Gender,
                level = info.Level,
                gold = info.Gold,
                gene_tier = info.GeneTier,
                is_hybrid = info.IsHybrid,
                equipment,
                skills,
                potential_stats = potentialStats,
                final_stats = new
                {
                    hp = finalStats.Hp,
                    max_hp = finalStats.MaxHp,
                    mp = finalStats.Mp,
                    max_mp = finalStats.MaxMp,
                    attack = finalStats.Attack,
                    defense = finalStats.Defense,
                    move_speed = finalStats.MoveSpeed,
                }
            });
        }
    }
}

