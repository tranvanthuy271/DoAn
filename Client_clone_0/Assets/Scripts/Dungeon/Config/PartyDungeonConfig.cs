using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonPartyConfig", menuName = "DoAn/Dungeon/Party Config")]
public class PartyDungeonConfig : ScriptableObject
{
    [Header("Identity")]
    public int dungeonId = 2;
    public int mapId = 111;
    public string returnSceneName = "GameScene";
    public int returnMapId = 0;

    [Header("Flow")]
    public float returnCountdownSeconds = 5f;

    [Header("Encounter")]
    public List<DungeonEnemyUnitConfig> enemySpawns = new();
    public DungeonEnemyUnitConfig bossSpawn = new();

    [Header("Completion Rewards")]
    public List<DungeonRewardItemConfig> completionRewards = new();
}
