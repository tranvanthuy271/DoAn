using System;
using System.Collections.Generic;
using UnityEngine;

// ScriptableObject ánh xạ npc_type / npc_id → prefab dùng cho NpcServerManager.
// Đặt asset mặc định tại Resources/ScriptableObjects/NpcPrefabConfig
// để scene chỉ cần đúng prefab list, không phải kéo nhiều nơi.
[CreateAssetMenu(fileName = "NpcPrefabConfig", menuName = "DoAn/Npc Prefab Config")]
public class NpcPrefabConfig : ScriptableObject
{
    public const string DefaultResourcesPath = "ScriptableObjects/NpcPrefabConfig";

    [Serializable]
    public struct NpcTypePrefabEntry
    {
        public string npcType;
        public GameObject prefab;
    }

    [Header("Fallback")]
    [SerializeField] private GameObject defaultPrefab;

    [Header("By Type")]
    [SerializeField] private NpcTypePrefabEntry[] prefabsByType;

    [Header("By NPC ID Override")]
    [SerializeField] private NpcIdPrefabEntry[] prefabsById;

    public static NpcPrefabConfig Resolve(NpcPrefabConfig assignedConfig, UnityEngine.Object context, string owner)
    {
        if (assignedConfig != null)
        {
            return assignedConfig;
        }

        NpcPrefabConfig loadedConfig = Resources.Load<NpcPrefabConfig>(DefaultResourcesPath);
        if (loadedConfig == null)
        {
            Debug.LogWarning(
                $"[{owner}] Chưa gán NpcPrefabConfig và không tìm thấy asset mặc định tại Resources/{DefaultResourcesPath}.",
                context);
        }

        return loadedConfig;
    }

    public GameObject ResolvePrefab(NpcData npc)
    {
        if (npc == null)
        {
            return defaultPrefab;
        }

        if (prefabsById != null)
        {
            foreach (NpcIdPrefabEntry entry in prefabsById)
            {
                if (entry.npcId == npc.npc_id && entry.prefab != null)
                {
                    return entry.prefab;
                }
            }
        }

        string npcType = npc.npc_type?.Trim();
        if (!string.IsNullOrWhiteSpace(npcType) && prefabsByType != null)
        {
            foreach (NpcTypePrefabEntry entry in prefabsByType)
            {
                if (entry.prefab == null || string.IsNullOrWhiteSpace(entry.npcType))
                {
                    continue;
                }

                if (string.Equals(entry.npcType.Trim(), npcType, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.prefab;
                }
            }
        }

        return defaultPrefab;
    }

    public void AppendAllPrefabs(HashSet<GameObject> results)
    {
        if (results == null)
        {
            return;
        }

        AddPrefab(results, defaultPrefab);

        if (prefabsByType != null)
        {
            foreach (NpcTypePrefabEntry entry in prefabsByType)
            {
                AddPrefab(results, entry.prefab);
            }
        }

        if (prefabsById != null)
        {
            foreach (NpcIdPrefabEntry entry in prefabsById)
            {
                AddPrefab(results, entry.prefab);
            }
        }
    }

    private static void AddPrefab(HashSet<GameObject> results, GameObject prefab)
    {
        if (prefab != null)
        {
            results.Add(prefab);
        }
    }
}