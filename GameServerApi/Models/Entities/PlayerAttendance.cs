using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    [Table("player_attendance")]
    public class PlayerAttendance
    {
        [Key]
        public int      Id          { get; set; }
        public int      CharacterId { get; set; }
        public DateTime CheckDate   { get; set; }
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    }
}
