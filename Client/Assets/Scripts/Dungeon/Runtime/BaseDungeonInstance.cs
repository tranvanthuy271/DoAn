using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseDungeonInstance : NetworkBehaviour
{
    [Header("Common UI")]
    [SerializeField] protected TMP_Text countdownText;
    [SerializeField] protected TMP_Text statusText;

    private Coroutine _localReturnCoroutine;
    protected int ForcedMapId { get; private set; } = -1;
    protected int ForcedZoneId { get; private set; } = 0;

    protected void SetEncounterLocation(int mapId, int zoneId)
    {
        ForcedMapId = mapId;
        ForcedZoneId = zoneId;
    }

    protected void ClearEncounterLocation()
    {
        ForcedMapId = -1;
        ForcedZoneId = 0;
    }

    protected NetworkObject SpawnConfiguredEnemy(DungeonEnemyUnitConfig config, float scale, bool isBoss)
    {
        if (!IsServer || config == null)
            return null;

        Debug.Log($"[BaseDungeonInstance] SpawnConfiguredEnemy: scene={gameObject.scene.name}, enemyId={config.enemyId}, configMaxHp={config.maxHp}, scale={scale:F2}, isBoss={isBoss}, map={ResolveCurrentMapId()}, zone={ResolveCurrentZoneId()}");

        GameObject prefab = EnemyPrefabManager.Instance != null
            ? EnemyPrefabManager.Instance.GetEnemyPrefab(config.enemyId)
            : null;
        if (prefab == null)
        {
            Debug.LogWarning($"[BaseDungeonInstance] Không tìm thấy prefab cho enemyId={config.enemyId}. Kiểm tra EnemyPrefabManager.");
            return null;
        }

        GameObject enemyObject = Instantiate(prefab, config.spawnPosition, Quaternion.identity);
        NetworkObject networkObject = enemyObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"[BaseDungeonInstance] Prefab {prefab.name} thiếu NetworkObject component.");
            Destroy(enemyObject);
            return null;
        }

        // *** QUAN TRỌNG: Apply stats VÀ set layer TRƯỚC Spawn() ***
        //
        // Lý do 1 (HP): OnNetworkSpawn() của NetworkEnemyHealth dùng field `maxHealth`
        // (SerializeField) để set networkCurrentHealth ban đầu. Nếu apply sau Spawn, enemy
        // sẽ có maxHealth=10 (inspector default), có thể chết ngay khi Player đánh lần đầu.
        // PreInitMaxHp() đặt đúng maxHealth TRƯỚC khi Spawn() chạy OnNetworkSpawn().
        //
        // Lý do 2 (Layer): NGO KHÔNG sync layer property. Nếu set layer sau Spawn, client
        // nhận NetworkObject với layer của prefab (thường là "Default"). Physics2D.OverlapCircleAll
        // dùng layer của collider — nếu layer sai, đòn đánh không detect được enemy.
        // NetworkEnemyHealth.OnNetworkSpawn() cũng tự set "Enemy" layer trên mọi client.

        var runtimeStats = enemyObject.GetComponent<DungeonEnemyRuntimeStats>()
            ?? enemyObject.AddComponent<DungeonEnemyRuntimeStats>();
        runtimeStats.Apply(config, scale, isBoss);
        runtimeStats.ApplyDrops(config.drops);
        Debug.Log($"[BaseDungeonInstance] runtimeStats.MaxHp={runtimeStats.MaxHp} sau Apply() (isBoss={isBoss}) scene={gameObject.scene.name} prefab={prefab.name}");

        // Pre-set maxHealth trên NetworkEnemyHealth TRƯỚC khi Spawn()
        var networkEnemyHealth = enemyObject.GetComponent<NetworkEnemyHealth>();
        if (networkEnemyHealth != null && runtimeStats.MaxHp > 0)
            networkEnemyHealth.PreInitMaxHp(runtimeStats.MaxHp);

        // Set layer trước Spawn — NGO cũng set lại trong OnNetworkSpawn() trên client
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex >= 0)
            SetLayerRecursively(enemyObject, enemyLayerIndex);

        int currentMapId = ResolveCurrentMapId();
        int currentZoneId = ResolveCurrentZoneId();
        if (currentMapId >= 0)
        {
            MapSceneManager.Instance?.MoveToMapScene(enemyObject, currentMapId);
            ApplyMapVisibility(enemyObject, currentMapId, currentZoneId);
        }
        else
        {
            Debug.LogWarning($"[BaseDungeonInstance] Không resolve được mapId cho scene '{gameObject.scene.name}'. Enemy sẽ spawn ở physics scene mặc định.");
        }

        networkObject.Spawn();
        Debug.Log($"[BaseDungeonInstance] Enemy spawned: NetworkObjectId={networkObject.NetworkObjectId}, layer={LayerMask.LayerToName(enemyObject.layer)}, HP={runtimeStats.MaxHp}");

        return networkObject;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }

    private static void ApplyMapVisibility(GameObject enemyObj, int targetMapId, int targetZoneId)
    {
        var zoneTag = enemyObj.GetComponent<ZoneOwnerTag>() ?? enemyObj.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(targetMapId, targetZoneId);

        var filter = enemyObj.GetComponent<NetworkVisibilityZoneFilter>() ?? enemyObj.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }

    protected int ResolveCurrentMapId()
    {
        if (ForcedMapId >= 0)
            return ForcedMapId;

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        MapWorldConfig worldConfig = ZoneRoomRegistry.Instance?.Config;
        if (worldConfig?.maps != null)
        {
            foreach (var mapDef in worldConfig.maps)
            {
                if (mapDef != null && string.Equals(mapDef.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                    return mapDef.mapId;
            }
        }

        var dungeonManager = FindAnyObjectByType<DungeonManager>();
        if (dungeonManager != null && dungeonManager.ActiveDungeonMapId >= 0)
            return dungeonManager.ActiveDungeonMapId;

        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentMapId >= 0)
            return ClientSceneController.Instance.CurrentMapId;

        if (MapManager.Instance != null && MapManager.Instance.GetMapId() >= 0)
            return MapManager.Instance.GetMapId();

        return -1;
    }

    protected int ResolveCurrentZoneId()
    {
        if (ForcedMapId >= 0)
            return ForcedZoneId;

        var dungeonManager = FindAnyObjectByType<DungeonManager>();
        if (dungeonManager != null && dungeonManager.ActiveDungeonMapId >= 0)
            return dungeonManager.ActiveDungeonZoneId;

        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentZoneId >= 0)
            return ClientSceneController.Instance.CurrentZoneId;

        return 0;
    }

    protected void BroadcastStatus(string message)
    {
        Debug.Log($"[BaseDungeonInstance] Status: {message}");

        if (!IsServer)
            return;

        ZoneTransitionController controller = FindAnyObjectByType<ZoneTransitionController>();
        if (controller == null)
        {
            Debug.LogWarning($"[BaseDungeonInstance] ZoneTransitionController not found. Cannot broadcast status '{message}'.");
            return;
        }

        controller.BroadcastDungeonStatusToZone(ResolveCurrentMapId(), ResolveCurrentZoneId(), message ?? string.Empty);
    }

    protected IEnumerator GrantRewardsToAll(IReadOnlyList<DungeonRewardItemConfig> rewards)
    {
        if (rewards == null || rewards.Count == 0 || NetworkManager.Singleton == null)
            yield break;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            yield return DungeonRewardGrantService.GrantRewardsToClient(client.ClientId, rewards);
    }

    protected IEnumerator BeginReturnFlow(bool completed, float countdownSeconds, int returnMapId, string returnSceneName)
    {
        int seconds = Mathf.Max(1, Mathf.CeilToInt(countdownSeconds));
        ZoneTransitionController controller = FindAnyObjectByType<ZoneTransitionController>();
        if (controller != null)
        {
            controller.BeginDungeonReturnFlowToZone(
                ResolveCurrentMapId(),
                ResolveCurrentZoneId(),
                completed,
                seconds,
                returnMapId,
                string.IsNullOrWhiteSpace(returnSceneName) ? "GameScene" : returnSceneName);
        }
        else
        {
            Debug.LogWarning($"[BaseDungeonInstance] ZoneTransitionController not found. Cannot begin return flow.");
        }

        yield return new WaitForSeconds(seconds);
    }

    [ClientRpc]
    private void SetStatusClientRpc(string message)
    {
        if (statusText == null)
            return;

        bool hasMessage = !string.IsNullOrWhiteSpace(message);
        statusText.gameObject.SetActive(hasMessage);
        statusText.text = message;
    }

    [ClientRpc]
    private void BeginReturnCountdownClientRpc(string prefix, int countdownSeconds, int returnMapId, string returnSceneName)
    {
        if (_localReturnCoroutine != null)
            StopCoroutine(_localReturnCoroutine);
        _localReturnCoroutine = StartCoroutine(LocalReturnCountdownCoroutine(prefix, countdownSeconds, returnMapId, returnSceneName));
    }

    private IEnumerator LocalReturnCountdownCoroutine(string prefix, int countdownSeconds, int returnMapId, string returnSceneName)
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int remaining = countdownSeconds; remaining > 0; remaining--)
        {
            if (countdownText != null)
                countdownText.text = $"{prefix}: {remaining}s";
            yield return new WaitForSeconds(1f);
        }

        PlayerPrefs.SetInt("SelectedMapId", returnMapId);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.ExitDungeon(returnMapId);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(string.IsNullOrWhiteSpace(returnSceneName) ? "GameScene" : returnSceneName);
    }
}
