using System.Collections.Generic;
using UnityEngine;

public class DungeonEnemyRuntimeStats : MonoBehaviour
{
    public int MaxHp { get; private set; }
    public int MaxMp { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int Level { get; private set; }
    public float MoveSpeed { get; private set; }
    public bool IsBoss { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public bool HasRuntimeOverride { get; private set; }

    public void Apply(DungeonEnemyUnitConfig config, float scale, bool forceBoss)
    {
        if (config == null)
            return;

        scale = Mathf.Max(0.1f, scale);
        MaxHp = Mathf.Max(1, Mathf.RoundToInt(config.maxHp * scale));
        MaxMp = Mathf.Max(0, Mathf.RoundToInt(config.maxMp * scale));
        Attack = Mathf.Max(1, Mathf.RoundToInt(config.attack * scale));
        Defense = Mathf.Max(0, Mathf.RoundToInt(config.defense * scale));
        MoveSpeed = Mathf.Max(0.1f, config.moveSpeed);
        Level = Mathf.Max(1, config.level);
        IsBoss = forceBoss;
        DisplayName = string.IsNullOrWhiteSpace(config.displayName) ? gameObject.name : config.displayName;
        HasRuntimeOverride = true;

        var statOverride = GetComponent<EnemyStatOverride>() ?? gameObject.AddComponent<EnemyStatOverride>();
        statOverride.Apply(MaxHp, Mathf.Max(0, config.expReward), forceBoss, Mathf.Max(1, config.respawnTime), Level, DisplayName);

        var enemyAi = GetComponent<EnemyAI>();
        if (enemyAi != null)
            enemyAi.ApplyRuntimeOverride(Attack, MoveSpeed, config.canFly);

        var bossAi = GetComponent<BossAI>();
        if (bossAi != null)
            bossAi.ApplyRuntimeOverride(Attack, MoveSpeed);
    }

    public void ApplyDrops(List<DropItemEntry> drops)
    {
        if (drops == null || drops.Count == 0)
            return;

        var itemDrop = GetComponent<EnemyItemDrop>();
        itemDrop?.SetDropsFromConfig(drops);
    }

    public int ResolveIncomingDamage(int rawDamage)
    {
        return DamageCalculator.CalcDungeonEnemyReceivedDamage(rawDamage, Defense);
    }
}