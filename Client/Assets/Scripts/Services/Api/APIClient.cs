using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Dungeon System DTOs
[System.Serializable]
public class DungeonConfigData
{
    public int    dungeon_id;
    public string dungeon_name;
    public string dungeon_type;       // "solo" | "multi"
    public int    map_id;
    public string map_name;
    public string scene_name;         // Tên scene Unity cần LoadScene()
    public int    max_players;
    public int    min_level_required;
    public int    time_limit_seconds; // 0 = không giới hạn
    public string description;
    public string thumbnail_icon_id;
    public int    boss_enemy_id;
    public string reward_json;
}

[System.Serializable]
public class DungeonListResponse
{
    public DungeonConfigData[] dungeons;
}

[System.Serializable]
public class DungeonSessionData
{
    public int    session_id;
    public int    dungeon_config_id;
    public string host_ip;
    public int    host_port;
    public int    current_players;
    public int    max_players;
    public string status; // "waiting" | "active" | "ended"
}

[System.Serializable]
public class DungeonSessionResponse
{
    public bool              has_session;
    public DungeonSessionData session;
}

[System.Serializable]
public class CreateDungeonSessionRequest
{
    public int    dungeon_config_id;
    public string host_ip;
    public int    host_port;
}

[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string token;
    public int user_id;
    public string username;
}

[System.Serializable]
public class RegisterResponse
{
    public string token;
    public int user_id;
    public string message;
}

[System.Serializable]
public class PlayerDataResponse
{
    public int user_id; // ID cß╗Âºa user sß╗ƒ hß╗Â»u player data n├áy
    public int player_id;
    public int level;
    public int experience;
    public int exp_required_for_next_level;
    public int exp_at_current_level;
    public int gold;
    public int silver;
    public int map_id;
    public int zone_id;
    public float position_x; // Vß╗ï tr├Â¡ X cuß╗æi c├╣ng khi out game
    public float position_y; // Vß╗ï tr├Â¡ Y cuß╗æi c├╣ng khi out game
    public BaseStats base_stats;
    public EquipmentData equipment;
    public PotentialStat[] potential_stats;
    public FinalStats final_stats;
    public InventoryItem[] inventory;
    public ApiSkillData[] skills;
    public int skill_points_available;
    public int potential_points_available;
    public string element_type;
    public int gene_tier;
    public int gene_exp;
    public bool is_hybrid;
    public string gender;
    public string character_name;
    // Hybrid Gene fields
    public string secondary_element;
    public int secondary_gene_tier;
    public int secondary_gene_exp;
    public int hybrid_id;
    public string hybrid_element_a;
    public string hybrid_element_b;
    public string hybrid_bonus_targets;    // CSV "Earth,Fire"
    public string hybrid_immune_elements;  // CSV "Water,Metal"
    public float hybrid_atk_bonus_pct;
    public string hybrid_prefab_path;      // Resources path cho CharacterLoader
    // Gene Tối Thượng (Ultimate Gene) fields
    public bool is_ultimate;               // Đã kích hoạt Gene Tối Thượng chưa
    public int ultimate_gene_exp;          // EXP tích lũy cho Gene Tối Thượng
    public string ultimate_aura_path;      // Resources path cho aura sau lưng
    public int bag_slots;                  // Số ô túi đồ hiện tại (mặc định 20)
    public BagEquippedItemData[] bag_equipped_items;
    public int Length => inventory?.Length ?? 0;

    // Helper accessors used by shared server/client code
    public int GetMaxHp()
    {
        if (final_stats != null && final_stats.max_hp > 0) return final_stats.max_hp;
        if (base_stats != null && base_stats.max_hp > 0) return base_stats.max_hp;
        return 0;
    }

    public int GetMaxMp()
    {
        if (final_stats != null && final_stats.max_mp > 0) return final_stats.max_mp;
        if (base_stats != null && base_stats.max_mp > 0) return base_stats.max_mp;
        return 0;
    }

    public int GetHp()
    {
        if (final_stats != null && final_stats.hp > 0) return final_stats.hp;
        if (base_stats != null) return base_stats.hp;
        return 0;
    }

    public int GetMp()
    {
        if (final_stats != null && final_stats.mp > 0) return final_stats.mp;
        if (base_stats != null) return base_stats.mp;
        return 0;
    }

    public int GetAttack()
    {
        if (final_stats != null) return final_stats.attack;
        if (base_stats != null) return base_stats.attack;
        return 10;
    }

    public int GetDefense()
    {
        if (final_stats != null) return final_stats.defense;
        return 0;
    }

    public float GetMoveSpeed()
    {
        if (final_stats != null && final_stats.move_speed > 0f) return final_stats.move_speed;
        return 5f;
    }
}

[System.Serializable]
public class BaseStats
{
    public int hp;
    public int max_hp;
    public int mp;
    public int max_mp;
    public int attack;
}

[System.Serializable]
public class FinalStats
{
    public int hp;
    public int max_hp;
    public int mp;
    public int max_mp;
    public int attack;
    public int defense;
    public float move_speed;
}

[System.Serializable]
public class EquipmentData
{
    public ApiItemData weapon;
    public ApiItemData armor;
    public ApiItemData pants;
    public ApiItemData boots;
}

[System.Serializable]
public class ApiItemData
{
    public int item_id;
    public string name;
    public int attack;
    public int hp;
    public float move_speed;
}

[System.Serializable]
public class PotentialStat
{
    public string stat_name;
    public int points;
}

[System.Serializable]
public class InventoryItem
{
    public int item_id;              // Old format (deprecated)
    public string name;              // Old format (deprecated)
    public int quantity;
    public int slot_index;           // Old format (deprecated)
    
    // New format (used by current system)
    public int slotIndex;
    public int itemTemplateId;
    public string itemCode;
    public string iconId;
    public bool isEquipped;
    public bool isLocked;      // item instance bị khóa
    public int upgradeLevel;   // bậc nâng cấp
    public string strOptions;  // stat options
}

[System.Serializable]
public class BagEquippedItemData
{
    public int quick_slot_index;
    public int item_template_id;
    public string item_code;
    public string item_name;
    public string icon_id;
    public int upgrade_level;
    public string str_options;
    public int slot_bonus;
    public bool is_locked;
}

[System.Serializable]
public class ApiSkillData
{
    public int skill_id;
    public string skill_name;
    public int level;
    public bool unlocked;
}

// Skill tab DTOs
[System.Serializable]
public class PlayerSkillInfo
{
    public int    skill_id;
    public string skill_code;
    public string skill_name;
    public string description;
    public string element_type;       // null = universal
    public int    max_level;
    public int    level_to_unlock;
    public int    current_level;
    public bool   can_upgrade;
    public int    next_level_player_req;
    public int    next_level_sp_cost;
    public string next_level_desc;
    public string icon_id;
    public int    gene_tier_required;
    public SkillLevelInfo[] level_details;
    // Runtime stats — client dùng để apply vào SkillData khi load
    public float  current_cooldown_sec;   // cooldown (giây) tại level hiện tại
    public float  current_effect_value;   // sát thương / heal / khoảng cách
    public int    current_mp_cost;        // MP tiêu khi dùng skill
    public float  current_total_effect_value;
    public float  current_attack_bonus;
    public float  current_hp_bonus;
    public float  current_mp_bonus;
    public float  current_defense_bonus;
    public float  current_evasion_bonus;
}

[System.Serializable]
public class SkillLevelInfo
{
    public int level;
    public int level_req;
    public int sp_cost;
    public float effect_value;
    public int mp_cost;
    public float cooldown_sec;
    public string desc;
}

[System.Serializable]
public class PlayerSkillsResponse
{
    public int              skill_points_available;
    public int              player_level;
    // Final attack stat của player (base + equipment + gene + potential).
    // SkillRuntimeLoader cộng vào current_effect_value của các skill gây sát thương.
    public int              player_final_attack;
    public PlayerSkillInfo[] skills;
}

[System.Serializable]
public class PotentialStatInfo
{
    public string stat_name;
    public string display_name;
    public int    current_points;
    public float  value_per_point;
    public float  total_value;
}

[System.Serializable]
public class PlayerPotentialResponse
{
    public int                potential_points_available;
    public int                player_level;
    public PotentialStatInfo[] stats;
}

[System.Serializable]
public class PotentialAllocationEntry
{
    public string stat_name;
    public int    points;
}

[System.Serializable]
public class PotentialAllocationRequest
{
    public PotentialAllocationEntry[] allocations;
}

public class APIClient : MonoBehaviour
{
    public static APIClient Instance { get; private set; }

    [Header("API Config")]
    public string baseURL = "http://localhost:5000/api";

    // Server root without the /api path segment (used by panels that construct /api/... URLs themselves).
    public static string BASE_URL
    {
        get
        {
            if (Instance == null) return ServerAddressConfig.Instance.ApiRoot;
            return ServerAddressConfig.Instance.ResolveApiRoot(Instance.baseURL);
        }
    }

    private void InitBaseUrl()
    {
        baseURL = ServerAddressConfig.Instance.ResolveApiUrl(baseURL);
    }

    private string jwtToken = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitBaseUrl();

        // Load token từ PlayerPrefs nếu có
        jwtToken = PlayerPrefs.GetString("JWT_TOKEN", "");
    }

    public string GetToken()
    {
        return jwtToken;
    }

    public void SetToken(string token)
    {
        jwtToken = token;
        PlayerPrefs.SetString("JWT_TOKEN", token);
    }

    public void ClearToken()
    {
        jwtToken = "";
        PlayerPrefs.DeleteKey("JWT_TOKEN");
    }

    public void ResetPlayerToStartMap(int playerId, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(ResetPlayerToStartMapCoroutine(playerId, onSuccess, onError));
    }

    private IEnumerator ResetPlayerToStartMapCoroutine(int playerId, Action onSuccess, Action<string> onError)
    {
        string json = "{\"reset_to_start_map\":true,\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest($"{baseURL}/player/{playerId}/position", "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                string serverMessage = www.downloadHandler != null ? www.downloadHandler.text : null;
                onError?.Invoke(!string.IsNullOrEmpty(serverMessage) ? serverMessage : www.error);
            }
        }
    }
    
    // Parse user_id tß╗Â½ JWT token (base64 decode payload)
    private int ParseUserIdFromJWT(string token)
    {
        try
        {
            // JWT format: header.payload.signature
            string[] parts = token.Split('.');
            if (parts.Length < 2)
            {
                return 0;
            }
            
            // Decode payload (phß║Âºn thß╗âŒ 2)
            string payload = parts[1];
            
            // Th├Â¬m padding nß║â”u cß║Âºn
            int padding = 4 - (payload.Length % 4);
            if (padding != 4)
            {
                payload += new string('=', padding);
            }
            
            // Base64 decode
            byte[] payloadBytes = System.Convert.FromBase64String(payload);
            string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            { /* JWT Payload: {payloadJson} */ }
            
            // Parse JSON ─æß╗â lß║Ñy user_id
            // JWT payload c├│ thß╗â c├│: {"sub":"1","unique_name":"1","user_id":"1",...}
            if (payloadJson.Contains("\"user_id\""))
            {
                // T├Â¼m "user_id":"X"
                int startIndex = payloadJson.IndexOf("\"user_id\"") + 9;
                int endIndex = payloadJson.IndexOf(",", startIndex);
                if (endIndex == -1)
                {
                    endIndex = payloadJson.IndexOf("}", startIndex);
                }
                
                if (endIndex > startIndex)
                {
                    string userIdStr = payloadJson.Substring(startIndex, endIndex - startIndex).Trim().Trim('"');
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        return userId;
                    }
                }
            }
            
            // Thß╗Â¡ parse "sub" nß║â”u kh├┤ng c├│ "user_id"
            if (payloadJson.Contains("\"sub\""))
            {
                int startIndex = payloadJson.IndexOf("\"sub\"") + 6;
                int endIndex = payloadJson.IndexOf(",", startIndex);
                if (endIndex == -1)
                {
                    endIndex = payloadJson.IndexOf("}", startIndex);
                }
                
                if (endIndex > startIndex)
                {
                    string userIdStr = payloadJson.Substring(startIndex, endIndex - startIndex).Trim().Trim('"');
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        return userId;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            { /* Lỗi: Error parsing JWT token: {ex.Message} */ }
        }
        
        return 0;
    }

    // Login
    public void Login(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoginCoroutine(username, password, onSuccess, onError));
    }

    private IEnumerator LoginCoroutine(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
    {
        LoginRequest request = new LoginRequest
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest($"{baseURL}/auth/login", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                { /* Login API Response: {responseText} */ }
                
                LoginResponse response = new LoginResponse();
                
                // Parse JSON thß╗Âº c├┤ng ─æß╗â ─æß║úm bß║úo lß║Ñy ─æ╞â–‘ß╗úc user_id
                try
                {
                    // Thß╗Â¡ parse bß║â–’ng JsonUtility tr╞â–‘ß╗Â¢c
                    response = JsonUtility.FromJson<LoginResponse>(responseText);
                    
                    // Nß║â”u user_id = 0, parse thß╗Âº c├┤ng tß╗Â½ JSON string
                    if (response.user_id == 0)
                    {
                        { /* Cảnh báo: user_id = 0 from JsonUtility, trying manual parse */ }
                        
                        // Parse thß╗Âº c├┤ng: t├Â¼m "user_id":X trong JSON
                        if (responseText.Contains("\"user_id\""))
                        {
                            int startIndex = responseText.IndexOf("\"user_id\"") + 9;
                            int endIndex = responseText.IndexOf(",", startIndex);
                            if (endIndex == -1)
                            {
                                endIndex = responseText.IndexOf("}", startIndex);
                            }
                            
                            if (endIndex > startIndex)
                            {
                                string userIdStr = responseText.Substring(startIndex, endIndex - startIndex).Trim().Trim(':').Trim();
                                if (int.TryParse(userIdStr, out int userId))
                                {
                                    { /* Parsed user_id manually: {userId} */ }
                                    response.user_id = userId;
                                }
                            }
                        }
                    }
                    
                    // Nß║â”u vß║Â½n = 0, thß╗Â¡ parse tß╗Â½ JWT token
                    if (response.user_id == 0 && !string.IsNullOrEmpty(response.token))
                    {
                        int userIdFromToken = ParseUserIdFromJWT(response.token);
                        if (userIdFromToken > 0)
                        {
                            { /* Got user_id from JWT token: {userIdFromToken} */ }
                            response.user_id = userIdFromToken;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    { /* Lỗi: Error parsing login response: {ex.Message} */ }
                }
                
                { /* Final LoginResponse - user_id: {response.user_id}, username: {response.username}, token length: {response.token?.Length ?? 0} */ }
                
                SetToken(response.token);
                onSuccess?.Invoke(response);
            }
            else
            {
                    // ╞Â»u ti├Â¬n hiß╗ân thß╗ï message tß╗Â½ server (v├Â¡ dß╗Ñ: "Sai username hoß║╖c password.")
                    string serverMessage = www.downloadHandler != null ? www.downloadHandler.text : null;
                    if (!string.IsNullOrEmpty(serverMessage))
                    {
                        onError?.Invoke(serverMessage);
                    }
                    else
                    {
                        onError?.Invoke(www.error);
                    }
            }
        }
    }

    // Register
    public void Register(string username, string email, string password, Action<RegisterResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(RegisterCoroutine(username, email, password, onSuccess, onError));
    }

    private IEnumerator RegisterCoroutine(string username, string email, string password, Action<RegisterResponse> onSuccess, Action<string> onError)
    {
        RegisterRequest request = new RegisterRequest
        {
            username = username,
            email = email,
            password = password
        };

        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest($"{baseURL}/auth/register", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(www.downloadHandler.text);
                SetToken(response.token);
                onSuccess?.Invoke(response);
            }
            else
            {
                    // ╞Â»u ti├Â¬n hiß╗ân thß╗ï message tß╗Â½ server
                    string serverMessage = www.downloadHandler != null ? www.downloadHandler.text : null;
                    if (!string.IsNullOrEmpty(serverMessage))
                    {
                        onError?.Invoke(serverMessage);
                    }
                    else
                    {
                        onError?.Invoke(www.error);
                    }
            }
        }
    }

    // Load Player Data
    public void LoadPlayerData(int playerId, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoadPlayerDataCoroutine(playerId, onSuccess, onError));
    }

    private IEnumerator LoadPlayerDataCoroutine(int playerId, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest www = UnityWebRequest.Get($"{baseURL}/player/{playerId}/data"))
        {
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                PlayerDataResponse response = JsonUtility.FromJson<PlayerDataResponse>(www.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(www.error);
            }
        }
    }

    // Create Player (Chß╗ìn hß╗ç ban ─æß║Âºu)
    public void CreatePlayer(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreatePlayerCoroutine(elementType, gender, characterName, onSuccess, onError));
    }

    private IEnumerator CreatePlayerCoroutine(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        // gender được server tự suy ra từ elementType, nhưng vẫn gửi để tương thích ngược
        string escapedName = characterName.Replace("\"", "\\\"").Replace("\\", "\\\\");
        string json = $"{{\"element_type\":\"{elementType}\",\"character_name\":\"{escapedName}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest($"{baseURL}/player/create", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                PlayerDataResponse response = JsonUtility.FromJson<PlayerDataResponse>(www.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(www.error);
            }
        }
    }

    // Inventory DTOs (used by NetworkInventory server-side direct API calls)
    [System.Serializable]
    public class AddInventoryItemRequest
    {
        public int    itemTemplateId;
        public string itemCode;
        public string iconId;
        public int    quantity;
        public int    slot_index;
    }

    [System.Serializable]
    public class AddInventoryItemsRequest
    {
        public AddInventoryItemRequest[] items;
    }

    // GENE SLOT 2 METHODS

    // Tải thông tin tóm tắt cả 2 gene slot để hiển thị màn SelectGene.
    public void LoadGeneSlots(int playerId, Action<GeneSlotsResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoadGeneSlotsCoroutine(playerId, onSuccess, onError));
    }

    private IEnumerator LoadGeneSlotsCoroutine(int playerId, Action<GeneSlotsResponse> onSuccess, Action<string> onError)
    {
        using (var www = UnityWebRequest.Get($"{baseURL}/player/{playerId}/gene-slots"))
        {
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(JsonUtility.FromJson<GeneSlotsResponse>(www.downloadHandler.text));
            else
                onError?.Invoke(www.error);
        }
    }

    // Tạo nhân vật hệ gene 2 mới.
    public void CreatePlayer2(string elementType, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreatePlayer2Coroutine(elementType, characterName, onSuccess, onError));
    }

    private IEnumerator CreatePlayer2Coroutine(string elementType, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        string escapedName = characterName.Replace("\"", "\\\"").Replace("\\", "\\\\");
        string json = $"{{\"element_type\":\"{elementType}\",\"character_name\":\"{escapedName}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{baseURL}/player/create2", "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(JsonUtility.FromJson<PlayerDataResponse>(www.downloadHandler.text));
            else
                onError?.Invoke(www.error);
        }
    }

    // Tải full dữ liệu nhân vật hệ gene 2.
    public void LoadPlayer2Data(int playerId, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoadPlayer2DataCoroutine(playerId, onSuccess, onError));
    }

    private IEnumerator LoadPlayer2DataCoroutine(int playerId, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        using (var www = UnityWebRequest.Get($"{baseURL}/player/{playerId}/data2"))
        {
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(JsonUtility.FromJson<PlayerDataResponse>(www.downloadHandler.text));
            else
                onError?.Invoke(www.error);
        }
    }
}

// Gene Slot DTOs  (SelectGene scene)
[System.Serializable]
public class GeneSlotInfo
{
    public int    slot;
    public bool   exists;
    public bool   is_unlocked;
    public string character_name;
    public string gender;
    public int    level;
    public string element_type;
    public int    gene_tier;
    public bool   is_hybrid;
}

[System.Serializable]
public class GeneSlotsResponse
{
    public GeneSlotInfo slot1;
    public GeneSlotInfo slot2;
    public bool         gene2_unlocked;
}

[System.Serializable]
public class CreatePlayer2Request
{
    public string character_name;
    public string element_type;
}
