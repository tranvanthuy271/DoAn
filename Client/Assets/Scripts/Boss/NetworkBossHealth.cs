using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

// ─────────────────────────────────────────────────────────────────────────────
//  NetworkBossHealth  —  Server-Authoritative HP cho Boss
//
//  ĐIỂM KHÁC SO VỚI NetworkEnemyHealth:
//    • Expose event OnBeforeTakeDamage → BossController check dodge + kháng nguyên tố
//    • Expose event OnAfterTakeDamage  → BossController xử lý return damage
//    • HealServer() để BossController gọi hồi HP
//    • Truyền elementType + attackerClientId qua ServerRpc
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NetworkObject))]
public class NetworkBossHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 1000;

    // HP sync
    private NetworkVariable<int> _currentHp = new(
        1000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<int> _maxHp = new(
        1000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged;  // current, max
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;

    // Events cho BossController (server only)
    // Func<rawDmg, elementType, attackerClientId, finalDmg>
    public Func<int, string, ulong, int>  OnBeforeTakeDamage;
    // Action<finalDmg, attackerClientId>
    public Action<int, ulong>             OnAfterTakeDamage;

    private bool _isDead;
    private ulong _lastAttackerClientId = ulong.MaxValue;

    // ─────────────────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _currentHp.OnValueChanged += OnHpChanged;
        _maxHp.OnValueChanged     += OnMaxHpChanged;

        if (IsServer)
        {
            _maxHp.Value     = maxHealth;
            _currentHp.Value = maxHealth;
        }

        OnHpChanged(0, _currentHp.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentHp.OnValueChanged -= OnHpChanged;
        _maxHp.OnValueChanged     -= OnMaxHpChanged;
        base.OnNetworkDespawn();
    }

    private void OnHpChanged(int _, int newVal)
    {
        int mx = _maxHp.Value > 0 ? _maxHp.Value : maxHealth;
        OnHealthChanged?.Invoke(newVal, mx);
        if (newVal <= 0 && !_isDead && IsServer)
            HandleDeath();
    }

    private void OnMaxHpChanged(int _, int newVal)
    {
        maxHealth = newVal;
        OnHealthChanged?.Invoke(_currentHp.Value, newVal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    public int GetCurrentHealth() => _currentHp.Value;
    public int GetMaxHealth()     => _maxHp.Value > 0 ? _maxHp.Value : maxHealth;

    /// <summary>ServerRpc: Client yêu cầu gây damage cho boss.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, string elementType, ServerRpcParams rpc = default)
    {
        TakeDamageInternal(damage, elementType, rpc.Receive.SenderClientId);
    }

    /// <summary>Gây damage trực tiếp trên server (không qua RPC).</summary>
    public void TakeDamageServer(int damage, string elementType = "", ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer) return;
        TakeDamageInternal(damage, elementType, attackerClientId);
    }

    private void TakeDamageInternal(int rawDamage, string elementType, ulong attackerClientId)
    {
        if (_isDead || _currentHp.Value <= 0) return;

        // Delegate sang BossController để xử lý dodge + kháng nguyên tố
        int finalDmg = rawDamage;
        if (OnBeforeTakeDamage != null)
            finalDmg = OnBeforeTakeDamage.Invoke(rawDamage, elementType, attackerClientId);

        if (finalDmg <= 0) return; // Đã né hoặc kháng hoàn toàn

        if (attackerClientId != ulong.MaxValue)
            _lastAttackerClientId = attackerClientId;

        _currentHp.Value = Mathf.Max(0, _currentHp.Value - finalDmg);
        OnTakeDamageClientRpc(finalDmg);

        OnAfterTakeDamage?.Invoke(finalDmg, attackerClientId);
    }

    /// <summary>Hồi HP trên server (gọi từ BossController.HandleHpRegen).</summary>
    public void HealServer(int amount)
    {
        if (!IsServer || _isDead) return;
        _currentHp.Value = Mathf.Min(_currentHp.Value + amount, _maxHp.Value);
    }

    [ClientRpc]
    private void OnTakeDamageClientRpc(int dmg)
    {
        OnTakeDamage?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Death
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleDeath()
    {
        if (_isDead) return;
        _isDead = true;

        OnDeathClientRpc();
        AwardExpToKiller();

        var bossCtrl = GetComponent<BossController>();
        bossCtrl?.OnDead();

        // Despawn sau delay nhỏ (để animation chạy)
        Invoke(nameof(DespawnBoss), 2f);
    }

    [ClientRpc]
    private void OnDeathClientRpc()
    {
        OnDeath?.Invoke();
        // Animator trigger "Die" nếu có
        var anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");
    }

    private void AwardExpToKiller()
    {
        if (_lastAttackerClientId == ulong.MaxValue) return;
        // Gửi EXP — tương tự NetworkEnemyHealth.HandleDeath pattern
        var bossData = GetComponent<BossController>()?.data;
        if (bossData == null) return;

        if (NetworkManager.Singleton == null) return;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != _lastAttackerClientId) continue;
            // Tìm component nhận EXP (tương tự cơ chế EXP hiện có)
            var expReceiver = client.PlayerObject?.GetComponent<IExpReceiver>();
            expReceiver?.ReceiveExp(bossData.expReward);
            break;
        }
    }

    private void DespawnBoss()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Interface — Nhận EXP (implement trên PlayerController hoặc PlayerStats)
// ─────────────────────────────────────────────────────────────────────────────
public interface IExpReceiver
{
    void ReceiveExp(int amount);
}
