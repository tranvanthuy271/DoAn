using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PartyDungeonRuntime : BaseDungeonInstance
{
    [Header("Config")]
    [SerializeField] private PartyDungeonConfig config;

    private readonly List<NetworkObject> _aliveEnemies = new();
    private NetworkObject _bossObject;
    private bool _bossSpawned;
    private bool _completed;
    private int _activeDungeonId;
    private int _activeMapId;
    private int _activeZoneId;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer || config == null)
            return;

        BeginEncounter(config.dungeonId, config.mapId, 0);
    }

    public void BeginEncounter(int dungeonConfigId, int mapId, int zoneId)
    {
        if (!IsServer || config == null)
            return;

        _activeDungeonId = dungeonConfigId > 0 ? dungeonConfigId : config.dungeonId;
        _activeMapId = mapId >= 0 ? mapId : config.mapId;
        _activeZoneId = Mathf.Max(0, zoneId);

        DespawnTrackedEnemies();
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        _aliveEnemies.Clear();
        _bossObject = null;
        _bossSpawned = false;
        _completed = false;

        SetEncounterLocation(_activeMapId, _activeZoneId);
        Debug.Log($"[PartyDungeonRuntime] SpawnEnemies: dungeonId={_activeDungeonId} mapId={_activeMapId} zoneId={_activeZoneId} enemyCount={config.enemySpawns?.Count ?? 0}");

        foreach (var enemyConfig in config.enemySpawns)
        {
            NetworkObject enemy = SpawnConfiguredEnemy(enemyConfig, 1f, false);
            RegisterEnemy(enemy, false);
        }

        if (_aliveEnemies.Count == 0)
        {
            Debug.Log("[PartyDungeonRuntime] Không có minion — spawn boss ngay.");
            SpawnBoss();
        }
        else
        {
            BroadcastStatus("Tiêu diệt toàn bộ quái vật để gọi Boss.");
        }
    }

    private void RegisterEnemy(NetworkObject networkObject, bool isBoss)
    {
        if (networkObject == null)
            return;

        if (!isBoss)
            _aliveEnemies.Add(networkObject);
        else
            _bossObject = networkObject;

        UnityAction handler = null;
        if (networkObject.TryGetComponent<NetworkEnemyHealth>(out var networkEnemyHealth))
        {
            handler = () =>
            {
                if (!IsServer)
                    return;
                networkEnemyHealth.OnDeath.RemoveListener(handler);
                HandleEnemyDeath(networkObject, isBoss);
            };
            networkEnemyHealth.OnDeath.AddListener(handler);
            return;
        }

        if (networkObject.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            handler = () =>
            {
                if (!IsServer)
                    return;
                enemyHealth.OnDeath.RemoveListener(handler);
                HandleEnemyDeath(networkObject, isBoss);
            };
            enemyHealth.OnDeath.AddListener(handler);
        }
    }

    private void HandleEnemyDeath(NetworkObject networkObject, bool isBoss)
    {
        if (_completed)
            return;

        if (!isBoss)
        {
            _aliveEnemies.Remove(networkObject);
            if (_aliveEnemies.Count == 0 && !_bossSpawned)
                SpawnBoss();
            return;
        }

        StartCoroutine(CompleteDungeonCoroutine());
    }

    private void SpawnBoss()
    {
        _bossSpawned = true;
        SetEncounterLocation(_activeMapId, _activeZoneId);
        Debug.Log($"[PartyDungeonRuntime] SpawnBoss: dungeonId={_activeDungeonId} mapId={_activeMapId} zoneId={_activeZoneId} bossEnemyId={config.bossSpawn?.enemyId}");
        NetworkObject boss = SpawnConfiguredEnemy(config.bossSpawn, 1f, true);
        RegisterEnemy(boss, true);
        BroadcastStatus("Boss đã xuất hiện.");
    }

    private void DespawnTrackedEnemies()
    {
        foreach (NetworkObject enemy in _aliveEnemies)
            DespawnTrackedObject(enemy);

        DespawnTrackedObject(_bossObject);
        _aliveEnemies.Clear();
        _bossObject = null;
    }

    private static void DespawnTrackedObject(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        if (networkObject.IsSpawned)
        {
            networkObject.Despawn(true);
            return;
        }

        if (networkObject.gameObject != null)
            UnityEngine.Object.Destroy(networkObject.gameObject);
    }

    private IEnumerator CompleteDungeonCoroutine()
    {
        if (_completed)
            yield break;

        _completed = true;
        BroadcastStatus("Hoàn thành phó bản. Đang phát thưởng.");
        yield return GrantRewardsToAll(config.completionRewards);
        yield return BeginReturnFlow(true, config.returnCountdownSeconds, config.returnMapId, config.returnSceneName);
    }
}
