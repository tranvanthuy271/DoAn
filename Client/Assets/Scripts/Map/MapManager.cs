using UnityEngine;

/// <summary>
/// Map Manager - Quản lý thông tin map hiện tại
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("Map Configuration")]
    [Tooltip("Map ID của scene này (0 = Main map, 1, 2, 3... = các map khác)")]
    public int mapId = 0;
    
    [Tooltip("Tên map")]
    public string mapName = "Main Map";

    private static MapManager instance;
    public static MapManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log($"[MapManager] Map loaded: ID={mapId}, Name={mapName}");
    }

    /// <summary>
    /// Lấy Map ID hiện tại
    /// </summary>
    public int GetMapId()
    {
        return mapId;
    }

    /// <summary>
    /// Lấy Map Name hiện tại
    /// </summary>
    public string GetMapName()
    {
        return mapName;
    }
}
