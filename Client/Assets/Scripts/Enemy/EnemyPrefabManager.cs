using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy Prefab Manager - Quản lý mapping giữa Enemy ID và Enemy Prefabs
/// </summary>
public class EnemyPrefabManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyPrefabData
    {
        public int enemyId;
        public GameObject enemyPrefab;
        public string enemyName; // Tùy chọn, để debug
    }

    [Header("Enemy Prefabs Configuration")]
    [Tooltip("Danh sách Enemy Prefabs và Enemy IDs tương ứng")]
    public List<EnemyPrefabData> enemyPrefabs = new List<EnemyPrefabData>();

    private static EnemyPrefabManager instance;
    public static EnemyPrefabManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EnemyPrefabManager>();
            }
            return instance;
        }
    }

    private Dictionary<int, GameObject> enemyPrefabDict = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (transform.parent != null)
                transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            BuildDictionary();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void BuildDictionary()
    {
        enemyPrefabDict.Clear();
        foreach (var enemyData in enemyPrefabs)
        {
            if (enemyData.enemyPrefab != null)
            {
                if (enemyPrefabDict.ContainsKey(enemyData.enemyId))
                {
                    Debug.LogWarning($"[EnemyPrefabManager] Duplicate Enemy ID: {enemyData.enemyId}. Overwriting previous entry.");
                }
                enemyPrefabDict[enemyData.enemyId] = enemyData.enemyPrefab;
                Debug.Log($"[EnemyPrefabManager] Registered Enemy ID {enemyData.enemyId}: {enemyData.enemyName ?? enemyData.enemyPrefab.name}");
            }
            else
            {
                Debug.LogWarning($"[EnemyPrefabManager] Enemy ID {enemyData.enemyId} has null prefab!");
            }
        }
    }

    /// <summary>
    /// Lấy Enemy Prefab theo Enemy ID
    /// </summary>
    public GameObject GetEnemyPrefab(int enemyId)
    {
        if (enemyPrefabDict.TryGetValue(enemyId, out GameObject prefab))
        {
            return prefab;
        }
        
        Debug.LogWarning($"[EnemyPrefabManager] Enemy ID {enemyId} not found! Returning null.");
        return null;
    }

    /// <summary>
    /// Kiểm tra xem Enemy ID có tồn tại không
    /// </summary>
    public bool HasEnemyPrefab(int enemyId)
    {
        return enemyPrefabDict.ContainsKey(enemyId);
    }

    /// <summary>
    /// Lấy tất cả Enemy IDs đã đăng ký
    /// </summary>
    public List<int> GetAllEnemyIds()
    {
        return new List<int>(enemyPrefabDict.Keys);
    }

    private void OnValidate()
    {
        // Rebuild dictionary khi có thay đổi trong Inspector
        if (Application.isPlaying)
        {
            BuildDictionary();
        }
    }
}
