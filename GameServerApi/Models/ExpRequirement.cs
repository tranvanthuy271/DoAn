using System;

namespace GameServerApi.Models
{
    public class ExpRequirement
    {
        public int Level { get; set; } // PK
        public int ExpRequired { get; set; }

        /// <summary>
        /// JSON: { "hp": 50, "mp": 30, "attack": 10, ... }
        /// </summary>
        public string BaseStatIncreaseJson { get; set; } = "{}";

        public int SkillPoints { get; set; }
        public int PotentialPoints { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

