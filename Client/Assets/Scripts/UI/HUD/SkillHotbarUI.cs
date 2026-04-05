using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Quản lý Hotbar kỹ năng — tự động tìm PlayerSkillManager của owner và
/// gắn từng SkillData vào các SkillSlotUI tương ứng.
///
/// Cấu trúc gợi ý trong Canvas:
///   SkillHotbar (GameObject — gắn script này)
///   ├── Slot0  (SkillSlotUI)
///   ├── Slot1  (SkillSlotUI)
///   ├── Slot2  (SkillSlotUI)
///   └── ...
///
/// Cách dùng:
///   1. Tạo các GameObject con theo cấu trúc trên.
///   2. Gán từng Slot vào mảng `slots`.
///   3. (Tuỳ chọn) Thêm icon sprites vào mảng `skillIcons` cùng thứ tự với skill trong PlayerSkillManager.
///   4. Script tự động bind khi tìm thấy PlayerSkillManager của owner.
/// </summary>
public class SkillHotbarUI : MonoBehaviour
{
    [Header("Slots")]
    [Tooltip("Danh sách SkillSlotUI — phải đúng thứ tự với skills trong PlayerSkillManager")]
    public List<SkillSlotUI> slots = new List<SkillSlotUI>();
    

    [Header("Icons (Tuỳ chọn)")]
    [Tooltip("Sprite icon cho từng skill (cùng thứ tự với danh sách). Để null nếu không có icon.")]
    public List<Sprite> skillIcons = new List<Sprite>();

    [Header("Settings")]
    [Tooltip("Tự động tìm PlayerSkillManager — nếu false, gán thủ công bằng SetSkillManager()")]
    public bool autoFind = true;

    [Tooltip("Tần suất retry tìm PlayerSkillManager (giây). Dùng khi player spawn chậm.")]
    [Range(0.1f, 2f)]
    public float retryInterval = 0.3f;

    // ── Internal ─────────────────────────────────────────────────────────────
    private PlayerSkillManager boundManager;
    private float retryTimer;
    private bool isBound;
    private int _lastManagerCount = -1; // track để detect khi player mới spawn
    private bool _loggedNoManagerWarning;
    private bool _loggedNoOwnerWarning;

    // ════════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Buộc rebind ngay lập tức — gọi từ PlayerSkillManager.OnNetworkSpawn() khi IsOwner
    /// </summary>
    public void ForceRebind()
    {
        isBound = false;
        boundManager = null;
        retryTimer = 0f;
        Debug.Log("[SkillHotbarUI] ForceRebind() được gọi — reset và tìm lại PlayerSkillManager.");
    }

    /// <summary>
    /// Gán thủ công PlayerSkillManager (nếu autoFind = false)
    /// </summary>
    public void SetSkillManager(PlayerSkillManager manager)
    {
        if (manager == null) return;
        BindToManager(manager);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!autoFind) return;

        // Nếu manager cũ bị destroy (player despawn khi chuyển map), reset để retry
        if (isBound && (boundManager == null || !boundManager.IsSpawned))
        {
            isBound = false;
            boundManager = null;
        }

        if (isBound) return;

        retryTimer -= Time.deltaTime;
        if (retryTimer > 0f) return;
        retryTimer = retryInterval;

        TryFindAndBind();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    private void TryFindAndBind()
    {
        PlayerSkillManager[] all = FindObjectsByType<PlayerSkillManager>(FindObjectsSortMode.None);
        if (all.Length != _lastManagerCount)
        {
            Debug.Log($"[SkillHotbarUI] TryFindAndBind — tìm thấy {all.Length} PlayerSkillManager trong scene.");
        }

        // Nếu số manager tăng lên (player mới spawn) và có owner mới → force rebind
        if (isBound && all.Length != _lastManagerCount)
        {
            bool ownerExists = System.Array.Exists(all, m => m.IsSpawned && m.IsOwner);
            if (ownerExists && (boundManager == null || !boundManager.IsOwner))
            {
                Debug.Log("[SkillHotbarUI] Phát hiện owner manager mới — rebind.");
                isBound = false;
                boundManager = null;
            }
        }
        _lastManagerCount = all.Length;

        if (isBound) return;

        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        bool isNetworkActive = networkManager != null
            && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer);

        foreach (var mgr in all)
        {
            if (mgr.IsSpawned && mgr.IsOwner)
            {
                _loggedNoManagerWarning = false;
                _loggedNoOwnerWarning = false;
                BindToManager(mgr);
                return;
            }
        }

        // Fallback CHỈ khi không có mạng (offline / single-player)
        bool isMultiplayer = Unity.Netcode.NetworkManager.Singleton != null
            && (Unity.Netcode.NetworkManager.Singleton.IsHost
                || Unity.Netcode.NetworkManager.Singleton.IsClient
                || Unity.Netcode.NetworkManager.Singleton.IsServer);
        if (!isMultiplayer && all.Length == 1)
        {
            Debug.Log($"[SkillHotbarUI] Offline fallback bind vào '{all[0].name}'.");
            _loggedNoManagerWarning = false;
            _loggedNoOwnerWarning = false;
            BindToManager(all[0]);
        }
        else if (all.Length == 0)
        {
            if (isNetworkActive && !_loggedNoManagerWarning)
            {
                Debug.LogWarning("[SkillHotbarUI] Chưa tìm thấy PlayerSkillManager nào — sẽ thử lại.");
                _loggedNoManagerWarning = true;
            }
        }
        else
        {
            _loggedNoManagerWarning = false;
            if (isNetworkActive && !_loggedNoOwnerWarning)
            {
                Debug.LogWarning($"[SkillHotbarUI] Có {all.Length} manager nhưng chưa tìm thấy IsOwner — sẽ thử lại.");
                _loggedNoOwnerWarning = true;
            }
        }
    }

    private void BindToManager(PlayerSkillManager manager)
    {
        boundManager = manager;
        isBound = true;

        int skillCount = manager.GetSkillCount();

        // Auto-discover: nếu số skill > số slot đã gán trong Inspector,
        // tìm thêm SkillSlotUI từ các GameObject con chưa có trong danh sách.
        if (skillCount > slots.Count)
        {
            foreach (Transform child in transform)
            {
                var slot = child.GetComponent<SkillSlotUI>();
                if (slot != null && !slots.Contains(slot))
                    slots.Add(slot);
                if (slots.Count >= skillCount) break;
            }
        }

        Debug.Log($"[SkillHotbarUI] BindToManager '{manager.name}' — skillCount={skillCount}, slots={slots.Count}, gameObject.activeSelf={gameObject.activeSelf}");

        for (int i = 0; i < slots.Count; i++)
        {
            SkillSlotUI slot = slots[i];
            if (slot == null)
            {
                Debug.LogWarning($"[SkillHotbarUI]   Slot[{i}] bị NULL trong danh sách!");
                continue;
            }

            if (i < skillCount)
            {
                SkillData skillData = manager.GetSkill(i);

                // Ưu tiên: 1) icon manual trong Inspector, 2) iconId từ DB, 3) skillCode fallback
                Sprite icon = (i < skillIcons.Count && skillIcons[i] != null) ? skillIcons[i] : null;
                if (icon == null && skillData != null && SkillIconDatabase.Instance != null)
                {
                    // Thử iconId (khớp với icon_id trong DB) trước, rồi fallback sang skillCode
                    if (!string.IsNullOrEmpty(skillData.iconId))
                        icon = SkillIconDatabase.Instance.GetIcon(skillData.iconId);
                    if (icon == null && !string.IsNullOrEmpty(skillData.skillCode))
                        icon = SkillIconDatabase.Instance.GetIcon(skillData.skillCode);
                }

                string iconName = icon != null ? icon.name : "null";
                Debug.Log($"[SkillHotbarUI]   Slot[{i}] ← skill '{skillData?.skillName}' key={skillData?.activationKey} icon={iconName}");
                slot.Bind(skillData, manager, i, icon);
            }
            else
            {
                Debug.Log($"[SkillHotbarUI]   Slot[{i}] không có skill tương ứng → Unbind.");
                slot.Unbind();
            }
        }

        Debug.Log($"[SkillHotbarUI] Hoàn tất bind {Mathf.Min(skillCount, slots.Count)} slot(s). GameObject active={gameObject.activeSelf}");

        // Nếu panel đang ẩn, log cảnh báo rõ ràng
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("[SkillHotbarUI] CẢNH BÁO: SkillHotbar đang bị SetActive(false) sau khi bind xong! Nhấn T để hiện.");
        }
    }
}
