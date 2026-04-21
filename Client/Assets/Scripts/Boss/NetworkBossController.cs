using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

// ─────────────────────────────────────────────────────────────────────────────
//  NetworkBossController  —  Network sync cho Boss
//
//  TRÁCH NHIỆM:
//    • Sync hướng mặt (scaleX) qua NetworkVariable
//    • Sync trạng thái attack animation cho remote clients
//    • Sync trạng thái stealth (alpha) qua NetworkVariable
//    • Server authority — AI logic chạy trong BossController (MonoBehaviour)
//
//  SETUP:
//    Attach cùng prefab với: NetworkObject, NetworkTransform, BossController,
//    NetworkBossHealth, Rigidbody2D, Animator
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(BossController))]
public class NetworkBossController : NetworkBehaviour
{
    private static readonly int AnimIsAttacking = Animator.StringToHash("isAttacking");

    // Sync hướng
    private NetworkVariable<float> _netScaleX = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Sync trạng thái tấn công
    private NetworkVariable<bool> _netIsAttacking = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Sync stealth alpha (0 = vô hình, 1 = bình thường)
    private NetworkVariable<float> _netAlpha = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Rigidbody2D     _rb;
    private Animator        _anim;
    private SpriteRenderer[] _renderers;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb        = GetComponent<Rigidbody2D>();
        _anim      = GetComponent<Animator>();
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);

        // Đảm bảo có NetworkTransform
        if (GetComponent<NetworkTransform>() == null)
        {
            var nt = gameObject.AddComponent<NetworkTransform>();
            nt.SyncPositionX = true;
            nt.SyncPositionY = true;
            nt.SyncPositionZ = false;
        }

        // Enemy layer không đẩy nhau
        int layer = LayerMask.NameToLayer("Enemy");
        if (layer >= 0)
            Physics2D.IgnoreLayerCollision(layer, layer, true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _netScaleX.OnValueChanged      += OnScaleXChanged;
        _netIsAttacking.OnValueChanged += OnAttackStateChanged;
        _netAlpha.OnValueChanged       += OnAlphaChanged;

        // Non-server: kinematic (server drives physics)
        if (!IsServer && _rb != null)
            _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public override void OnNetworkDespawn()
    {
        _netScaleX.OnValueChanged      -= OnScaleXChanged;
        _netIsAttacking.OnValueChanged -= OnAttackStateChanged;
        _netAlpha.OnValueChanged       -= OnAlphaChanged;
        base.OnNetworkDespawn();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Server Update — sync state to clients every frame
    // ─────────────────────────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // Sync scale (facing direction)
        float sx = transform.localScale.x;
        if (!Mathf.Approximately(_netScaleX.Value, sx))
            _netScaleX.Value = sx;

        // Sync attack anim state
        if (_anim != null)
        {
            bool attacking = _anim.GetBool(AnimIsAttacking);
            if (_netIsAttacking.Value != attacking)
                _netIsAttacking.Value = attacking;

            // Sync alpha (stealth) — lấy từ renderer đầu tiên
            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null)
            {
                float alpha = _renderers[0].color.a;
                if (!Mathf.Approximately(_netAlpha.Value, alpha))
                    _netAlpha.Value = alpha;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  NetworkVariable callbacks (remote clients)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnScaleXChanged(float _, float newVal)
    {
        Vector3 s = transform.localScale;
        s.x = newVal;
        transform.localScale = s;
    }

    private void OnAttackStateChanged(bool _, bool newVal)
    {
        if (_anim != null) _anim.SetBool(AnimIsAttacking, newVal);
    }

    private void OnAlphaChanged(float _, float newVal)
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = newVal;
            r.color = c;
        }
    }
}
