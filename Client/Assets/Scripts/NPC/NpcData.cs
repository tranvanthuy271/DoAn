using System;

// NPC data DTO — dùng chung server lẫn client.
// [Serializable] để JsonUtility có thể serialize khi truyền qua ClientRpc (dạng JSON string).
[Serializable]
public class NpcData
{
    public int    npc_id;
    public string npc_name;
    public string npc_type;       // "shop" | "blacksmith" | "quest" | "exchange" | "event" | "dungeon"
    public float  pos_x;
    public float  pos_y;
    public string dialogue_key;   // (tuỳ chọn) key để lookup bảng dialogue
    public string dialogue_text;  // runtime: server điền trước khi gửi qua ClientRpc
    public string icon_id;
    // Danh sách nhãn menu server-driven (chỉ label, không kèm action_type).
    // Semicolon-separated: "Mua đồ;Nâng cấp;Cáo từ"
    // Khi khác rỗng, client sẽ hiện NpcDynamicMenuUI thay vì NpcMenuUI cũ.
    // Server lưu full "label:action_type" trong npc_config.menu_items;
    // chỉ truyền labels sang client để tránh lộ logic action.
    public string menu_items;
}

// Wrapper dùng khi API trả về JSON array thô — bọc lại cho JsonUtility parse.
[Serializable]
public class NpcListWrapper { public NpcData[] npcs; }

// Map npc_id cụ thể → prefab riêng, dùng trong NpcServerManager.npcPrefabsById[].
[System.Serializable]
public struct NpcIdPrefabEntry
{
    public int            npcId;
    public UnityEngine.GameObject prefab;
}
