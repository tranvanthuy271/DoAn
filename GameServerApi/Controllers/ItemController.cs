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

                // Convert sang format phù hợp cho Unity
                var response = itemTemplates.Select(item => new
                {
                    id = item.Id,
                    code = item.Code,
                    name = item.Name,
                    description = item.Description,
                    category = item.Category,
                    item_type = item.ItemType,
                    stackable = item.Stackable,
                    max_stack = item.MaxStack,
                    rarity = item.Rarity,
                    icon_id = item.IconId,
                    base_stat_json = item.BaseStatJson
                }).ToList();

                Console.WriteLine($"[ItemController] ✅ Returning {response.Count} item templates");
                
                // Log first 5 items for debugging
                int logCount = Math.Min(5, response.Count);
                Console.WriteLine($"[ItemController] 📋 Sample items (first {logCount}):");
                for (int i = 0; i < logCount; i++)
                {
                    var item = response[i];
                    Console.WriteLine($"  [{i+1}] ID={item.id}, Name='{item.name}', Code='{item.code}', IconId='{item.icon_id}'");
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
                    id = item.Id,
                    code = item.Code,
                    name = item.Name,
                    description = item.Description,
                    category = item.Category,
                    item_type = item.ItemType,
                    stackable = item.Stackable,
                    max_stack = item.MaxStack,
                    rarity = item.Rarity,
                    icon_id = item.IconId,
                    base_stat_json = item.BaseStatJson
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

        /// <summary>
        /// GET /api/item/templates/code/{code}
        /// Lấy thông tin item template theo code
        /// </summary>
        [HttpGet("templates/code/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetItemTemplateByCode(string code)
        {
            try
            {
                var item = await _db.ItemTemplates
                    .FirstOrDefaultAsync(i => i.Code == code);
                
                if (item == null)
                {
                    return NotFound(new
                    {
                        error = "Item template không tồn tại",
                        code = code
                    });
                }

                return Ok(new
                {
                    id = item.Id,
                    code = item.Code,
                    name = item.Name,
                    description = item.Description,
                    category = item.Category,
                    item_type = item.ItemType,
                    stackable = item.Stackable,
                    max_stack = item.MaxStack,
                    rarity = item.Rarity,
                    icon_id = item.IconId,
                    base_stat_json = item.BaseStatJson
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
    }
}
