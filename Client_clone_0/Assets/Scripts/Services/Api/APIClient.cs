using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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
    public int user_id; // ID cß╗ºa user sß╗ƒ hß╗»u player data n├áy
    public int player_id;
    public int level;
    public int experience;
    public int exp_required_for_next_level;
    public int gold;
    public int silver;
    public int map_id;
    public float position_x; // Vß╗ï tr├¡ X cuß╗æi c├╣ng khi out game
    public float position_y; // Vß╗ï tr├¡ Y cuß╗æi c├╣ng khi out game
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
}

[System.Serializable]
public class ApiSkillData
{
    public int skill_id;
    public string skill_name;
    public int level;
    public bool unlocked;
}

// ΓöÇΓöÇ Skill tab DTOs ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
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
}

[System.Serializable]
public class PlayerSkillsResponse
{
    public int              skill_points_available;
    public int              player_level;
    public PlayerSkillInfo[] skills;
}

// ΓöÇΓöÇ Potential tab DTOs ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
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

public class APIClient : MonoBehaviour
{
    public static APIClient Instance { get; private set; }

    [Header("API Config")]
    public string baseURL = "http://localhost:5000/api"; // Thay ─æß╗òi theo server cß╗ºa bß║ín

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

        // Load token tß╗½ PlayerPrefs nß║┐u c├│
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
    
    /// <summary>
    /// Parse user_id tß╗½ JWT token (base64 decode payload)
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
            
            // Decode payload (phß║ºn thß╗⌐ 2)
            string payload = parts[1];
            
            // Th├¬m padding nß║┐u cß║ºn
            int padding = 4 - (payload.Length % 4);
            if (padding != 4)
            {
                payload += new string('=', padding);
            }
            
            // Base64 decode
            byte[] payloadBytes = System.Convert.FromBase64String(payload);
            string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            Debug.Log($"JWT Payload: {payloadJson}");
            
            // Parse JSON ─æß╗â lß║Ñy user_id
            // JWT payload c├│ thß╗â c├│: {"sub":"1","unique_name":"1","user_id":"1",...}
            if (payloadJson.Contains("\"user_id\""))
            {
                // T├¼m "user_id":"X"
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
            
            // Thß╗¡ parse "sub" nß║┐u kh├┤ng c├│ "user_id"
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
                
                // Parse JSON thß╗º c├┤ng ─æß╗â ─æß║úm bß║úo lß║Ñy ─æ╞░ß╗úc user_id
                try
                {
                    // Thß╗¡ parse bß║▒ng JsonUtility tr╞░ß╗¢c
                    response = JsonUtility.FromJson<LoginResponse>(responseText);
                    
                    // Nß║┐u user_id = 0, parse thß╗º c├┤ng tß╗½ JSON string
                    if (response.user_id == 0)
                    {
                        Debug.LogWarning("user_id = 0 from JsonUtility, trying manual parse...");
                        
                        // Parse thß╗º c├┤ng: t├¼m "user_id":X trong JSON
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
                    
                    // Nß║┐u vß║½n = 0, thß╗¡ parse tß╗½ JWT token
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
                    // ╞»u ti├¬n hiß╗ân thß╗ï message tß╗½ server (v├¡ dß╗Ñ: "Sai username hoß║╖c password.")
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
                    // ╞»u ti├¬n hiß╗ân thß╗ï message tß╗½ server
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

    // Create Player (Chß╗ìn hß╗ç ban ─æß║ºu)
    public void CreatePlayer(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreatePlayerCoroutine(elementType, gender, characterName, onSuccess, onError));
    }

    private IEnumerator CreatePlayerCoroutine(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        // Escape JSON string ─æß╗â tr├ính lß╗ùi vß╗¢i k├╜ tß╗▒ ─æß║╖c biß╗çt
        string escapedName = characterName.Replace("\"", "\\\"").Replace("\\", "\\\\");
        string json = $"{{\"element_type\":\"{elementType}\",\"gender\":\"{gender}\",\"character_name\":\"{escapedName}\"}}";
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

    /// <summary>
    /// Update position cß╗ºa player l├¬n server
    /// </summary>
    public void UpdatePlayerPosition(int playerId, int mapId, float positionX, float positionY, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(UpdatePlayerPositionCoroutine(playerId, mapId, positionX, positionY, onSuccess, onError));
    }

    private System.Collections.IEnumerator UpdatePlayerPositionCoroutine(int playerId, int mapId, float positionX, float positionY, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/position";
        
        // Tß║ío JSON string thß╗º c├┤ng v├¼ JsonUtility kh├┤ng hß╗ù trß╗ú anonymous objects
        string jsonData = $"{{\"map_id\":{mapId},\"position_x\":{positionX},\"position_y\":{positionY}}}";
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Put(url, jsonData))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[APIClient] Position updated successfully: Map={mapId}, X={positionX}, Y={positionY}");
                onSuccess?.Invoke();
            }
            else
            {
                Debug.LogError($"[APIClient] Failed to update position: {www.error}");
                onError?.Invoke(www.error);
            }
        }
    }

    /// <summary>
    /// Update player data (batch update) l├¬n server
    /// </summary>
    public void UpdatePlayerData(int playerId, string jsonData, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(UpdatePlayerDataCoroutine(playerId, jsonData, onSuccess, onError));
    }

    private System.Collections.IEnumerator UpdatePlayerDataCoroutine(int playerId, string jsonData, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/data";
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Put(url, jsonData))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[APIClient] Player data updated successfully for player {playerId}");
                onSuccess?.Invoke();
            }
            else
            {
                Debug.LogError($"[APIClient] Failed to update player data: {www.error}");
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    Debug.LogError($"[APIClient] Response: {www.downloadHandler.text}");
                }
                onError?.Invoke(www.error);
            }
        }
    }

    /// <summary>
    /// Th├¬m items v├áo inventory cß╗ºa player
    /// </summary>
    [System.Serializable]
    public class AddInventoryItemRequest
    {
        public int itemTemplateId;
        public string itemCode;
        public string iconId;
        public int quantity;
    }

    [System.Serializable]
    public class AddInventoryItemsRequest
    {
        public AddInventoryItemRequest[] items;
    }

    public void AddItemsToInventory(int playerId, AddInventoryItemRequest[] items, System.Action<string> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(AddItemsToInventoryCoroutine(playerId, items, onSuccess, onError));
    }

    private System.Collections.IEnumerator AddItemsToInventoryCoroutine(int playerId, AddInventoryItemRequest[] items, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/inventory/add";
        
        AddInventoryItemsRequest requestBody = new AddInventoryItemsRequest
        {
            items = items
        };
        
        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        
        using (UnityEngine.Networking.UnityWebRequest www = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[APIClient] Items added to inventory successfully: {responseText}");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] Failed to add items to inventory: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Xóa toàn bộ inventory và equipment của player (debug)
    /// POST /api/player/{playerId}/inventory/clear
    /// </summary>
    public void ClearInventory(int playerId, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(ClearInventoryCoroutine(playerId, onSuccess, onError));
    }

    private System.Collections.IEnumerator ClearInventoryCoroutine(int playerId, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/inventory/clear";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");

        using (UnityEngine.Networking.UnityWebRequest www = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[APIClient] Inventory cleared for player {playerId}");
                onSuccess?.Invoke();
            }
            else
            {
                string errMsg = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] Failed to clear inventory: {errMsg}");
                onError?.Invoke(errMsg);
            }
        }
    }

    /// <summary>
    /// Lß║Ñy tß║Ñt cß║ú item templates tß╗½ server
    /// </summary>
    [System.Serializable]
    public class ItemTemplatesResponse
    {
        public int count;
        public ItemTemplateDto[] item_templates;
    }

    public void GetItemTemplates(System.Action<ItemTemplateDto[]> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetItemTemplatesCoroutine(onSuccess, onError));
    }

    private System.Collections.IEnumerator GetItemTemplatesCoroutine(System.Action<ItemTemplateDto[]> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/item/templates";
        Debug.Log($"[APIClient] ≡ƒîÉ Sending GET request to: {url}");
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            // Kh├┤ng cß║ºn Authorization v├¼ endpoint l├á AllowAnonymous
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[APIClient] Γ£à Item templates response received - Length: {responseText.Length} chars");
                Debug.Log($"[APIClient] ≡ƒôä Response preview: {responseText.Substring(0, Mathf.Min(200, responseText.Length))}...");
                
                try
                {
                    // Parse JSON response
                    ItemTemplatesResponse response = JsonUtility.FromJson<ItemTemplatesResponse>(responseText);
                    
                    if (response != null && response.item_templates != null)
                    {
                        Debug.Log($"[APIClient] Γ£à Parsed {response.item_templates.Length} item templates successfully");
                        onSuccess?.Invoke(response.item_templates);
                    }
                    else
                    {
                        Debug.LogError("[APIClient] Γ¥î Failed to parse item templates response - response or item_templates is null");
                        onError?.Invoke("Failed to parse response");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] Γ¥î Error parsing item templates: {ex.Message}");
                    Debug.LogError($"[APIClient] Stack trace: {ex.StackTrace}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] Γ¥î Failed to load item templates: {errorMessage}");
                Debug.LogError($"[APIClient] Response code: {www.responseCode}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Fetch inventory tß╗½ DB cho player (d├╣ng ─æß╗â refresh UI)
    /// </summary>
    public void GetPlayerInventory(int playerId, System.Action<InventoryItem[]> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetPlayerInventoryCoroutine(playerId, onSuccess, onError));
    }

    private System.Collections.IEnumerator GetPlayerInventoryCoroutine(int playerId, System.Action<InventoryItem[]> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/data";
        Debug.Log($"[APIClient] ≡ƒöä Fetching inventory from DB for player {playerId}...");
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                
                try
                {
                    PlayerDataResponse response = JsonUtility.FromJson<PlayerDataResponse>(responseText);
                    
                    if (response != null)
                    {
                        Debug.Log($"[APIClient] Γ£à Inventory fetched successfully: {response.inventory?.Length ?? 0} items");
                        onSuccess?.Invoke(response.inventory ?? new InventoryItem[0]);
                    }
                    else
                    {
                        Debug.LogError("[APIClient] Γ¥î Failed to parse player data");
                        onError?.Invoke("Failed to parse response");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] Γ¥î Error parsing inventory: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] Γ¥î Failed to fetch inventory: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    // ==================== EQUIPMENT API ====================

    /// <summary>
    /// Trang bß╗ï item tß╗½ inventory
    /// </summary>
    public void EquipItem(int playerId, int inventorySlotIndex, System.Action<string> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(EquipItemCoroutine(playerId, inventorySlotIndex, onSuccess, onError));
    }

    private System.Collections.IEnumerator EquipItemCoroutine(int playerId, int inventorySlotIndex, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/equipment/equip";
        string json = $"{{\"inventorySlotIndex\":{inventorySlotIndex}}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        Debug.Log($"[APIClient] ≡ƒÄ« Equip item: playerId={playerId}, slotIndex={inventorySlotIndex}");

        using (UnityEngine.Networking.UnityWebRequest www = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[APIClient] Γ£à Equip th├ánh c├┤ng: {responseText}");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] Γ¥î Equip thß║Ñt bß║íi: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Th├ío trang bß╗ï
    /// </summary>
    public void UnequipItem(int playerId, string equipmentSlot, System.Action<string> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(UnequipItemCoroutine(playerId, equipmentSlot, onSuccess, onError));
    }

    private System.Collections.IEnumerator UnequipItemCoroutine(int playerId, string equipmentSlot, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/equipment/unequip";
        string json = $"{{\"equipmentSlot\":\"{equipmentSlot}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        Debug.Log($"[APIClient] ≡ƒöº Unequip: playerId={playerId}, slot={equipmentSlot}");

        using (UnityEngine.Networking.UnityWebRequest www = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[APIClient] Γ£à Unequip th├ánh c├┤ng: {responseText}");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] Γ¥î Unequip thß║Ñt bß║íi: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Lß║Ñy th├┤ng tin trang bß╗ï cß╗ºa player
    /// </summary>
    public void GetPlayerEquipment(int playerId, System.Action<PlayerEquipmentDto> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetPlayerEquipmentCoroutine(playerId, onSuccess, onError));
    }

    private System.Collections.IEnumerator GetPlayerEquipmentCoroutine(int playerId, System.Action<PlayerEquipmentDto> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/equipment";
        Debug.Log($"[APIClient] ≡ƒöä Fetching equipment for player {playerId}...");

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[APIClient] Γ£à Equipment response: {responseText}");

                try
                {
                    // Response format: { "player_id": 1, "equipment": { "weapon": {...}, ... } }
                    // Parse equipment tß╗½ wrapper
                    var wrapper = JsonUtility.FromJson<EquipmentResponseWrapper>(responseText);
                    if (wrapper != null && wrapper.equipment != null)
                    {
                        onSuccess?.Invoke(wrapper.equipment);
                    }
                    else
                    {
                        // Try parsing trß╗▒c tiß║┐p
                        var equipment = JsonUtility.FromJson<PlayerEquipmentDto>(responseText);
                        onSuccess?.Invoke(equipment ?? new PlayerEquipmentDto());
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] Γ¥î Error parsing equipment: {ex.Message}");
                    // Trß║ú vß╗ü equipment trß╗æng thay v├¼ lß╗ùi
                    onSuccess?.Invoke(new PlayerEquipmentDto());
                }
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] Γ¥î Failed to fetch equipment: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Wrapper ─æß╗â parse response tß╗½ GET /api/player/{id}/equipment
    /// </summary>
    [System.Serializable]
    private class EquipmentResponseWrapper
    {
        public int player_id;
        public PlayerEquipmentDto equipment;
    }

    // =====================================================================
    // SKILL API
    // =====================================================================

    /// <summary>
    /// Lß║Ñy to├án bß╗Ö skill templates k├¿m level hiß╗çn tß║íi cß╗ºa player.
    /// GET /api/player/{id}/skills
    /// </summary>
    public void GetPlayerSkills(int playerId,
        System.Action<PlayerSkillsResponse> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetPlayerSkillsCoroutine(playerId, onSuccess, onError));
    }

    private IEnumerator GetPlayerSkillsCoroutine(int playerId,
        System.Action<PlayerSkillsResponse> onSuccess,
        System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/skills";
        using (var www = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string text = www.downloadHandler.text;
                Debug.Log($"[APIClient] Skills response: {text.Substring(0, Mathf.Min(200, text.Length))}");
                try
                {
                    var response = JsonUtility.FromJson<PlayerSkillsResponse>(text);
                    onSuccess?.Invoke(response);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] Parse skills error: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] GetPlayerSkills failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    /// <summary>
    /// N├óng cß║Ñp 1 skill l├¬n level kß║┐ tiß║┐p.
    /// POST /api/player/{id}/skills/upgrade
    /// Body: { "skill_id": 1 }
    /// </summary>
    public void UpgradeSkill(int playerId, int skillId,
        System.Action<string> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(UpgradeSkillCoroutine(playerId, skillId, onSuccess, onError));
    }

    private IEnumerator UpgradeSkillCoroutine(int playerId, int skillId,
        System.Action<string> onSuccess,
        System.Action<string> onError)
    {
        string url  = $"{baseURL}/player/{playerId}/skills/upgrade";
        string json = $"{{\"skill_id\":{skillId}}}";
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[APIClient] UpgradeSkill OK: {www.downloadHandler.text}");
                onSuccess?.Invoke(www.downloadHandler.text);
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] UpgradeSkill failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    // =====================================================================
    // POTENTIAL API
    // =====================================================================

    /// <summary>
    /// Lß║Ñy th├┤ng tin tiß╗üm n─âng cß╗ºa player.
    /// GET /api/player/{id}/potential
    /// </summary>
    public void GetPlayerPotential(int playerId,
        System.Action<PlayerPotentialResponse> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetPlayerPotentialCoroutine(playerId, onSuccess, onError));
    }

    private IEnumerator GetPlayerPotentialCoroutine(int playerId,
        System.Action<PlayerPotentialResponse> onSuccess,
        System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/potential";
        using (var www = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string text = www.downloadHandler.text;
                Debug.Log($"[APIClient] Potential response: {text}");
                try
                {
                    var response = JsonUtility.FromJson<PlayerPotentialResponse>(text);
                    onSuccess?.Invoke(response);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] Parse potential error: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] GetPlayerPotential failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    /// <summary>
    /// ─Éß║ºu t╞░ 1 ─æiß╗âm tiß╗üm n─âng v├áo chß╗ë sß╗æ ─æ╞░ß╗úc chß╗ìn.
    /// POST /api/player/{id}/potential/upgrade
    /// Body: { "stat_name": "attack" }
    /// </summary>
    public void UpgradePotentialStat(int playerId, string statName,
        System.Action<string> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(UpgradePotentialStatCoroutine(playerId, statName, onSuccess, onError));
    }

    private IEnumerator UpgradePotentialStatCoroutine(int playerId, string statName,
        System.Action<string> onSuccess,
        System.Action<string> onError)
    {
        string url  = $"{baseURL}/player/{playerId}/potential/upgrade";
        string json = $"{{\"stat_name\":\"{statName}\"}}";
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[APIClient] UpgradePotential OK: {www.downloadHandler.text}");
                onSuccess?.Invoke(www.downloadHandler.text);
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] UpgradePotential failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    // =====================================================================
    // EQUIPMENT UPGRADE
    // =====================================================================

    /// <summary>
    /// Lấy config nâng cấp cho 1 bậc: GET /api/upgrade/config?itemId=X&targetLevel=Y
    /// </summary>
    public void GetUpgradeConfig(
        int itemId, int targetLevel,
        System.Action<UpgradeConfigDto> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetUpgradeConfigCoroutine(itemId, targetLevel, onSuccess, onError));
    }

    private IEnumerator GetUpgradeConfigCoroutine(
        int itemId, int targetLevel,
        System.Action<UpgradeConfigDto> onSuccess,
        System.Action<string> onError)
    {
        string url = $"{baseURL}/upgrade/config?itemId={itemId}&targetLevel={targetLevel}";
        using (var www = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var cfg = JsonUtility.FromJson<UpgradeConfigDto>(www.downloadHandler.text);
                    onSuccess?.Invoke(cfg);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] GetUpgradeConfig parse error: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] GetUpgradeConfig failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    /// <summary>
    /// Nâng cấp trang bị: POST /api/upgrade/equipment
    /// </summary>
    public void UpgradeEquipment(
        UpgradeRequestDto request,
        System.Action<UpgradeResponseDto> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(UpgradeEquipmentCoroutine(request, onSuccess, onError));
    }

    private IEnumerator UpgradeEquipmentCoroutine(
        UpgradeRequestDto request,
        System.Action<UpgradeResponseDto> onSuccess,
        System.Action<string> onError)
    {
        string url  = $"{baseURL}/upgrade/equipment";
        string json = JsonUtility.ToJson(request);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var resp = JsonUtility.FromJson<UpgradeResponseDto>(www.downloadHandler.text);
                    onSuccess?.Invoke(resp);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] UpgradeEquipment parse error: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] UpgradeEquipment failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    /// <summary>
    /// Lấy toàn bộ option templates: GET /api/upgrade/options
    /// </summary>
    public void GetOptionTemplates(
        System.Action<OptionTemplateDto[]> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetOptionTemplatesCoroutine(onSuccess, onError));
    }

    private IEnumerator GetOptionTemplatesCoroutine(
        System.Action<OptionTemplateDto[]> onSuccess,
        System.Action<string> onError)
    {
        string url = $"{baseURL}/upgrade/options";
        using (var www = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<OptionTemplatesResponse>(www.downloadHandler.text);
                    onSuccess?.Invoke(wrapper?.options ?? new OptionTemplateDto[0]);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] GetOptionTemplates parse error: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] GetOptionTemplates failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  GENE UPGRADE
    // ──────────────────────────────────────────────────────────────

    /// <summary>Lấy config nâng cấp gene: GET /api/gene/config?elementType=X&tier=Y</summary>
    public void GetGeneConfig(
        string elementType, int tier,
        System.Action<GeneConfigDto> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetGeneConfigCoroutine(elementType, tier, onSuccess, onError));
    }

    private IEnumerator GetGeneConfigCoroutine(
        string elementType, int tier,
        System.Action<GeneConfigDto> onSuccess,
        System.Action<string> onError)
    {
        string url = $"{baseURL}/gene/config?elementType={UnityEngine.Networking.UnityWebRequest.EscapeURL(elementType)}&tier={tier}";
        using (var www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try { onSuccess?.Invoke(JsonUtility.FromJson<GeneConfigDto>(www.downloadHandler.text)); }
                catch (System.Exception ex) { onError?.Invoke(ex.Message); }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] GetGeneConfig failed: {err}");
                onError?.Invoke(err);
            }
        }
    }

    /// <summary>Nâng cấp gene: POST /api/gene/upgrade</summary>
    public void UpgradeGene(
        GeneUpgradeRequest request,
        System.Action<GeneUpgradeResponse> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(UpgradeGeneCoroutine(request, onSuccess, onError));
    }

    private IEnumerator UpgradeGeneCoroutine(
        GeneUpgradeRequest request,
        System.Action<GeneUpgradeResponse> onSuccess,
        System.Action<string> onError)
    {
        string url  = $"{baseURL}/gene/upgrade";
        byte[] body = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        using (var www = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            www.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(body);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(jwtToken))
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try { onSuccess?.Invoke(JsonUtility.FromJson<GeneUpgradeResponse>(www.downloadHandler.text)); }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] UpgradeGene parse error: {ex.Message}");
                    onError?.Invoke(ex.Message);
                }
            }
            else
            {
                string err = www.downloadHandler?.text ?? www.error;
                Debug.LogError($"[APIClient] UpgradeGene failed: {err}");
                onError?.Invoke(err);
            }
        }
    }
}
