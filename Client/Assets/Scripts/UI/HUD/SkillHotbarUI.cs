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
        // Tìm tất cả PlayerSkillManager trong scene, lấy cái là owner (IsOwner)
        PlayerSkillManager[] all = FindObjectsByType<PlayerSkillManager>(FindObjectsSortMode.None);
        foreach (var mgr in all)
        {
            // Kiểm tra ownership cho Netcode for GameObjects
            if (mgr.IsOwner)
            {
                BindToManager(mgr);
                return;
            }
        }

        // Fallback: nếu chỉ có 1 instance (single-player / host)
        if (all.Length == 1)
        {
            BindToManager(all[0]);
        }
    }

    private void BindToManager(PlayerSkillManager manager)
    {
        boundManager = manager;
        isBound = true;

        int skillCount = manager.GetSkillCount();

        for (int i = 0; i < slots.Count; i++)
        {
            SkillSlotUI slot = slots[i];
            if (slot == null) continue;

            if (i < skillCount)
            {
                SkillData skillData = manager.GetSkill(i);
                Sprite icon = (i < skillIcons.Count) ? skillIcons[i] : null;
                slot.Bind(skillData, manager, i, icon);
            }
            else
            {
                // Skill chưa có → làm trống slot
                slot.Unbind();
            }
        }

        Debug.Log($"[SkillHotbarUI] Đã bind {Mathf.Min(skillCount, slots.Count)} slot(s) vào '{manager.name}'.");
    }
}
