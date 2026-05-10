using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HostSpawnConfigLoader — Fetch cấu hình spawn enemy từ DB qua API,
/// validate toàn bộ entries, rồi spawn enemy với thông số ghi đè (HP, EXP, is_boss, drops).
///
/// CHỈ CHẠY TRÊN HOST/SERVER — mọi logic đều guard bởi IsServer check.
/// Thứ tự hoạt động:
///   1. OnNetworkSpawn() (IsServer=true) → StartCoroutine(LoadAndApplyConfig)
///   2. Fetch GET /api/map/{mapId}/spawn-config
///   3. Validate từng SpawnEntry + DropEntry
///   4. Với mỗi SpawnEntry: spawn enemy, apply EnemyStatOverride, set drops
///   5. Fire OnSpawnComplete event
///   6. Nếu API trả về rỗng hoặc lỗi → fallback NetworkEnemySpawner (cũ)
///
/// Gắn component này vào cùng GameObject với NetworkEnemySpawner trong HostScene.
/// </summary>
public class HostSpawnConfigLoader : NetworkBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  Inspector fields
    // ─────────────────────────────────────────────────────────────────────

    [Header("API")]
    [Tooltip("Base URL của GameServerApi. Ví dụ: http://localhost:5000/api")]
    public string apiBaseURL = "";

    [Header("Map")]
    [Tooltip("Map ID cần load config. Để -1 sẽ tự lấy từ MapManager.Instance.")]
    public int mapId = -1;

    [Header("References")]
    [Tooltip("EnemyPrefabManager để lấy prefab theo enemy_id")]
    public EnemyPrefabManager enemyPrefabManager;

    [Tooltip("NetworkEnemySpawner để fallback nếu không có spawn-config trong DB")]
    public NetworkEnemySpawner fallbackSpawner;

    [Header("Settings")]
    [Tooltip("Nếu true: khi spawn-config rỗng sẽ gọi fallbackSpawner thay vì bỏ qua")]
    public bool fallbackToOldSpawner = true;

    [Tooltip("Offset ngẫu nhiên (world units) áp dụng cho mỗi enemy trong cùng điểm spawn khi count > 1")]
    public float multiSpawnSpreadRadius = 0.8f;

    [Header("Events")]
    [Tooltip("Fired khi tất cả enemy đã spawn xong. Tham số: số lượng spawned.")]
    public UnityEvent<int> OnSpawnComplete;

    [Tooltip("Fired khi API lỗi hoặc data không hợp lệ — trước khi fallback.")]
    public UnityEvent<string> OnSpawnError;

    // ─────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────

    // Skills lookup: enemy_id → EnemySkillsEntry đã validate
    private Dictionary<int, EnemySkillsEntry> _skillLookup
        = new Dictionary<int, EnemySkillsEntry>();

    private int _totalSpawned = 0;
    private bool _started = false;

    // ─────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return; // Clients không làm gì cả

        if (_started) return;
        _started = true;
        apiBaseURL = ServerAddressConfig.Instance.ResolveApiUrl(apiBaseURL);
        // Lấy mapId từ MapManager nếu đang ở chế độ auto-detect
        if (mapId < 0 && MapManager.Instance != null)
            mapId = MapManager.Instance.GetMapId();

        StartCoroutine(LoadAndApplyConfig());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Main coroutine
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator LoadAndApplyConfig()
    {
        // WaveDungeonRuntime manages all enemy spawning in wave dungeons.
        // Spawning from map_spawn_config would duplicate enemies and immediately
        // place the boss (is_boss=true entry) before the wave clears regular enemies.
        if (FindObjectsOfType<WaveDungeonRuntime>(includeInactive: true).Length > 0
            || FindObjectsOfType<PartyDungeonRuntime>(includeInactive: true).Length > 0)
        {
            Debug.Log("[HostSpawnConfigLoader] Dungeon runtime detected (includeInactive) — skipping spawn config because runtime manages all spawning.");
            yield break;
        }

        string url = $"{apiBaseURL}/map/{mapId}/spawn-config";
        Debug.Log($"[HostSpawnConfigLoader] Fetching spawn config: {url}");

        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string errMsg = $"API error: {request.error}";
            Debug.LogError($"[HostSpawnConfigLoader] {errMsg}");
            OnSpawnError?.Invoke(errMsg);
            TryFallback();
            yield break;
        }

        MapSpawnConfigResponse response;
        try
        {
            response = JsonUtility.FromJson<MapSpawnConfigResponse>(request.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            string errMsg = $"JSON parse error: {ex.Message}";
            Debug.LogError($"[HostSpawnConfigLoader] {errMsg}");
            OnSpawnError?.Invoke(errMsg);
            TryFallback();
            yield break;
        }

        if (response == null
            || response.spawns == null
            || response.spawns.Length == 0)
        {
            Debug.LogWarning($"[HostSpawnConfigLoader] Không có spawn config cho map {mapId}. Fallback...");
            TryFallback();
            yield break;
        }

        // Build skill lookup (bao gồm cả drops và reward từ enemy_skills)
        BuildSkillLookup(response.enemy_skills);

        // Spawn tất cả enemies
        int totalEntries = response.spawns.Length;
        for (int i = 0; i < totalEntries; i++)
        {
            SpawnEntry entry = response.spawns[i];

            if (!ValidateSpawnEntry(entry, i))
                continue;

            ApplySpawnEntryDefaults(entry);
            SpawnEnemyGroup(entry);

            // Yield mỗi 5 enemy để tránh freeze frame
            if (i % 5 == 4)
                yield return null;
        }

        Debug.Log($"[HostSpawnConfigLoader] Spawn hoàn tất: {_totalSpawned} enemy từ {totalEntries} entries.");
        OnSpawnComplete?.Invoke(_totalSpawned);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Validation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Validate một SpawnEntry. Trả về false nếu phải bỏ qua.</summary>
    private bool ValidateSpawnEntry(SpawnEntry e, int index)
    {
        if (e.enemy_id <= 0)
        {
            Debug.LogError($"[HostSpawnConfigLoader] spawns[{index}]: enemy_id={e.enemy_id} không hợp lệ → bỏ qua.");
            return false;
        }

        if (enemyPrefabManager != null
            && enemyPrefabManager.GetEnemyPrefab(e.enemy_id) == null)
        {
            Debug.LogWarning($"[HostSpawnConfigLoader] spawns[{index}]: Không tìm thấy prefab cho enemy_id={e.enemy_id} → bỏ qua.");
            return false;
        }

        if (e.cx == 0f && e.cy == 0f)
        {
            Debug.LogWarning($"[HostSpawnConfigLoader] spawns[{index}]: enemy_id={e.enemy_id} có cx=0, cy=0 (vị trí gốc thế giới). Kiểm tra lại DB config.");
            // Không skip — vẫn spawn nhưng cảnh báo
        }

        return true;
    }

    /// <summary>Áp dụng giá trị mặc định nếu field bằng 0 / âm.</summary>
    private void ApplySpawnEntryDefaults(SpawnEntry e)
    {
        if (e.count <= 0)        e.count        = 1;
        if (e.respawn_time <= 0) e.respawn_time = 30;
    }

    /// <summary>
    /// Validate + build dictionary {enemy_id → EnemySkillsEntry} từ EnemySkillsEntry[].
    /// EnemySkillsEntry cũng chứa base_hp, exp_reward, drops — dùng cho spawn và drop setup.
    /// </summary>
    private void BuildSkillLookup(EnemySkillsEntry[] enemySkills)
    {
        _skillLookup.Clear();
        if (enemySkills == null) return;

        foreach (var entry in enemySkills)
        {
            if (entry.enemy_id <= 0) continue;
            _skillLookup[entry.enemy_id] = entry;
        }

        Debug.Log($"[HostSpawnConfigLoader] Skill/reward lookup built: {_skillLookup.Count} enemy types.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Spawning
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Spawn `entry.count` enemy tại (cx, cy) với spread nhỏ nếu count > 1.</summary>
    private void SpawnEnemyGroup(SpawnEntry entry)
    {
        if (enemyPrefabManager == null)
        {
            Debug.LogError("[HostSpawnConfigLoader] EnemyPrefabManager chưa được gán!");
            return;
        }

        GameObject prefab = enemyPrefabManager.GetEnemyPrefab(entry.enemy_id);
        if (prefab == null) return;

        _skillLookup.TryGetValue(entry.enemy_id, out var skillsEntry);

        for (int i = 0; i < entry.count; i++)
        {
            Vector3 pos = CalculateSpawnPosition(entry.cx, entry.cy, i, entry.count);
            SpawnSingleEnemy(prefab, pos, entry, skillsEntry);
        }
    }

    /// <summary>Tính vị trí spread cho nhiều enemy cùng điểm.</summary>
    private Vector3 CalculateSpawnPosition(float cx, float cy, int index, int total)
    {
        if (total <= 1)
            return new Vector3(cx, cy, 0f);

        // Phân tán theo vòng tròn nhỏ để tránh chồng nhau
        float angle = (360f / total) * index * Mathf.Deg2Rad;
        float radius = multiSpawnSpreadRadius;
        return new Vector3(
            cx + Mathf.Cos(angle) * radius,
            cy + Mathf.Sin(angle) * radius,
            0f);
    }

    /// <summary>Instantiate, Spawn qua Network, áp dụng override stats + drops + skills.</summary>
    private void SpawnSingleEnemy(GameObject prefab, Vector3 pos, SpawnEntry entry,
        EnemySkillsEntry skillsEntry)
    {
        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);
        bool watchBoss25 = entry.enemy_id == 25 || entry.is_boss || prefab.GetComponent<BossAI>() != null || enemyObj.name.Contains("Enemy 25");
        if (watchBoss25)
        {
            BossAI prefabBossAI = prefab.GetComponent<BossAI>();
            BossAI instanceBossAI = enemyObj.GetComponent<BossAI>();
            EnemyAI instanceEnemyAI = enemyObj.GetComponent<EnemyAI>();
            Debug.LogWarning(
                $"[BOSS25][HostSpawnConfigLoader] Instantiate enemy_id={entry.enemy_id} prefab={prefab.name} instance={enemyObj.name} pos={pos} mapId={mapId} entry.is_boss={entry.is_boss} skillsName='{(skillsEntry != null ? skillsEntry.enemy_name : "")}' prefabHasBossAI={(prefabBossAI != null)} prefabBossEnabled={(prefabBossAI != null && prefabBossAI.enabled)} instanceHasBossAI={(instanceBossAI != null)} instanceBossEnabled={(instanceBossAI != null && instanceBossAI.enabled)} instanceEnemyAIEnabled={(instanceEnemyAI != null && instanceEnemyAI.enabled)}",
                enemyObj);
        }

        NetworkObject netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[HostSpawnConfigLoader] Prefab enemy_id={entry.enemy_id} thiếu NetworkObject component!");
            Destroy(enemyObj);
            return;
        }

        // Di chuyển vào physics scene riêng của map — TRƯỚC Spawn()
        // Đảm bảo enemy ở đúng physics world, tránh cross-map collision
        MapSceneManager.Instance?.MoveToMapScene(enemyObj, mapId);

        // Map-based visibility: enemy chỉ visible cho player cùng map
        ApplyMapVisibility(enemyObj, mapId);
        netObj.Spawn();
        StartCoroutine(DelayedRefreshVisibility(enemyObj));

        // Gắn hoặc lấy EnemyStatOverride rồi apply
        EnemyStatOverride statOverride = enemyObj.GetComponent<EnemyStatOverride>();
        if (statOverride == null)
            statOverride = enemyObj.AddComponent<EnemyStatOverride>();

        // ✅ FIX: Ưu tiên override_hp/override_exp từ SpawnEntry (map_spawn_config legacy)
        // rồi mới dùng base_hp/exp_reward từ enemy_skills (bảng enemy).
        int baseHp    = entry.override_hp  > 0 ? entry.override_hp  : (skillsEntry != null ? skillsEntry.base_hp    : 0);
        int expReward = entry.override_exp > 0 ? entry.override_exp : (skillsEntry != null ? skillsEntry.exp_reward : 0);
        bool forceBossMode = entry.enemy_id == 25 || enemyObj.GetComponent<BossAI>() != null;
        bool effectiveIsBoss = entry.is_boss || forceBossMode;
        if (watchBoss25 && effectiveIsBoss != entry.is_boss)
        {
            Debug.LogWarning(
                $"[BOSS25][HostSpawnConfigLoader] Force boss mode for enemy_id={entry.enemy_id}. entry.is_boss={entry.is_boss} effectiveIsBoss={effectiveIsBoss}",
                enemyObj);
        }

        statOverride.Apply(
            baseHp,
            expReward,
            effectiveIsBoss,
            entry.respawn_time,
            entry.level,
            skillsEntry != null ? skillsEntry.enemy_name : ""
        );

        if (watchBoss25)
        {
            BossAI instanceBossAI = enemyObj.GetComponent<BossAI>();
            EnemyAI instanceEnemyAI = enemyObj.GetComponent<EnemyAI>();
            Debug.LogWarning(
                $"[BOSS25][HostSpawnConfigLoader] After statOverride enemy_id={entry.enemy_id} entry.is_boss={entry.is_boss} effectiveIsBoss={effectiveIsBoss} bossAIEnabled={(instanceBossAI != null && instanceBossAI.enabled)} enemyAIEnabled={(instanceEnemyAI != null && instanceEnemyAI.enabled)} netSpawned={netObj.IsSpawned} scene={enemyObj.scene.name}",
                enemyObj);
        }

        // Set drop rules từ enemy_skills (reward_json đã parse sẵn trên server)
        if (skillsEntry != null && skillsEntry.drops != null && skillsEntry.drops.Length > 0)
        {
            EnemyItemDrop itemDrop = enemyObj.GetComponent<EnemyItemDrop>();
            if (itemDrop != null)
            {
                var dropList = new List<DropItemEntry>(skillsEntry.drops);
                // Clamp/fix values
                foreach (var d in dropList)
                {
                    d.rate    = Mathf.Clamp01(d.rate);
                    if (d.qty_min < 1) d.qty_min = 1;
                    if (d.qty_max < d.qty_min) d.qty_max = d.qty_min;
                }
                itemDrop.SetDropsFromConfig(dropList);
            }
            else
            {
                Debug.LogWarning($"[HostSpawnConfigLoader] enemy_id={entry.enemy_id}: EnemyItemDrop component không tồn tại trên prefab!");
            }
        }
        else
        {
            Debug.Log($"[HostSpawnConfigLoader] enemy_id={entry.enemy_id}: không có drop rules trong reward_json.");
        }

        // Set skills
        if (skillsEntry != null && skillsEntry.skills != null && skillsEntry.skills.Length > 0)
        {
            EnemySkillSet skillSet = enemyObj.GetComponent<EnemySkillSet>();
            if (skillSet == null)
                skillSet = enemyObj.AddComponent<EnemySkillSet>();
            skillSet.SetSkillsFromConfig(skillsEntry);
        }

        // Ghi đè EnemyAI.damage từ base_damage của DB
        if (skillsEntry != null && skillsEntry.base_damage > 0)
        {
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.damage = skillsEntry.base_damage;

            BossAI bossAI = enemyObj.GetComponent<BossAI>();
            if (bossAI != null)
                bossAI.ApplyRuntimeOverride(skillsEntry.base_damage, 0f);
        }

        _totalSpawned++;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Fallback
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gắn map-based visibility: enemy visible cho TẤT CẢ player cùng map.
    /// </summary>
    private static void ApplyMapVisibility(GameObject enemyObj, int targetMapId)
    {
        var zoneTag = enemyObj.GetComponent<ZoneOwnerTag>() ?? enemyObj.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(targetMapId, 0);

        var filter = enemyObj.GetComponent<NetworkVisibilityZoneFilter>() ?? enemyObj.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }

    private IEnumerator DelayedRefreshVisibility(GameObject obj)
    {
        yield return null; // chờ 1 frame
        if (obj != null)
            obj.GetComponent<NetworkVisibilityZoneFilter>()?.RefreshVisibility();
    }

    private void TryFallback()
    {
        if (!fallbackToOldSpawner) return;

        if (fallbackSpawner == null)
            fallbackSpawner = GetComponent<NetworkEnemySpawner>();

        if (fallbackSpawner != null)
        {
            Debug.Log("[HostSpawnConfigLoader] Chạy NetworkEnemySpawner (fallback)...");
            // NetworkEnemySpawner tự check IsServer trong Start() của nó
            // Chỉ cần enable là nó tự load
            fallbackSpawner.enabled = true;
        }
        else
        {
            Debug.LogWarning("[HostSpawnConfigLoader] Không có fallbackSpawner — không spawn được enemy.");
        }
    }
}
