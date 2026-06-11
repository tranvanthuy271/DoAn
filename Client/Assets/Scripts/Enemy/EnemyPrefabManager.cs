using UnityEngine;
using System.Collections.Generic;

// Enemy Prefab Manager - Quản lý mapping giữa Enemy ID và Enemy Prefabs
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
        { /* BuildDictionary START  total entries in list: {enemyPrefabs.Count} */ }
        foreach (var enemyData in enemyPrefabs)
        {
            if (enemyData.enemyPrefab != null)
            {
                if (enemyPrefabDict.ContainsKey(enemyData.enemyId))
                {
                    { /* Cảnh báo: DUPLICATE Enemy ID: {enemyData.enemyId} (prefab='{enemyData.enemyPrefab.name}'). Overwriting previous entry */ }
                }
                enemyPrefabDict[enemyData.enemyId] = enemyData.enemyPrefab;
                { /* Registered enemyId={enemyData.enemyId} prefab='{enemyData.enemyPrefab.name}' label='{enemyData.enemyName}' */ }
            }
            else
            {
                { /* Cảnh báo: enemyId={enemyData.enemyId} has NULL prefab  entry bị bỏ qua */ }
            }
        }
        { /* BuildDictionary DONE  registered keys: [{string.Join( */ }
    }

    // Lấy Enemy Prefab theo Enemy ID
    public GameObject GetEnemyPrefab(int enemyId)
    {
        if (enemyPrefabDict.TryGetValue(enemyId, out GameObject prefab))
        {
            return prefab;
        }

        foreach (var enemyData in enemyPrefabs)
        {
            if (enemyData.enemyPrefab == null)
                continue;

            { /* Cảnh báo: enemyId={enemyId} KH\u00d4NG \u0110\u0102NG K\u00dd \u2192 fallback v\u1ec1 prefab '{enemyData.enemyPrefab.name}' (enemyId={enemyData.enemyId}). H\u00e3y th\u00eam mapping enemyId={enemyId} v\u00e0o EnemyPrefabManager trong ServerScene n\u1ebfu c\u1ea7n boss \u0111\u00fang */ }
            return enemyData.enemyPrefab;
        }

        { /* Lỗi: enemyId={enemyId} KH\u00d4NG T\u00ccM TH\u1ea4Y v\u00e0 list r\u1ed7ng \u2192 tr\u1ea3 v\u1ec1 null */ }
        return null;
    }

    // Kiểm tra xem Enemy ID có tồn tại không
    public bool HasEnemyPrefab(int enemyId)
    {
        return enemyPrefabDict.ContainsKey(enemyId);
    }

    // Lấy tất cả Enemy IDs đã đăng ký
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
