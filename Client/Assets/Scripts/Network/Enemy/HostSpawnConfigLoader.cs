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
    [Tooltip("Map ID cần load config. Để 0 sẽ tự lấy từ MapManager.Instance.")]
    public int mapId = 0;

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

    // Drop lookup: enemy_id → danh sách DropItemEntry đã validate
    private Dictionary<int, List<DropItemEntry>> _dropLookup
        = new Dictionary<int, List<DropItemEntry>>();

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
        if (string.IsNullOrWhiteSpace(apiBaseURL)) apiBaseURL = ServerAddressConfig.Instance.ApiUrl;
        // Lấy mapId từ MapManager nếu chưa set
        if (mapId == 0 && MapManager.Instance != null)
            mapId = MapManager.Instance.GetMapId();

        StartCoroutine(LoadAndApplyConfig());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Main coroutine
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator LoadAndApplyConfig()
    {
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

        // Validate + build drop lookup trước khi spawn bất kỳ enemy nào
        BuildDropLookup(response.drops);

        // Build skill lookup (từ enemy_skills trong response)
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
        if (e.count <= 0)      e.count        = 1;
        if (e.respawn_time <= 0) e.respawn_time = 30;
        if (e.exp < 0)         e.exp          = 0;
        // hp=0 → EnemyStatOverride sẽ fallback về prefab default
    }

    /// <summary>
    /// Validate + build dictionary {enemy_id → EnemySkillsEntry} từ EnemySkillsEntry[].
    /// Bỏ qua entry thiếu skills hoặc skills rỗng.
    /// </summary>
    private void BuildSkillLookup(EnemySkillsEntry[] enemySkills)
    {
        _skillLookup.Clear();
        if (enemySkills == null) return;

        foreach (var entry in enemySkills)
        {
            if (entry.enemy_id <= 0) continue;
            if (entry.skills == null || entry.skills.Length == 0) continue;
            _skillLookup[entry.enemy_id] = entry;
        }

        Debug.Log($"[HostSpawnConfigLoader] Skill lookup built: {_skillLookup.Count} enemy types có skills.");
    }

    /// <summary>
    /// Validate + build dictionary {enemy_id → List&lt;DropItemEntry&gt;} từ DropEntry[].
    /// Clamp rate về [0,1], đảm bảo qty_min ≤ qty_max.
    /// </summary>
    private void BuildDropLookup(DropEntry[] drops)
    {
        _dropLookup.Clear();
        if (drops == null) return;

        foreach (var dropEntry in drops)
        {
            if (dropEntry.enemy_id <= 0 || dropEntry.items == null) continue;

            var validatedItems = new List<DropItemEntry>();
            foreach (var item in dropEntry.items)
            {
                if (item.item_id <= 0)
                {
                    Debug.LogWarning($"[HostSpawnConfigLoader] Drop rule enemy_id={dropEntry.enemy_id}: item_id={item.item_id} không hợp lệ → bỏ qua item này.");
                    continue;
                }

                // Clamp rate
                if (item.rate < 0f || item.rate > 1f)
                {
                    Debug.LogWarning($"[HostSpawnConfigLoader] Drop rule enemy_id={dropEntry.enemy_id}, item_id={item.item_id}: rate={item.rate} ngoài [0,1] → clamp.");
                    item.rate = Mathf.Clamp01(item.rate);
                }

                // Fix qty range
                if (item.qty_min < 1) item.qty_min = 1;
                if (item.qty_max < item.qty_min) item.qty_max = item.qty_min;

                validatedItems.Add(item);
            }

            if (validatedItems.Count > 0)
                _dropLookup[dropEntry.enemy_id] = validatedItems;
        }

        Debug.Log($"[HostSpawnConfigLoader] Drop lookup built: {_dropLookup.Count} enemy types có drop rules.");
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
        if (prefab == null) return; // đã validate, không nên xảy ra

        _dropLookup.TryGetValue(entry.enemy_id, out var drops);
        _skillLookup.TryGetValue(entry.enemy_id, out var skillsEntry);

        for (int i = 0; i < entry.count; i++)
        {
            Vector3 pos = CalculateSpawnPosition(entry.cx, entry.cy, i, entry.count);
            SpawnSingleEnemy(prefab, pos, entry, drops, skillsEntry);
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
        List<DropItemEntry> drops, EnemySkillsEntry skillsEntry)
    {
        GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);

        NetworkObject netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[HostSpawnConfigLoader] Prefab enemy_id={entry.enemy_id} thiếu NetworkObject component!");
            Destroy(enemyObj);
            return;
        }

        netObj.Spawn();

        // Gắn hoặc lấy EnemyStatOverride rồi apply
        EnemyStatOverride statOverride = enemyObj.GetComponent<EnemyStatOverride>();
        if (statOverride == null)
            statOverride = enemyObj.AddComponent<EnemyStatOverride>();

        statOverride.Apply(
            entry.hp,
            entry.exp,
            entry.is_boss,
            entry.respawn_time,
            entry.level,
            skillsEntry != null ? skillsEntry.enemy_name : ""
        );

        // Set drop rules
        if (drops != null && drops.Count > 0)
        {
            EnemyItemDrop itemDrop = enemyObj.GetComponent<EnemyItemDrop>();
            if (itemDrop != null)
                itemDrop.SetDropsFromConfig(drops);
            else
                Debug.LogWarning($"[HostSpawnConfigLoader] enemy_id={entry.enemy_id}: EnemyItemDrop component không tồn tại trên prefab!");
        }
        else
        {
            Debug.Log($"[HostSpawnConfigLoader] enemy_id={entry.enemy_id}: không có drop rules trong config → enemy này sẽ không drop item.");
        }

        // Set skills — áp dụng cho cả EnemyAI thường lẫn BossAI
        if (skillsEntry != null && skillsEntry.skills != null && skillsEntry.skills.Length > 0)
        {
            EnemySkillSet skillSet = enemyObj.GetComponent<EnemySkillSet>();
            if (skillSet == null)
                skillSet = enemyObj.AddComponent<EnemySkillSet>();
            skillSet.SetSkillsFromConfig(skillsEntry);
        }

        _totalSpawned++;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Fallback
    // ─────────────────────────────────────────────────────────────────────

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
