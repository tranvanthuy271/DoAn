using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn script này vào root GameObject của mỗi Canvas cần tồn tại qua scene load.
/// 
/// Ví dụ gắn vào: ScreenSpaceCanvas, InformationCanvas, SkillHotbar (root canvas), EventSystem.
///
/// Cơ chế:
///   - Awake(): DontDestroyOnLoad(gameObject) — object persist khi LoadScene
///   - Singleton theo tên: nếu scene mới cũng có object cùng tên có GameUIPersist,
///     instance MỚI tự hủy để tránh duplicate.
///
/// LƯU Ý:
///   - Chỉ gắn vào ROOT GameObject của canvas (không phải canvas con).
///   - Đảm bảo Canvas dùng render mode "Screen Space - Overlay" hoặc
///     camera reference còn tồn tại sau khi scene load.
///   - EventSystem cũng nên persist (gắn script này vào EventSystem object).
/// </summary>
public class GameUIPersist : MonoBehaviour
{
    private static System.Collections.Generic.Dictionary<string, GameUIPersist> _instances
        = new System.Collections.Generic.Dictionary<string, GameUIPersist>();
    private static GameUIPersist _eventSystemInstance;

    private void Awake()
    {
        bool isEventSystem = GetComponent<EventSystem>() != null;
        string key = gameObject.name;

        if (isEventSystem && _eventSystemInstance != null && _eventSystemInstance != this)
        {
            Debug.Log($"[GameUIPersist] Duplicate EventSystem '{key}' — destroying new instance, keeping persisted one.");
            Destroy(gameObject);
            return;
        }

        if (_instances.TryGetValue(key, out var existing) && existing != null)
        {
            // Đã có instance cũ (persistent từ scene trước) — hủy object mới này
            Debug.Log($"[GameUIPersist] Duplicate '{key}' — destroying new instance, keeping persisted one.");
            Destroy(gameObject);
            return;
        }

        _instances[key] = this;
        if (isEventSystem)
            _eventSystemInstance = this;

        DontDestroyOnLoad(gameObject);
        Debug.Log($"[GameUIPersist] '{key}' marked as DontDestroyOnLoad.");
    }

    private void OnDestroy()
    {
        string key = gameObject.name;
        if (_instances.TryGetValue(key, out var registered) && registered == this)
        {
            _instances.Remove(key);
        }

        if (_eventSystemInstance == this)
            _eventSystemInstance = null;
    }
}
