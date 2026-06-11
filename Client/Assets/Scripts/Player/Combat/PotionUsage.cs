using Unity.Netcode;
using UnityEngine;

// Hệ thống bình hồi phục HP/MP cho player.
// Cách dùng:
// - Gắn script này lên Player Prefab (cùng object với NetworkPlayerDataSync)
// - Config phím và lượng hồi phục trong Inspector
// - Nhấn phím để uống bình (có cooldown để tránh spam)
// Phím mặc định:
// H = Bình máu (HP Potion)
// M = Bình mana (MP Potion)
public class PotionUsage : NetworkBehaviour
{
    [Header("HP Potion (Bình Máu)")]
    [Tooltip("Phím uống bình máu")]
    [SerializeField] private KeyCode hpPotionKey = KeyCode.H;

    [Tooltip("Lượng HP hồi phục mỗi lần uống")]
    [SerializeField] private int hpRestoreAmount = 30;

    [Tooltip("Cooldown bình máu (giây)")]
    [SerializeField] private float hpPotionCooldown = 5f;

    [Header("MP Potion (Bình Mana)")]
    [Tooltip("Phím uống bình mana")]
    [SerializeField] private KeyCode mpPotionKey = KeyCode.M;

    [Tooltip("Lượng MP hồi phục mỗi lần uống")]
    [SerializeField] private int mpRestoreAmount = 30;

    [Tooltip("Cooldown bình mana (giây)")]
    [SerializeField] private float mpPotionCooldown = 5f;

    // Xử lý nội bộ phục vụ các hàm public.
    private float hpCooldownTimer;
    private float mpCooldownTimer;
    private NetworkPlayerDataSync dataSync;
    private NetworkPlayerHealth networkHealth;

    private void Start()
    {
        dataSync      = GetComponent<NetworkPlayerDataSync>();
        networkHealth = GetComponent<NetworkPlayerHealth>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (hpCooldownTimer > 0f) hpCooldownTimer -= Time.deltaTime;
        if (mpCooldownTimer > 0f) mpCooldownTimer -= Time.deltaTime;

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
            return;

        if (Input.GetKeyDown(hpPotionKey) && hpCooldownTimer <= 0f)
            UseHpPotion();

        if (Input.GetKeyDown(mpPotionKey) && mpCooldownTimer <= 0f)
            UseMpPotion();
    }

    // Potion Actions

    private void UseHpPotion()
    {
        hpCooldownTimer = hpPotionCooldown;

        // Ưu tiên NetworkPlayerHealth (server-authoritative)
        if (networkHealth != null)
        {
            networkHealth.HealServerRpc(hpRestoreAmount);
            { /* Uống bình HP  hồi {hpRestoreAmount} HP (NetworkPlayerHealth) */ }
            return;
        }

        // Fallback: NetworkPlayerDataSync
        if (dataSync == null) { { /* Cảnh báo: Không tìm thấy NetworkPlayerDataSync */ } return; }

        if (IsServer)
            dataSync.networkHp.Value = Mathf.Min(dataSync.networkMaxHp.Value, dataSync.networkHp.Value + hpRestoreAmount);
        else
            dataSync.RestoreHpServerRpc(hpRestoreAmount);

        { /* Uống bình HP  hồi {hpRestoreAmount} HP (DataSync) */ }
    }

    private void UseMpPotion()
    {
        if (dataSync == null) { { /* Cảnh báo: Không tìm thấy NetworkPlayerDataSync */ } return; }

        mpCooldownTimer = mpPotionCooldown;

        if (IsServer)
            dataSync.networkMp.Value = Mathf.Min(dataSync.networkMaxMp.Value, dataSync.networkMp.Value + mpRestoreAmount);
        else
            dataSync.RestoreMpServerRpc(mpRestoreAmount);

        { /* Uống bình MP  hồi {mpRestoreAmount} MP */ }
    }

    // Public API (dùng cho UI)

    // % cooldown bình HP (0 = sẵn sàng, 1 = đang CD).
    public float GetHpCooldownPercent() =>
        hpPotionCooldown > 0f ? Mathf.Clamp01(hpCooldownTimer / hpPotionCooldown) : 0f;

    // % cooldown bình MP (0 = sẵn sàng, 1 = đang CD).
    public float GetMpCooldownPercent() =>
        mpPotionCooldown > 0f ? Mathf.Clamp01(mpCooldownTimer / mpPotionCooldown) : 0f;

    // Giây còn lại của cooldown bình HP.
    public float GetHpCooldownRemaining() => Mathf.Max(0f, hpCooldownTimer);

    // Giây còn lại của cooldown bình MP.
    public float GetMpCooldownRemaining() => Mathf.Max(0f, mpCooldownTimer);
}
