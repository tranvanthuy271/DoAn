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

        // Đợi player data hoặc auth token xuất hiện để gameplay service có context.
        // Loader hiện dùng GameplayCommandService.GetPlayerSkillsServerRpc(), không còn phụ thuộc cứng vào PLAYER_ID.
        waited = 0f;
        while (GameManager.Instance?.currentPlayerData == null
            && string.IsNullOrWhiteSpace(PlayerPrefs.GetString("JWT_TOKEN", ""))
            && waited < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            waited += 0.5f;
        }

        int playerId = GetPlayerId();
        string gmData = GameManager.Instance?.currentPlayerData != null
            ? GameManager.Instance.currentPlayerData.player_id.ToString() : "null";
        int ppId = PlayerPrefs.GetInt("PLAYER_ID", 0);
        bool hasJwtToken = !string.IsNullOrWhiteSpace(PlayerPrefs.GetString("JWT_TOKEN", ""));
        Debug.Log($"[SkillRuntimeLoader] WaitAndLoad | playerId={playerId} | GameMgr.player_id={gmData} | PlayerPrefs.PLAYER_ID={ppId} | hasJwt={hasJwtToken} | APIClient={APIClient.Instance != null}");
        if (!hasJwtToken && GameManager.Instance?.currentPlayerData == null)
        {
            Debug.LogWarning("[SkillRuntimeLoader] Thiếu cả JWT_TOKEN lẫn GameManager.currentPlayerData. Skill stats sẽ KHÔNG load từ runtime service.");
            yield break;
        }

        yield return StartCoroutine(LoadWithRetry());
    }

    private IEnumerator LoadWithRetry()
    {
        int attempts = 0;
        while (!loaded && attempts < maxRetries)
        {
            attempts++;
            if (GameplayCommandService.Instance == null)
            {
                yield return new WaitForSeconds(retryDelay);
                continue;
            }

            bool done = false;
            bool success = false;

            GameplayCommandService.OnSkillsReceived -= HandleSkills;
            GameplayCommandService.OnSkillsReceived += HandleSkills;
            GameplayCommandService.Instance.GetPlayerSkillsServerRpc();

            void HandleSkills(string json)
            {
                GameplayCommandService.OnSkillsReceived -= HandleSkills;
                if (!json.Contains("\"error\""))
                {
                    var response = JsonUtility.FromJson<PlayerSkillsResponse>(json);
                    if (response != null) { ApplySkillStats(response); success = true; }
                }
                else
                    Debug.LogWarning($"[SkillRuntimeLoader] Lần {attempts}: GetPlayerSkills lỗi — {json}");
                done = true;
            }

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

        int playerFinalAtk = response.player_final_attack;

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
            sd.cooldown      = info.current_cooldown_sec > 0 ? info.current_cooldown_sec : sd.cooldown;
            sd.currentMpCost = info.current_mp_cost;

            // ── Tính tổng sát thương: skill base + player final attack ──────────────
            // Chỉ áp dụng cho các skill gây sát thương trực tiếp, không áp dụng cho
            // buff/utility skill nơi effect_value là khoảng cách / lượng buff.
            float effectValue = info.current_effect_value;
            if (IsDamageSkill(sd.skillType) && playerFinalAtk > 0)
            {
                effectValue += playerFinalAtk;
            }
            sd.currentEffectValue = effectValue;

            // Đồng bộ sang component chuyên biệt nếu có
            if (sd.skillType == SkillType.WindStep && windStepSkill != null)
            {
                windStepSkill.cooldown     = sd.cooldown;
                windStepSkill.dashDistance = info.current_effect_value; // effect_value = units di chuyển (KHÔNG cộng atk)
            }
            else if (sd.skillType == SkillType.Teleport && teleportSkill != null)
            {
                teleportSkill.cooldown = sd.cooldown;
            }

            // Đồng bộ effectValue sang HybridSkillBase component nếu là hybrid skill
            if (effectValue > 0f)
            {
                foreach (var hc in GetComponents<HybridSkillBase>())
                {
                    if (string.Equals(hc.skillCode, sd.skillCode, System.StringComparison.OrdinalIgnoreCase))
                    {
                        hc.effectValue = effectValue;
                        hc.cooldown    = sd.cooldown;
                        hc.mpCost      = info.current_mp_cost;
                        break;
                    }
                }
            }

            matched++;
            Debug.Log($"[SkillRuntimeLoader] Applied '{sd.skillCode}' lv{info.current_level}: CD={sd.cooldown}s base_EV={info.current_effect_value} atkBonus={playerFinalAtk} totalEV={effectValue} MP={sd.currentMpCost}");
        }

        loaded = true;
        Debug.Log($"[SkillRuntimeLoader] Load xong: {matched}/{skillManager.GetSkillCount()} skill, player_final_attack={playerFinalAtk}");
    }

    /// <summary>
    /// Trả về true nếu skill này gây sát thương trực tiếp và cần cộng player attack vào effectValue.
    /// Các skill buff/utility (dash distance, shield, armor buff, aura buff) trả về false.
    /// </summary>
    private static bool IsDamageSkill(SkillType type)
    {
        switch (type)
        {
            case SkillType.Projectile:
            case SkillType.Melee:
            case SkillType.NormalAttack:
            case SkillType.FireRain:
            case SkillType.WaterPillar:
            case SkillType.EarthBoomerang:
            case SkillType.EarthBlinkStrike:
            case SkillType.HybridBarrage:
            case SkillType.HybridLavaAura:
            case SkillType.HybridVenom:
                return true;

            // Utility & buff skills — effect_value là khoảng cách, thời gian, hoặc lượng buff
            case SkillType.Teleport:
            case SkillType.WindStep:
            case SkillType.MetalShield:
            case SkillType.WaterArmorBuff:
            case SkillType.EarthAura:
            case SkillType.Dash:
            default:
                return false;
        }
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
