using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn vào prefab NPC. Khi player click (PC) hoặc tap (mobile) → kiểm tra khoảng cách → mở NpcMenuUI.
/// Implement IPointerClickHandler thay vì OnMouseDown để hỗ trợ cả mobile.
///
/// Yêu cầu:
///   - Camera cần có Physics2DRaycaster (Add Component → Physics 2D Raycaster)
///   - Canvas ở chế độ Screen Space - Camera hoặc World Space (không phải Overlay)
///     nếu muốn raycast vào world object.
///   - Hoặc dùng EventSystem + StandaloneInputModule (đã có sẵn khi dùng EventSystem mặc định).
/// </summary>
public class NpcInteraction : MonoBehaviour, IPointerClickHandler
{
    [Header("Khoảng cách tối đa để tương tác")]
    [SerializeField] private float maxInteractDistance = 3f;

    private NpcSpawner.NpcData npcData;

    /// <summary>Được gọi bởi NpcSpawner sau khi instantiate prefab.</summary>
    public void Init(NpcSpawner.NpcData data)
    {
        npcData = data;
    }

    // ── IPointerClickHandler — hoạt động cả PC lẫn Android/iOS ──────
    public void OnPointerClick(PointerEventData eventData)
    {
        TryInteract();
    }

    // Fallback cho trường hợp chưa có EventSystem đầy đủ (editor test)
    private void OnMouseDown()
    {
        TryInteract();
    }

    private void TryInteract()
    {
        if (npcData == null)
        {
            Debug.LogWarning("[NpcInteraction] NpcData chưa được khởi tạo.");
            return;
        }

        GameObject localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            float dist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(localPlayer.transform.position.x, localPlayer.transform.position.y)
            );

            if (dist > maxInteractDistance)
            {
                Debug.Log($"[NpcInteraction] Quá xa NPC '{npcData.npc_name}' ({dist:F1}u). Hãy lại gần hơn!");
                return;
            }
        }

        NpcMenuUI.Instance?.Open(npcData);
    }

    private GameObject FindLocalPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            if (p.TryGetComponent<Unity.Netcode.NetworkObject>(out var no) && no.IsOwner)
                return p;
        }
        return GameObject.FindGameObjectWithTag("Player");
    }
}
