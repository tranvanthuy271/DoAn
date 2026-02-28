using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    /// <summary>
    /// Model cho bảng item_template trong database
    /// Chứa thông tin master data của các items trong game
    /// </summary>
    [Table("item_template")]
    public class ItemTemplate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Category: 1=Equipment, 2=Consumable, 3=Material
        /// </summary>
        [Column("category")]
        public int Category { get; set; }

        /// <summary>
        /// ItemType: 1=Weapon, 2=Potion, 3=Material, etc.
        /// </summary>
        [Column("item_type")]
        public int ItemType { get; set; }

        [Column("stackable")]
        public bool Stackable { get; set; } = true;

        [Column("max_stack")]
        public int MaxStack { get; set; } = 99;

        /// <summary>
        /// Gender limit: 0=All, 1=Male, 2=Female
        /// </summary>
        [Column("gender_limit")]
        public int GenderLimit { get; set; } = 0;

        /// <summary>
        /// Class limit: 0=All classes, specific class IDs otherwise
        /// </summary>
        [Column("class_limit")]
        public int ClassLimit { get; set; } = 0;

        /// <summary>
        /// Level required to use this item
        /// </summary>
        [Column("level_required")]
        public int LevelRequired { get; set; } = 0;

        /// <summary>
        /// Rarity: 1=Common, 2=Uncommon, 3=Rare, 4=Epic, 5=Legendary
        /// </summary>
        [Column("rarity")]
        public int Rarity { get; set; } = 1;

        /// <summary>
        /// Path to icon in Unity Resources (legacy)
        /// </summary>
        [Column("icon_path")]
        public string? IconPath { get; set; }

        /// <summary>
        /// Path to prefab in Unity Resources
        /// </summary>
        [Column("prefab_path")]
        public string? PrefabPath { get; set; }

        /// <summary>
        /// Icon ID để Unity load sprite từ Resources/ItemIcons
        /// Tính toán từ icon_path hoặc sử dụng code
        /// </summary>
        [NotMapped] // Không map vào DB, tính toán runtime
        public string IconId => IconPath ?? Code ?? "default_icon";

        /// <summary>
        /// JSON chứa base stats (attack, defense, heal_amount, etc.)
        /// </summary>
        [Column("base_stat_json")]
        public string? BaseStatJson { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
