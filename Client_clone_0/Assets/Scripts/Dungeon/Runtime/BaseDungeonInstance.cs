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

    protected NetworkObject SpawnConfiguredEnemy(DungeonEnemyUnitConfig config, float scale, bool isBoss)
    {
        if (!IsServer || config == null)
            return null;

        Debug.Log($"[BaseDungeonInstance] SpawnConfiguredEnemy: enemyId={config.enemyId}, configMaxHp={config.maxHp}, scale={scale:F2}, isBoss={isBoss}");

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
        Debug.Log($"[BaseDungeonInstance] runtimeStats.MaxHp={runtimeStats.MaxHp} sau Apply() (isBoss={isBoss})");

        // Pre-set maxHealth trên NetworkEnemyHealth TRƯỚC khi Spawn()
        var networkEnemyHealth = enemyObject.GetComponent<NetworkEnemyHealth>();
        if (networkEnemyHealth != null && runtimeStats.MaxHp > 0)
            networkEnemyHealth.PreInitMaxHp(runtimeStats.MaxHp);

        // Set layer trước Spawn — NGO cũng set lại trong OnNetworkSpawn() trên client
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex >= 0)
            SetLayerRecursively(enemyObject, enemyLayerIndex);

        int currentMapId = ResolveCurrentMapId();
        if (currentMapId >= 0)
        {
            MapSceneManager.Instance?.MoveToMapScene(enemyObject, currentMapId);
            ApplyMapVisibility(enemyObject, currentMapId);
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

    private static void ApplyMapVisibility(GameObject enemyObj, int targetMapId)
    {
        var zoneTag = enemyObj.GetComponent<ZoneOwnerTag>() ?? enemyObj.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(targetMapId, 0);

        var filter = enemyObj.GetComponent<NetworkVisibilityZoneFilter>() ?? enemyObj.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }

    protected int ResolveCurrentMapId()
    {
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

    protected void BroadcastStatus(string message)
    {
        SetStatusClientRpc(message ?? string.Empty);
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
        string prefix = completed ? "Hoàn thành! Trở về sau" : "Thất bại! Trở về sau";
        BeginReturnCountdownClientRpc(prefix, seconds, returnMapId, string.IsNullOrWhiteSpace(returnSceneName) ? "GameScene" : returnSceneName);
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