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
    public int user_id; // ID của user sở hữu player data này
    public int player_id;
    public int level;
    public int experience;
    public int exp_required_for_next_level;
    public int gold;
    public int map_id;
    public float position_x; // Vị trí X cuối cùng khi out game
    public float position_y; // Vị trí Y cuối cùng khi out game
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

public class APIClient : MonoBehaviour
{
    public static APIClient Instance { get; private set; }

    [Header("API Config")]
    public string baseURL = "http://localhost:5000/api"; // Thay đổi theo server của bạn

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
    
    /// <summary>
    /// Parse user_id từ JWT token (base64 decode payload)
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
            
            // Decode payload (phần thứ 2)
            string payload = parts[1];
            
            // Thêm padding nếu cần
            int padding = 4 - (payload.Length % 4);
            if (padding != 4)
            {
                payload += new string('=', padding);
            }
            
            // Base64 decode
            byte[] payloadBytes = System.Convert.FromBase64String(payload);
            string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            Debug.Log($"JWT Payload: {payloadJson}");
            
            // Parse JSON để lấy user_id
            // JWT payload có thể có: {"sub":"1","unique_name":"1","user_id":"1",...}
            if (payloadJson.Contains("\"user_id\""))
            {
                // Tìm "user_id":"X"
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
            
            // Thử parse "sub" nếu không có "user_id"
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
                
                // Parse JSON thủ công để đảm bảo lấy được user_id
                try
                {
                    // Thử parse bằng JsonUtility trước
                    response = JsonUtility.FromJson<LoginResponse>(responseText);
                    
                    // Nếu user_id = 0, parse thủ công từ JSON string
                    if (response.user_id == 0)
                    {
                        Debug.LogWarning("user_id = 0 from JsonUtility, trying manual parse...");
                        
                        // Parse thủ công: tìm "user_id":X trong JSON
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
                    
                    // Nếu vẫn = 0, thử parse từ JWT token
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
                    // Ưu tiên hiển thị message từ server (ví dụ: "Sai username hoặc password.")
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
                    // Ưu tiên hiển thị message từ server
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

    // Create Player (Chọn hệ ban đầu)
    public void CreatePlayer(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreatePlayerCoroutine(elementType, gender, characterName, onSuccess, onError));
    }

    private IEnumerator CreatePlayerCoroutine(string elementType, string gender, string characterName, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        // Escape JSON string để tránh lỗi với ký tự đặc biệt
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
    /// Update position của player lên server
    /// </summary>
    public void UpdatePlayerPosition(int playerId, int mapId, float positionX, float positionY, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(UpdatePlayerPositionCoroutine(playerId, mapId, positionX, positionY, onSuccess, onError));
    }

    private System.Collections.IEnumerator UpdatePlayerPositionCoroutine(int playerId, int mapId, float positionX, float positionY, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/position";
        
        // Tạo JSON string thủ công vì JsonUtility không hỗ trợ anonymous objects
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
    /// Update player data (batch update) lên server
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
    /// Thêm items vào inventory của player
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
    /// Lấy tất cả item templates từ server
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
        Debug.Log($"[APIClient] 🌐 Sending GET request to: {url}");
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            // Không cần Authorization vì endpoint là AllowAnonymous
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[APIClient] ✅ Item templates response received - Length: {responseText.Length} chars");
                Debug.Log($"[APIClient] 📄 Response preview: {responseText.Substring(0, Mathf.Min(200, responseText.Length))}...");
                
                try
                {
                    // Parse JSON response
                    ItemTemplatesResponse response = JsonUtility.FromJson<ItemTemplatesResponse>(responseText);
                    
                    if (response != null && response.item_templates != null)
                    {
                        Debug.Log($"[APIClient] ✅ Parsed {response.item_templates.Length} item templates successfully");
                        onSuccess?.Invoke(response.item_templates);
                    }
                    else
                    {
                        Debug.LogError("[APIClient] ❌ Failed to parse item templates response - response or item_templates is null");
                        onError?.Invoke("Failed to parse response");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] ❌ Error parsing item templates: {ex.Message}");
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
                Debug.LogError($"[APIClient] ❌ Failed to load item templates: {errorMessage}");
                Debug.LogError($"[APIClient] Response code: {www.responseCode}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Fetch inventory từ DB cho player (dùng để refresh UI)
    /// </summary>
    public void GetPlayerInventory(int playerId, System.Action<InventoryItem[]> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetPlayerInventoryCoroutine(playerId, onSuccess, onError));
    }

    private System.Collections.IEnumerator GetPlayerInventoryCoroutine(int playerId, System.Action<InventoryItem[]> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/data";
        Debug.Log($"[APIClient] 🔄 Fetching inventory from DB for player {playerId}...");
        
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
                        Debug.Log($"[APIClient] ✅ Inventory fetched successfully: {response.inventory?.Length ?? 0} items");
                        onSuccess?.Invoke(response.inventory ?? new InventoryItem[0]);
                    }
                    else
                    {
                        Debug.LogError("[APIClient] ❌ Failed to parse player data");
                        onError?.Invoke("Failed to parse response");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] ❌ Error parsing inventory: {ex.Message}");
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
                Debug.LogError($"[APIClient] ❌ Failed to fetch inventory: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    // ==================== EQUIPMENT API ====================

    /// <summary>
    /// Trang bị item từ inventory
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

        Debug.Log($"[APIClient] 🎮 Equip item: playerId={playerId}, slotIndex={inventorySlotIndex}");

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
                Debug.Log($"[APIClient] ✅ Equip thành công: {responseText}");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] ❌ Equip thất bại: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Tháo trang bị
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

        Debug.Log($"[APIClient] 🔧 Unequip: playerId={playerId}, slot={equipmentSlot}");

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
                Debug.Log($"[APIClient] ✅ Unequip thành công: {responseText}");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage = www.downloadHandler.text;
                }
                Debug.LogError($"[APIClient] ❌ Unequip thất bại: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Lấy thông tin trang bị của player
    /// </summary>
    public void GetPlayerEquipment(int playerId, System.Action<PlayerEquipmentDto> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetPlayerEquipmentCoroutine(playerId, onSuccess, onError));
    }

    private System.Collections.IEnumerator GetPlayerEquipmentCoroutine(int playerId, System.Action<PlayerEquipmentDto> onSuccess, System.Action<string> onError)
    {
        string url = $"{baseURL}/player/{playerId}/equipment";
        Debug.Log($"[APIClient] 🔄 Fetching equipment for player {playerId}...");

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
                Debug.Log($"[APIClient] ✅ Equipment response: {responseText}");

                try
                {
                    // Response format: { "player_id": 1, "equipment": { "weapon": {...}, ... } }
                    // Parse equipment từ wrapper
                    var wrapper = JsonUtility.FromJson<EquipmentResponseWrapper>(responseText);
                    if (wrapper != null && wrapper.equipment != null)
                    {
                        onSuccess?.Invoke(wrapper.equipment);
                    }
                    else
                    {
                        // Try parsing trực tiếp
                        var equipment = JsonUtility.FromJson<PlayerEquipmentDto>(responseText);
                        onSuccess?.Invoke(equipment ?? new PlayerEquipmentDto());
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[APIClient] ❌ Error parsing equipment: {ex.Message}");
                    // Trả về equipment trống thay vì lỗi
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
                Debug.LogError($"[APIClient] ❌ Failed to fetch equipment: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Wrapper để parse response từ GET /api/player/{id}/equipment
    /// </summary>
    [System.Serializable]
    private class EquipmentResponseWrapper
    {
        public int player_id;
        public PlayerEquipmentDto equipment;
    }
}
