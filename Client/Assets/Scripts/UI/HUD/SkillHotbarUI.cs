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

    // ════════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════════

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
        if (!autoFind || isBound) return;

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
        Debug.Log($"[SkillHotbarUI] TryFindAndBind — tìm thấy {all.Length} PlayerSkillManager trong scene.");

        foreach (var mgr in all)
        {
            Debug.Log($"[SkillHotbarUI]   • '{mgr.name}' IsSpawned={mgr.IsSpawned} IsOwner={mgr.IsOwner} skillCount={mgr.GetSkillCount()}");
            if (mgr.IsOwner)
            {
                BindToManager(mgr);
                return;
            }
        }

        // Fallback: nếu chỉ có 1 instance (single-player / host)
        if (all.Length == 1)
        {
            Debug.Log($"[SkillHotbarUI] Fallback bind vào instance duy nhất '{all[0].name}'.");
            BindToManager(all[0]);
        }
        else if (all.Length == 0)
        {
            Debug.LogWarning("[SkillHotbarUI] Chưa tìm thấy PlayerSkillManager nào — sẽ thử lại.");
        }
        else
        {
            Debug.LogWarning($"[SkillHotbarUI] Có {all.Length} manager nhưng không cái nào IsOwner — sẽ thử lại.");
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
                Sprite icon = (i < skillIcons.Count) ? skillIcons[i] : null;
                // Dùng == null để tránh UnassignedReferenceException với Unity Object
                string iconName = (icon != null && icon) ? icon.name : "null";
                Debug.Log($"[SkillHotbarUI]   Slot[{i}] ← skill '{skillData?.skillName}' key={skillData?.activationKey} icon={iconName}");
                slot.Bind(skillData, manager, i, icon != null && icon ? icon : null);
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
