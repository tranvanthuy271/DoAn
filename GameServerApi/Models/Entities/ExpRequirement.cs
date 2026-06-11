using System;

namespace GameServerApi.Models
{
    public class ExpRequirement
    {
        public int Level { get; set; } // PK
        public int ExpRequired { get; set; }

        // JSON: { "hp": 50, "mp": 30, "attack": 10, ... }
        public string BaseStatIncreaseJson { get; set; } = "{}";

        public int SkillPoints { get; set; }
        public int PotentialPoints { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

