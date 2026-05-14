using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerBuffSync – NetworkBehaviour sync trạng thái buff có lợi từ skill lên tất cả clients.
///
/// Setup: Thêm vào Player prefab (không dùng cho Enemy).
///
/// Hai buff được sync:
///   • Armor Buff (WaterArmorBuffSkill): armorBuffExpiry, armorBuffValue, armorIconId
///   • Attack Buff (EarthAttackBuffSkill): attackBuffExpiry, attackBuffValue, attackIconId
///
/// Flow:
///   1. WaterArmorBuffSkill / EarthAttackBuffSkill gọi SetArmorBuffServerRpc / SetAttackBuffServerRpc.
///   2. Server set NetworkVariables → tự sync sang tất cả clients.
///   3. OnValueChanged → fire OnBuffStateChanged → OverheadStatusDisplay + ActiveBuffManager update.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBuffSync : NetworkBehaviour
{
    // ── Armor Buff (WaterArmor) ───────────────────────────────────────────────
    public NetworkVariable<float> armorBuffExpiry = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> armorBuffValue = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> armorIconId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> armorBuffName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Tổng thời gian buff giáp (giây) — dùng để tính tỉ lệ fade khi tint sprite.</summary>
    public NetworkVariable<float> armorBuffTotalDuration = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Attack Buff (EarthAura) ───────────────────────────────────────────────
    public NetworkVariable<float> attackBuffExpiry = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> attackBuffValue = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> attackIconId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> attackBuffName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Tổng thời gian buff tấn công (giây) — dùng để tính tỉ lệ fade khi tint sprite.</summary>
    public NetworkVariable<float> attackBuffTotalDuration = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Event (UI subscribe) ─────────────────────────────────────────────────
    /// <summary>Fired khi bất kỳ NetworkVariable nào thay đổi.</summary>
    public event Action OnBuffStateChanged;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        armorBuffExpiry.OnValueChanged  += OnAnyChanged;
        attackBuffExpiry.OnValueChanged += OnAnyChanged;

        // Local player: subscribe để push vào ActiveBuffManager sau khi HUD sẵn sàng
        if (IsOwner)
        {
            armorBuffExpiry.OnValueChanged  += PushArmorToActiveBuffManager;
            attackBuffExpiry.OnValueChanged += PushAttackToActiveBuffManager;
        }
    }

    public override void OnNetworkDespawn()
    {
        armorBuffExpiry.OnValueChanged  -= OnAnyChanged;
        attackBuffExpiry.OnValueChanged -= OnAnyChanged;

        if (IsOwner)
        {
            armorBuffExpiry.OnValueChanged  -= PushArmorToActiveBuffManager;
            attackBuffExpiry.OnValueChanged -= PushAttackToActiveBuffManager;
        }
        base.OnNetworkDespawn();
    }

    private void OnAnyChanged<T>(T prev, T next) => OnBuffStateChanged?.Invoke();

    // ── Server RPCs ───────────────────────────────────────────────────────────

    /// <summary>
    /// WaterArmorBuffSkill gọi sau khi ApplyArmorBuff() để sync lên HUD mọi client.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetArmorBuffServerRpc(int value, float duration, int iconId, FixedString64Bytes buffNameStr)
    {
        float expiry = (float)NetworkManager.Singleton.ServerTime.TimeAsFloat + duration;
        // Refresh: chỉ cập nhật nếu expiry mới dài hơn
        if (expiry > armorBuffExpiry.Value)
        {
            armorBuffExpiry.Value       = expiry;
            armorBuffValue.Value        = value;
            armorIconId.Value           = iconId;
            armorBuffName.Value         = buffNameStr;
            armorBuffTotalDuration.Value = duration;
        }
    }

    /// <summary>
    /// EarthAttackBuffSkill gọi sau khi ApplyAttackBuff() để sync lên HUD mọi client.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetAttackBuffServerRpc(int value, float duration, int iconId, FixedString64Bytes buffNameStr)
    {
        float expiry = (float)NetworkManager.Singleton.ServerTime.TimeAsFloat + duration;
        if (expiry > attackBuffExpiry.Value)
        {
            attackBuffExpiry.Value        = expiry;
            attackBuffValue.Value         = value;
            attackIconId.Value            = iconId;
            attackBuffName.Value          = buffNameStr;
            attackBuffTotalDuration.Value = duration;
        }
    }

    // ── Queries (hoạt động trên mọi client) ──────────────────────────────────

    /// <returns>Giây còn lại của ArmorBuff. 0 nếu không active.</returns>
    public float GetArmorBuffRemaining()
    {
        float now = (float)NetworkManager.Singleton.ServerTime.TimeAsFloat;
        return Mathf.Max(0f, armorBuffExpiry.Value - now);
    }

    /// <returns>Giây còn lại của AttackBuff. 0 nếu không active.</returns>
    public float GetAttackBuffRemaining()
    {
        float now = (float)NetworkManager.Singleton.ServerTime.TimeAsFloat;
        return Mathf.Max(0f, attackBuffExpiry.Value - now);
    }

    public bool IsArmorBuffActive()  => GetArmorBuffRemaining() > 0f;
    public bool IsAttackBuffActive() => GetAttackBuffRemaining() > 0f;

    // ── Push to ActiveBuffManager (local player only) ─────────────────────────

    private void PushArmorToActiveBuffManager(float prev, float next)
    {
        if (ActiveBuffManager.Instance == null) return;
        float remaining = GetArmorBuffRemaining();
        if (remaining <= 0f) return;

        var dto = new ActiveBuffDto
        {
            effectType = "ArmorBuff",
            value      = armorBuffValue.Value,
            iconId     = armorIconId.Value,
            name       = armorBuffName.Value.ToString(),
            detail     = $"+{armorBuffValue.Value} giáp tạm thời",
            expireAt   = System.DateTime.UtcNow.AddSeconds(remaining).ToString("o"),
        };
        ActiveBuffManager.Instance.PushSkillBuff(dto);
    }

    private void PushAttackToActiveBuffManager(float prev, float next)
    {
        if (ActiveBuffManager.Instance == null) return;
        float remaining = GetAttackBuffRemaining();
        if (remaining <= 0f) return;

        var dto = new ActiveBuffDto
        {
            effectType = "AttackBuff",
            value      = attackBuffValue.Value,
            iconId     = attackIconId.Value,
            name       = attackBuffName.Value.ToString(),
            detail     = $"+{attackBuffValue.Value}% tấn công",
            expireAt   = System.DateTime.UtcNow.AddSeconds(remaining).ToString("o"),
        };
        ActiveBuffManager.Instance.PushSkillBuff(dto);
    }
}
