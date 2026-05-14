using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// DebuffManager – NetworkBehaviour quản lý debuff (hiệu ứng bất lợi) trên một target (player hoặc enemy).
///
/// Setup:
///   • Thêm vào Player prefab và tất cả Enemy prefabs.
///   • Script tự detect xem nó đang trên player hay enemy qua PlayerMovement/EnemyAI.
///
/// Flow:
///   1. Projectile/Skill gọi ApplyDebuffServerRpc() trên target.
///   2. Server thêm DebuffEntry vào NetworkList (tự sync sang tất cả clients).
///   3. NetworkList.OnListChanged → fire OnDebuffsChanged → UI cập nhật.
///   4. PlayerMovement / EnemyAI đọc GetSlowFactor() / IsFrozen() mỗi frame.
///   5. TakeDamageInternal đọc GetDefenseDebuffPct().
/// </summary>
[DisallowMultipleComponent]
public class DebuffManager : NetworkBehaviour
{
    // ── Network State (syncs to all clients automatically) ────────────────────
    public NetworkList<DebuffEntry> ActiveDebuffs { get; private set; }

    // ── Events (UI subscribe) ────────────────────────────────────────────────
    /// <summary>Fired mỗi khi danh sách debuff thay đổi (thêm / xóa).</summary>
    public event Action OnDebuffsChanged;

    // ── Cached component refs ────────────────────────────────────────────────
    private PlayerMovement   _playerMovement;
    private EnemyAI          _enemyAI;
    private NetworkPlayerHealth _playerHealth;
    private NetworkEnemyHealth  _enemyHealth;

    // ── Burn coroutine tracking (server-side, no double-burn) ─────────────────
    private readonly Dictionary<SkillDebuffType, Coroutine> _activeCoroutines
        = new Dictionary<SkillDebuffType, Coroutine>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        ActiveDebuffs = new NetworkList<DebuffEntry>(
            new List<DebuffEntry>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        _playerMovement = GetComponent<PlayerMovement>();
        _enemyAI        = GetComponent<EnemyAI>();
        _playerHealth   = GetComponent<NetworkPlayerHealth>();
        _enemyHealth    = GetComponent<NetworkEnemyHealth>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ActiveDebuffs.OnListChanged += HandleListChanged;
    }

    public override void OnNetworkDespawn()
    {
        ActiveDebuffs.OnListChanged -= HandleListChanged;
        base.OnNetworkDespawn();
    }

    private void HandleListChanged(NetworkListEvent<DebuffEntry> evt)
    {
        OnDebuffsChanged?.Invoke();
    }

    // ── Update: server tự expire entries ─────────────────────────────────────
    private void Update()
    {
        if (!IsServer) return;

        float now = (float)NetworkManager.Singleton.ServerTime.TimeAsFloat;
        for (int i = ActiveDebuffs.Count - 1; i >= 0; i--)
        {
            if (ActiveDebuffs[i].ExpireServerTime <= now)
            {
                var removed = ActiveDebuffs[i];
                ActiveDebuffs.RemoveAt(i);

                // Unfreeze khi hết freeze debuff
                if (removed.Type == SkillDebuffType.Freeze)
                    RemoveFreezeEffect();
            }
        }
    }

    // ── API ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Server-RPC: Áp dụng debuff lên target này.
    /// Gọi từ FireballDamage/DotDamage khi hit target.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyDebuffServerRpc(
        SkillDebuffType type,
        int value,
        float duration,
        int iconId,
        FixedString64Bytes debuffName)
    {
        if (!IsSpawned) return;

        float now    = (float)NetworkManager.Singleton.ServerTime.TimeAsFloat;
        float expiry = now + duration;

        // Refresh nếu cùng loại đã active (lấy cái mạnh/dài hơn)
        for (int i = 0; i < ActiveDebuffs.Count; i++)
        {
            if (ActiveDebuffs[i].Type == type)
            {
                var existing = ActiveDebuffs[i];
                if (expiry > existing.ExpireServerTime || value > existing.Value)
                {
                    ActiveDebuffs.RemoveAt(i);
                    // dừng coroutine cũ nếu có
                    StopDebuffCoroutine(type);
                }
                else
                {
                    return; // debuff hiện tại mạnh hơn hoặc dài hơn → giữ nguyên
                }
                break;
            }
        }

        var entry = new DebuffEntry
        {
            Type             = type,
            Value            = value,
            IconId           = iconId,
            Name             = debuffName,
            ExpireServerTime = expiry,
            TotalDuration    = duration,
        };
        ActiveDebuffs.Add(entry);

        // Side-effects chạy trên server
        if (type == SkillDebuffType.Freeze)
            ApplyFreezeEffect(duration);
        else if (type == SkillDebuffType.Burn)
        {
            var co = StartCoroutine(BurnTickCoroutine(value, duration));
            _activeCoroutines[type] = co;
        }
    }

    // ── Stat Queries (đọc từ NetworkList, hoạt động trên mọi client) ─────────

    /// <returns>Hệ số tốc độ [0..1]. 1 = bình thường, 0.5 = chậm 50%.</returns>
    public float GetSlowFactor()
    {
        for (int i = 0; i < ActiveDebuffs.Count; i++)
        {
            var e = ActiveDebuffs[i];
            if (e.Type == SkillDebuffType.Slow)
                return Mathf.Clamp01(1f - e.Value / 100f);
        }
        return 1f;
    }

    /// <returns>% giảm tấn công (0-100). 0 = không giảm.</returns>
    public int GetAttackDebuffPct()
    {
        int max = 0;
        for (int i = 0; i < ActiveDebuffs.Count; i++)
        {
            if (ActiveDebuffs[i].Type == SkillDebuffType.Weaken)
                max = Mathf.Max(max, ActiveDebuffs[i].Value);
        }
        return max;
    }

    /// <returns>% giảm giáp (0-100). 0 = không giảm.</returns>
    public int GetDefenseDebuffPct()
    {
        int max = 0;
        for (int i = 0; i < ActiveDebuffs.Count; i++)
        {
            if (ActiveDebuffs[i].Type == SkillDebuffType.DefenseDown)
                max = Mathf.Max(max, ActiveDebuffs[i].Value);
        }
        return max;
    }

    /// <returns>true nếu target đang bị Freeze.</returns>
    public bool IsFrozen()
    {
        for (int i = 0; i < ActiveDebuffs.Count; i++)
        {
            if (ActiveDebuffs[i].Type == SkillDebuffType.Freeze)
                return true;
        }
        return false;
    }

    /// <returns>true nếu có bất kỳ debuff active nào.</returns>
    public bool HasAnyDebuff()
    {
        return ActiveDebuffs.Count > 0;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyFreezeEffect(float duration)
    {
        // Player
        if (_playerMovement != null)
        {
            _playerMovement.SetStunned(duration);
        }
        // Enemy
        if (_enemyAI != null)
        {
            // Freeze enemy qua ClientRpc để enemy (nếu là AI client-driven) cũng đứng im
            FreezeEnemyClientRpc(duration);
        }
    }

    private void RemoveFreezeEffect()
    {
        // PlayerMovement tự unfreeze theo stunTimer; không cần làm gì thêm.
        // Enemy thì gọi ClientRpc để unfreeze
        if (_enemyAI != null)
            UnfreezeEnemyClientRpc();
    }

    [ClientRpc]
    private void FreezeEnemyClientRpc(float duration)
    {
        if (_enemyAI != null)
            _enemyAI.ApplyFreeze(duration);
    }

    [ClientRpc]
    private void UnfreezeEnemyClientRpc()
    {
        if (_enemyAI != null)
            _enemyAI.RemoveFreeze();
    }

    private IEnumerator BurnTickCoroutine(int damagePerTick, float duration)
    {
        int ticks = Mathf.Max(1, Mathf.FloorToInt(duration));
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (!IsSpawned) yield break;

            if (_playerHealth != null)
                _playerHealth.TakeDamage(damagePerTick);
            else if (_enemyHealth != null)
                _enemyHealth.TakeDamage(damagePerTick);
        }
        _activeCoroutines.Remove(SkillDebuffType.Burn);
    }

    private void StopDebuffCoroutine(SkillDebuffType type)
    {
        if (_activeCoroutines.TryGetValue(type, out var co) && co != null)
        {
            StopCoroutine(co);
            _activeCoroutines.Remove(type);
        }
    }
}
