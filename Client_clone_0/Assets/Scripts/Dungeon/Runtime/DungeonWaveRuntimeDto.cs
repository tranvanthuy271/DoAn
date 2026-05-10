using System;

[Serializable]
public class DungeonWaveRuntimeResponse
{
    public int dungeon_id;
    public int map_id;
    public string dungeon_name = "";
    public string scene_name = "";
    public int max_waves = 20;
    public int wave_time_seconds = 300;
    public float enemy_scale_percent = 10f;
    public float boss_scale_percent = 15f;
    public float exp_gold_scale_percent = 10f;
    public int daily_entry_limit = 1;
    public int entry_item_plus1_id = 409;
    public int entry_item_plus2_id = 410;
    public DungeonWaveMilestoneRewardDto[] milestone_rewards;
    public DungeonWaveEnemySpawnDto[] enemy_spawns;
    public DungeonWaveEnemySpawnDto boss_spawn;
}

[Serializable]
public class DungeonWaveMilestoneRewardDto
{
    public int wave;
    public long bonus_exp;
    public long bonus_gold;
    public DungeonWaveRewardItemDto[] items;
}

[Serializable]
public class DungeonWaveRewardItemDto
{
    public int item_template_id;
    public int quantity = 1;
    public int upgrade_level;
    public string str_options = "";
}

[Serializable]
public class DungeonWaveEnemySpawnDto
{
    public int enemy_id;
    public string enemy_name = "";
    public float spawn_x;
    public float spawn_y;
    public bool is_boss;
    public int level = 1;
    public int max_hp = 1;
    public int max_mp;
    public int base_damage = 1;
    public int base_defense;
    public int exp_reward;
    public int respawn_time;
    public float move_speed = 2f;
    public bool can_fly;
    public DropItemEntry[] drops;
    public string element_type = "None";
}