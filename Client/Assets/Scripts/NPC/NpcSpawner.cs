using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Gắn vào một GameObject trong scene. Khi Start(), tự gọi API lấy danh sách NPC
/// của map hiện tại và instantiate prefab tại đúng vị trí.
///
/// Setup trong Inspector:
///   - npcPrefabs[0] = prefab NPC loại "shop"
///   - npcPrefabs[1] = prefab NPC loại "blacksmith"
///   - npcPrefabs[2] = prefab NPC loại "quest"
///   - npcPrefabs[3] = prefab NPC loại "exchange"
///   - npcPrefabs[4] = prefab NPC loại "event"
/// </summary>
public class NpcSpawner : MonoBehaviour
{
    [Header("Prefabs theo npc_type")]
    [Tooltip("Index: 0=shop, 1=blacksmith, 2=quest, 3=exchange, 4=event")]
    [SerializeField] private GameObject[] npcPrefabs;

    [Header("API")]
    [SerializeField] private string apiBase = "http://localhost:5000";

    private void Start()
    {
        StartCoroutine(SpawnNpcs());
    }

    private IEnumerator SpawnNpcs()
    {
        int mapId = MapManager.Instance != null ? MapManager.Instance.GetMapId() : 0;
        string url = $"{apiBase}/api/npc/list?mapId={mapId}";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NpcSpawner] Không load được NPC list: {req.error}");
            yield break;
        }

        // API trả về array JSON → bọc thành object để JsonUtility parse được
        var wrapped = "{\"npcs\":" + req.downloadHandler.text + "}";
        var resp = JsonUtility.FromJson<NpcListWrapper>(wrapped);

        if (resp?.npcs == null)
        {
            Debug.LogWarning("[NpcSpawner] Không có NPC nào trên map này.");
            yield break;
        }

        foreach (var npc in resp.npcs)
        {
            var prefab = GetPrefabForType(npc.npc_type);
            if (prefab == null)
            {
                Debug.LogWarning($"[NpcSpawner] Không tìm được prefab cho npc_type='{npc.npc_type}'");
                continue;
            }

            var go = Instantiate(prefab, new Vector3(npc.pos_x, npc.pos_y, 0f), Quaternion.identity);
            go.name = npc.npc_name;

            if (go.TryGetComponent<NpcInteraction>(out var inter))
                inter.Init(npc);
        }

        Debug.Log($"[NpcSpawner] Đã spawn {resp.npcs.Length} NPC trên map {mapId}.");
    }

    private GameObject GetPrefabForType(string type)
    {
        int idx = type switch
        {
            "shop"       => 0,
            "blacksmith" => 1,
            "quest"      => 2,
            "exchange"   => 3,
            "event"      => 4,
            _            => 0
        };
        return idx < npcPrefabs.Length ? npcPrefabs[idx] : null;
    }

    // ── Data classes ─────────────────────────────────────────

    [System.Serializable]
    private class NpcListWrapper { public NpcData[] npcs; }

    [System.Serializable]
    public class NpcData
    {
        public int    npc_id;
        public string npc_name;
        public string npc_type;   // shop | blacksmith | quest | exchange | event
        public float  pos_x;
        public float  pos_y;
        public string dialogue_key;
        public string icon_id;
    }
}
