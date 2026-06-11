using UnityEngine;

// DEPRECATED — đã thay thế bởi NpcServerManager.cs (NGO server-authoritative).
// Xóa component NpcSpawner khá»i tất cả GameObject trong scene và gắn NpcServerManager thay vào.
// Class giữ lại để tránh lỗi compile nếu còn reference cũ trong scene.
[System.Obsolete("Dùng NpcServerManager thay thế. Xem Client/Assets/Scripts/NPC/NpcServerManager.cs")]
public class NpcSpawner : MonoBehaviour { }
