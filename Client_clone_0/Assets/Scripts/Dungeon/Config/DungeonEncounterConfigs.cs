using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonWaveConfig", menuName = "DoAn/Dungeon/Wave Config")]
public class DungeonWaveConfig : ScriptableObject
{
    [Header("Identity")]
    public int dungeonId = 1;
    public string returnSceneName = "GameScene";
    public int returnMapId = 0;

    [Header("Flow")]
    public float roundTimeSeconds = 300f;
    public int maxRounds = 20;
    public float returnCountdownSeconds = 5f;

    [Header("Stat Scaling (compound per wave)")]
    [Range(0f, 100f)] public float roundScalingPercent = 10f;
    [Range(0f, 100f)] public float bossScalePercent    = 15f;
    [Range(0f, 100f)] public float expGoldScalePercent = 10f;

    [Header("Daily Entry Limit")]
    public int dailyEntryLimit = 1;
    [Tooltip("item_template_id của vé +1 lần. 0 = không dùng.")]
    public int entryItemIdPlusOne = 409;
    [Tooltip("item_template_id của vé +2 lần. 0 = không dùng.")]
    public int entryItemIdPlusTwo = 410;

    [Header("Optional Player Spawn Marker")]
    public Vector3 playerSpawnPosition = Vector3.zero;

    [Header("Enemy Waves (data load từ DB; SO chỉ để designer preview)")]
    public List<DungeonEnemyUnitConfig> enemySpawns = new();
    public DungeonEnemyUnitConfig bossSpawn = new();

    [Header("Milestone Rewards (vòng 5/10/15/20)")]
    public List<DungeonMilestoneReward> milestoneRewards = new();

    [Header("Final Completion Rewards (sau vòng cuối)")]
    public List<DungeonRewardItemConfig> completionRewards = new();
}

[Serializable]
public class DungeonMilestoneReward
{
    [Tooltip("Clear đủ vòng này thì nhận reward.")]
    public int atWave = 5;
    public long bonusExp  = 0;
    public long bonusGold = 0;
    public List<DungeonRewardItemConfig> items = new();
}

[Serializable]
public class DungeonEnemyUnitConfig
{
    public int enemyId;
    public string displayName = string.Empty;
    public Vector3 spawnPosition = Vector3.zero;
    public int maxHp = 100;
    public int maxMp = 0;
    public int attack = 10;
    public int defense = 0;
    public int expReward = 0;
    public int level = 1;
    public int respawnTime = 30;
    public float moveSpeed = 2f;
    public bool canFly;
    public string elementType = "None";
    public List<DropItemEntry> drops = new();
}

[Serializable]
public class DungeonRewardItemConfig
{
    public int itemTemplateId;
    public int quantity = 1;
    public int upgradeLevel = 0;
    public string strOptions = string.Empty;
}