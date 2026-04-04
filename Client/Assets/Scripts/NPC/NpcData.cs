using System;

/// <summary>
/// NPC data DTO — dùng chung server lẫn client.
/// [Serializable] để JsonUtility có thể serialize khi truyền qua ClientRpc (dạng JSON string).
/// </summary>
[Serializable]
public class NpcData
{
    public int    npc_id;
    public string npc_name;
    public string npc_type;       // "shop" | "blacksmith" | "quest" | "exchange" | "event"
    public float  pos_x;
    public float  pos_y;
    public string dialogue_key;   // (tuỳ chọn) key để lookup bảng dialogue
    public string dialogue_text;  // runtime: server điền trước khi gửi qua ClientRpc
    public string icon_id;
}

/// <summary>Wrapper dùng khi API trả về JSON array thô — bọc lại cho JsonUtility parse.</summary>
[Serializable]
public class NpcListWrapper { public NpcData[] npcs; }

/// <summary>Map npc_id cụ thể → prefab riêng, dùng trong NpcServerManager.npcPrefabsById[].</summary>
[System.Serializable]
public struct NpcIdPrefabEntry
{
    public int            npcId;
    public UnityEngine.GameObject prefab;
}
