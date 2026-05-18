using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    [Table("player_quest_log")]
    public class PlayerQuestLog
    {
        [Key]
        public int Id { get; set; }

        public int      CharacterId { get; set; }
        public int      QuestId     { get; set; }

        [MaxLength(255)]
        public string   QuestName   { get; set; } = "";
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
