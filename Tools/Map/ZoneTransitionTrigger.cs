using UnityEngine;

// Gắn vào BoxCollider2D tại ranh giới zone/map trong Scene.
// Khi player (client) bước vào → gửi ServerRpc xin chuyển zone.
// Kiến trúc 1-port (LangLa model):
// - KHÔNG disconnect/reconnect khi chuyển zone
// - Client gửi RequestZoneTransferServerRpc → ZoneTransitionController xử lý server-side
// - Server in-process reassign room → ClientRpc(sceneName, x, y) chỉ đến client đó
// Inspector setup:
// BoxCollider2D:  IsTrigger = true, layer = "ZoneTrigger" (tạo layer mới)
// ZoneTransitionTrigger:
// targetMapId     = ID của map đích
// targetZoneId    = ID của zone đích
// entryPointId    = Index của entry point trong zone đích
// transitionLabel = "Làng → Cánh Đồng Lửa" (chỉ dùng để debug)
// playerLayerMask = Layer của player
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneTransitionTrigger : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Map ID đích — khớp với map_config.map_id trong DB")]
    public int targetMapId;

    [Tooltip("Zone ID đích trong map (0-based)")]
    public int targetZoneId;

    [Tooltip("Entry point index trong ZoneServerConfig.entryPoints của zone đích")]
    public int entryPointId;

    [Header("Visual / Debug")]
    [Tooltip("Tên hiển thị trong Scene view — dùng để debug, không ảnh hưởng logic")]
    public string transitionLabel = "Zone Transition";

    [Header("Layer")]
    [Tooltip("Layer mask của player prefab — để trigger chỉ phản ứng với player")]
    public LayerMask playerLayerMask;

    // Cooldown để tránh double-trigger khi player đứng ở ranh giới
    private const float TRIGGER_COOLDOWN = 1.5f;
    private float _lastTriggerTime = -999f;

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Rate limit
        if (Time.time - _lastTriggerTime < TRIGGER_COOLDOWN) return;

        // Kiểm tra layer — chỉ phản ứng với player
        if (playerLayerMask != 0 && ((1 << other.gameObject.layer) & playerLayerMask) == 0)
            return;

        // Chỉ client owner của character mới gửi RPC
        var netObj = other.GetComponentInParent<Unity.Netcode.NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        _lastTriggerTime = Time.time;

        // Gửi yêu cầu chuyển zone lên server (single-port model)
        var transController = FindAnyObjectByType<ZoneTransitionController>();
        if (transController == null)
        {
            { /* Cảnh báo: Không tìm thấy ZoneTransitionController trong scene */ }
            return;
        }

        { /* '{transitionLabel}' → */ }

        transController.RequestZoneTransferServerRpc(targetMapId, targetZoneId, entryPointId);
    }

    // Editor: vẽ hình hộp trong Scene view để dễ nhìn
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.offset, col.size);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireCube(col.offset, col.size);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"{transitionLabel}\n→ map{targetMapId}_zone{targetZoneId}[{entryPointId}]");
#endif
    }
}
