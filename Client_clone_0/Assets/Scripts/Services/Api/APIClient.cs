using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// â”€â”€ Dungeon System DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
[System.Serializable]
public class DungeonConfigData
{
    public int    dungeon_id;
    public string dungeon_name;
    public string dungeon_type;       // "solo" | "multi"
    public int    map_id;
    public string map_name;
    public string scene_name;         // TÃªn scene Unity cáº§n LoadScene()
    public int    max_players;
    public int    min_level_required;
    public int    time_limit_seconds; // 0 = khÃ´ng giá»›i háº¡n
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
// â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
    public int user_id; // ID cÃŸâ•—Âºa user sÃŸâ•—Æ’ hÃŸâ•—Â»u player data nâ”œÃ¡y
    public int player_id;
    public int level;
    public int experience;
    public int exp_required_for_next_level;
    public int exp_at_current_level;
    public int gold;
    public int silver;
    public int map_id;
    public int zone_id;
    public float position_x; // VÃŸâ•—Ã¯ trâ”œÂ¡ X cuÃŸâ•—Ã¦i câ”œâ•£ng khi out game
    public float position_y; // VÃŸâ•—Ã¯ trâ”œÂ¡ Y cuÃŸâ•—Ã¦i câ”œâ•£ng khi out game
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
    // â”€â”€ Hybrid Gene fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
    public int bag_slots;                  // Sá»‘ Ã´ tÃºi Ä‘á»“ hiá»‡n táº¡i (máº·c Ä‘á»‹nh 20)
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
    public bool isLocked;      // item instance bá»‹ khÃ³a
    public int upgradeLevel;   // báº­c nÃ¢ng cáº¥p
    public string strOptions;  // stat options
}

[System.Serializable]
public class ApiSkillData
{
    public int skill_id;
    public string skill_name;
    public int level;
    public bool unlocked;
}

// â”€â”€ Skill tab DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
    // â”€â”€ Runtime stats â€” client dÃ¹ng Ä‘á»ƒ apply vÃ o SkillData khi load â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public float  current_cooldown_sec;   // cooldown (giÃ¢y) táº¡i level hiá»‡n táº¡i
    public float  current_effect_value;   // sÃ¡t thÆ°Æ¡ng / heal / khoáº£ng cÃ¡ch
    public int    current_mp_cost;        // MP tiÃªu khi dÃ¹ng skill
}

[System.Serializable]
public class PlayerSkillsResponse
{
    public int              skill_points_available;
    public int              player_level;
    /// <summary>
    /// Final attack stat cá»§a player (base + equipment + gene + potential).
    /// SkillRuntimeLoader cá»™ng vÃ o current_effect_value cá»§a cÃ¡c skill gÃ¢y sÃ¡t thÆ°Æ¡ng.
    /// </summary>
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

    /// <summary>Server root without the /api path segment (used by panels that construct /api/... URLs themselves).</summary>
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

        // Load token tá»« PlayerPrefs náº¿u cÃ³
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
    
    /// <summary>
    /// Parse user_id tÃŸâ•—Â½ JWT token (base64 decode payload)
    /// </summary>
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
            
            // Decode payload (phÃŸâ•‘Âºn thÃŸâ•—âŒ 2)
            string payload = parts[1];
            
            // Thâ”œÂ¬m padding nÃŸâ•‘â”u cÃŸâ•‘Âºn
            int padding = 4 - (payload.Length % 4);
            if (padding != 4)
            {
                payload += new string('=', padding);
            }
            
            // Base64 decode
            byte[] payloadBytes = System.Convert.FromBase64String(payload);
            string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            Debug.Log($"JWT Payload: {payloadJson}");
            
            // Parse JSON â”€Ã¦ÃŸâ•—Ã¢ lÃŸâ•‘Ã‘y user_id
            // JWT payload câ”œâ”‚ thÃŸâ•—Ã¢ câ”œâ”‚: {"sub":"1","unique_name":"1","user_id":"1",...}
            if (payloadJson.Contains("\"user_id\""))
            {
                // Tâ”œÂ¼m "user_id":"X"
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
            
            // ThÃŸâ•—Â¡ parse "sub" nÃŸâ•‘â”u khâ”œâ”¤ng câ”œâ”‚ "user_id"
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
            Debug.LogError($"Error parsing JWT token: {ex.Message}");
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
                Debug.Log($"Login API Response: {responseText}");
                
                LoginResponse response = new LoginResponse();
                
                // Parse JSON thÃŸâ•—Âº câ”œâ”¤ng â”€Ã¦ÃŸâ•—Ã¢ â”€Ã¦ÃŸâ•‘Ãºm bÃŸâ•‘Ãºo lÃŸâ•‘Ã‘y â”€Ã¦â•žâ–‘ÃŸâ•—Ãºc user_id
                try
                {
                    // ThÃŸâ•—Â¡ parse bÃŸâ•‘â–’ng JsonUtility trâ•žâ–‘ÃŸâ•—Â¢c
                    response = JsonUtility.FromJson<LoginResponse>(responseText);
                    
                    // NÃŸâ•‘â”u user_id = 0, parse thÃŸâ•—Âº câ”œâ”¤ng tÃŸâ•—Â½ JSON string
                    if (response.user_id == 0)
                    {
                        Debug.LogWarning("user_id = 0 from JsonUtility, trying manual parse...");
                        
                        // Parse thÃŸâ•—Âº câ”œâ”¤ng: tâ”œÂ¼m "user_id":X trong JSON
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
                                    Debug.Log($"Parsed user_id manually: {userId}");
                                    response.user_id = userId;
                                }
                            }
                        }
                    }
                    
                    // NÃŸâ•‘â”u vÃŸâ•‘Â½n = 0, thÃŸâ•—Â¡ parse tÃŸâ•—Â½ JWT token
                    if (response.user_id == 0 && !string.IsNullOrEmpty(response.token))
                    {
                        int userIdFromToken = ParseUserIdFromJWT(response.token);
                        if (userIdFromToken > 0)
                        {
                            Debug.Log($"Got user_id from JWT token: {userIdFromToken}");
                            response.user_id = userIdFromToken;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error parsing login response: {ex.Message}");
                }
                
                Debug.Log($"Final LoginResponse - user_id: {response.user_id}, username: {response.username}, token length: {response.token?.Length ?? 0}");
                
                SetToken(response.token);
                onSuccess?.Invoke(response);
            }
            else
            {
                    // â•žÂ»u tiâ”œÂ¬n hiÃŸâ•—Ã¢n thÃŸâ•—Ã¯ message tÃŸâ•—Â½ server (vâ”œÂ¡ dÃŸâ•—Ã‘: "Sai username hoÃŸâ•‘â•–c password.")
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
                    // â•žÂ»u tiâ”œÂ¬n hiÃŸâ•—Ã¢n thÃŸâ•—Ã¯ message tÃŸâ•—Â½ server
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

    // Create Player (ChÃŸâ•—Ã¬n hÃŸâ•—Ã§ ban â”€Ã¦ÃŸâ•‘Âºu)
    public void CreatePlayer(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreatePlayerCoroutine(elementType, gender, characterName, onSuccess, onError));
    }

    private IEnumerator CreatePlayerCoroutine(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        // gender Ä‘Æ°á»£c server tá»± suy ra tá»« elementType, nhÆ°ng váº«n gá»­i Ä‘á»ƒ tÆ°Æ¡ng thÃ­ch ngÆ°á»£c
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

    // ── Inventory DTOs (used by NetworkInventory server-side direct API calls) ────────────────
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
}