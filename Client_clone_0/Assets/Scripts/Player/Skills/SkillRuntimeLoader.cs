using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Load thống kê skill (cooldown, effectValue, mpCost) từ API ngay sau khi
/// NetworkObject spawn (Owner chạy — Host hoặc Client owner).
///
/// Cách hoạt động:
///   1. Sau khi IsOwner và IsSpawned, lấy player_id từ GameManager / PlayerPrefs.
///   2. Gọi APIClient.GetPlayerSkills(playerId, ...).
///   3. Duyệt từng PlayerSkillInfo trả về, tìm SkillData trong PlayerSkillManager
///      khớp skill_code → ghi đè cooldown / currentEffectValue / currentMpCost.
///   4. Nếu skill là WindStep, đồng thời ghi đè WindStepSkill.cooldown và dashDistance.
///   5. Nếu skill là Teleport (DASH), ghi đè TeleportSkill.cooldown.
///
/// Gắn phải: Cùng GameObject với PlayerSkillManager.
/// Yêu cầu:  APIClient singleton phải tồn tại và có token (được set sau login).
/// </summary>
[RequireComponent(typeof(PlayerSkillManager))]
public class SkillRuntimeLoader : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("Retry khi API fail (giây)")]
    [SerializeField] private float retryDelay = 3f;

    [Tooltip("Số lần retry tối đa")]
    [SerializeField] private int maxRetries = 3;

    // ── Internal ─────────────────────────────────────────────────────────────
    private PlayerSkillManager skillManager;
    private WindStepSkill windStepSkill;
    private TeleportSkill teleportSkill;
    private bool loaded = false;

    // ════════════════════════════════════════════════════════════════════════
    //  Network lifecycle
    // ════════════════════════════════════════════════════════════════════════

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Chỉ owner mới load — server (host) và client owner đều chạy riêng
        Debug.Log($"[SkillRuntimeLoader] OnNetworkSpawn | IsOwner={IsOwner} | IsServer={IsServer} | go={gameObject.name}");
        if (!IsOwner) return;

        skillManager  = GetComponent<PlayerSkillManager>();
        windStepSkill = GetComponent<WindStepSkill>() ?? GetComponentInParent<WindStepSkill>();
        teleportSkill = GetComponent<TeleportSkill>() ?? GetComponentInParent<TeleportSkill>();

        Debug.Log($"[SkillRuntimeLoader] skillManager={skillManager != null} | IsOwner={IsOwner}");
        StartCoroutine(WaitAndLoad());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Load logic
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator WaitAndLoad()
    {
        // Đợi APIClient sẵn sàng (có thể chưa init ngay khi spawn)
        float waited = 0f;
        while (APIClient.Instance == null && waited < 5f)
        {
            yield return new WaitForSeconds(0.2f);
            waited += 0.2f;
        }

        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[SkillRuntimeLoader] APIClient.Instance không tìm thấy. Skill stats sẽ dùng giá trị Inspector.");
            yield break;
        }

        // Đợi player data load xong từ API/GameManager (race condition khi client mới spawn)
        // Timeout 10 giây để tránh chờ mãi nếu data không bao giờ có
        waited = 0f;
        while (GetPlayerId() <= 0 && waited < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            waited += 0.5f;
        }

        int playerId = GetPlayerId();
        string gmData = GameManager.Instance?.currentPlayerData != null
            ? GameManager.Instance.currentPlayerData.player_id.ToString() : "null";
        int ppId = PlayerPrefs.GetInt("PLAYER_ID", 0);
        Debug.Log($"[SkillRuntimeLoader] WaitAndLoad | playerId={playerId} | GameMgr.player_id={gmData} | PlayerPrefs.PLAYER_ID={ppId} | APIClient={APIClient.Instance != null}");
        if (playerId <= 0)
        {
            Debug.LogWarning($"[SkillRuntimeLoader] playerId={playerId} <= 0! Skill stats sẽ KHÔNG load từ DB. Kiểm tra: login trước khi spawn, GameManager.currentPlayerData, PlayerPrefs[PLAYER_ID].");
            yield break;
        }

        yield return StartCoroutine(LoadWithRetry(playerId));
    }

    private IEnumerator LoadWithRetry(int playerId)
    {
        int attempts = 0;
        while (!loaded && attempts < maxRetries)
        {
            attempts++;
            bool done = false;
            bool success = false;

            APIClient.Instance.GetPlayerSkills(playerId,
                response =>
                {
                    ApplySkillStats(response);
                    success = true;
                    done = true;
                },
                err =>
                {
                    Debug.LogWarning($"[SkillRuntimeLoader] Lần {attempts}: GetPlayerSkills lỗi — {err}");
                    done = true;
                }
            );

            // Đợi callback hoàn thành (tối đa 5 giây/lần)
            float t = 0f;
            while (!done && t < 5f) { yield return null; t += Time.deltaTime; }

            if (success) yield break;

            if (attempts < maxRetries)
                yield return new WaitForSeconds(retryDelay);
        }

        if (!loaded)
            Debug.LogWarning("[SkillRuntimeLoader] Không load được skill stats từ DB. Dùng giá trị Inspector.");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Apply
    // ════════════════════════════════════════════════════════════════════════

    private void ApplySkillStats(PlayerSkillsResponse response)
    {
        if (response?.skills == null || skillManager == null)
        {
            Debug.LogWarning("[SkillRuntimeLoader] response null hoặc skillManager null.");
            return;
        }

        // Build lookup theo skill_code
        var lookup = new Dictionary<string, PlayerSkillInfo>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var info in response.skills)
        {
            if (!string.IsNullOrEmpty(info.skill_code))
                lookup[info.skill_code] = info;
        }

        int matched = 0;

        // Duyệt SkillData trong PlayerSkillManager
        for (int i = 0; i < skillManager.GetSkillCount(); i++)
        {
            SkillData sd = skillManager.GetSkill(i);
            if (sd == null || string.IsNullOrEmpty(sd.skillCode)) continue;

            if (!TryGetPlayerSkillInfo(lookup, sd.skillCode, out PlayerSkillInfo info)) continue;

            // Apply stats từ DB — kể cả khi current_level=0 (dùng stats level 1 làm base)
            // Giúp skill luôn hiển thị đúng MP cost / cooldown ngay cả khi chưa nâng cấp

            // Ghi đè stats
            sd.cooldown           = info.current_cooldown_sec > 0 ? info.current_cooldown_sec : sd.cooldown;
            sd.currentEffectValue = info.current_effect_value;
            sd.currentMpCost      = info.current_mp_cost;

            // Đồng bộ sang component chuyên biệt nếu có
            if (sd.skillType == SkillType.WindStep && windStepSkill != null)
            {
                windStepSkill.cooldown      = sd.cooldown;
                windStepSkill.dashDistance  = info.current_effect_value; // effect_value = units di chuyển
            }
            else if (sd.skillType == SkillType.Teleport && teleportSkill != null)
            {
                teleportSkill.cooldown = sd.cooldown;
            }

            // Đồng bộ effectValue sang HybridSkillBase component nếu là hybrid skill
            if (info.current_effect_value > 0f)
            {
                foreach (var hc in GetComponents<HybridSkillBase>())
                {
                    if (string.Equals(hc.skillCode, sd.skillCode, System.StringComparison.OrdinalIgnoreCase))
                    {
                        hc.effectValue = info.current_effect_value;
                        hc.cooldown    = sd.cooldown;
                        hc.mpCost      = info.current_mp_cost;
                        break;
                    }
                }
            }

            matched++;
            Debug.Log($"[SkillRuntimeLoader] Applied '{sd.skillCode}' lv{info.current_level}: CD={sd.cooldown}s EV={sd.currentEffectValue} MP={sd.currentMpCost}");
        }

        loaded = true;
        Debug.Log($"[SkillRuntimeLoader] Load xong: {matched}/{skillManager.GetSkillCount()} skill đã apply từ DB.");
    }

    private bool TryGetPlayerSkillInfo(Dictionary<string, PlayerSkillInfo> lookup, string skillCode, out PlayerSkillInfo info)
    {
        if (lookup.TryGetValue(skillCode, out info))
            return true;

        // Fire+Earth hybrid đang có dữ liệu cũ ở DB với mã ERUPTION.
        // Cho phép client map song song để prefab/code mới vẫn load đúng runtime stats.
        if (string.Equals(skillCode, "HYBRID_FIRE_EARTH_LAVA_AURA", System.StringComparison.OrdinalIgnoreCase)
            && lookup.TryGetValue("HYBRID_EARTH_FIRE_ERUPTION", out info))
        {
            Debug.Log("[SkillRuntimeLoader] Alias match: HYBRID_FIRE_EARTH_LAVA_AURA -> HYBRID_EARTH_FIRE_ERUPTION");
            return true;
        }

        if (string.Equals(skillCode, "HYBRID_EARTH_FIRE_ERUPTION", System.StringComparison.OrdinalIgnoreCase)
            && lookup.TryGetValue("HYBRID_FIRE_EARTH_LAVA_AURA", out info))
        {
            Debug.Log("[SkillRuntimeLoader] Alias match: HYBRID_EARTH_FIRE_ERUPTION -> HYBRID_FIRE_EARTH_LAVA_AURA");
            return true;
        }

        // Kim Phong: prefab dùng HYBRID_METAL_WIND_BARRAGE, DB có thể lưu dưới HYBRID_METAL_WIND_GALE
        if (string.Equals(skillCode, "HYBRID_METAL_WIND_BARRAGE", System.StringComparison.OrdinalIgnoreCase)
            && lookup.TryGetValue("HYBRID_METAL_WIND_GALE", out info))
        {
            Debug.Log("[SkillRuntimeLoader] Alias match: HYBRID_METAL_WIND_BARRAGE -> HYBRID_METAL_WIND_GALE");
            return true;
        }

        if (string.Equals(skillCode, "HYBRID_METAL_WIND_GALE", System.StringComparison.OrdinalIgnoreCase)
            && lookup.TryGetValue("HYBRID_METAL_WIND_BARRAGE", out info))
        {
            Debug.Log("[SkillRuntimeLoader] Alias match: HYBRID_METAL_WIND_GALE -> HYBRID_METAL_WIND_BARRAGE");
            return true;
        }

        info = null;
        return false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private int GetPlayerId()
    {
        // 1. Từ GameManager (ưu tiên)
        if (GameManager.Instance?.currentPlayerData != null)
            return GameManager.Instance.currentPlayerData.player_id;

        // 2. Fallback PlayerPrefs
        int id = PlayerPrefs.GetInt("PLAYER_ID", 0);
        return id;
    }
}
