using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    [Table("player_dungeon_record")]
    public class PlayerDungeonRecord
    {
        [Key]
        public int      Id          { get; set; }
        public int      CharacterId { get; set; }
        public int      DungeonId   { get; set; }
        public int      BestWave    { get; set; } = 0;
        public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
    }
}
