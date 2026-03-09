using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// NetworkItemTemplateSync - Đồng bộ item templates giữa Host và Clients
/// 
/// Flow:
/// 1. Host load item templates từ API (qua ItemTemplateManager)
/// 2. Host serialize thành JSON
/// 3. Host gửi ClientRpc chứa JSON cho tất cả Clients
/// 4. Clients nhận và deserialize, lưu vào ItemTemplateManager
/// 
/// Setup:
/// - Gắn script này vào GameObject có NetworkObject
/// - Hoặc gắn vào cùng GameObject với NetworkManager
/// - Script tự động sync khi Host start
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkItemTemplateSync : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("Có tự động sync khi Host start không")]
    [SerializeField] private bool autoSyncOnHostStart = true;

    [Tooltip("Có bật debug log không")]
    [SerializeField] private bool enableDebugLog = true;

    private bool hasSynced = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && autoSyncOnHostStart)
        {
            // Host: Đợi ItemTemplateManager load xong rồi sync
            StartCoroutine(WaitAndSyncItemTemplates());
        }
    }

    /// <summary>
    /// Đợi ItemTemplateManager load xong rồi sync
    /// </summary>
    private System.Collections.IEnumerator WaitAndSyncItemTemplates()
    {
        if (enableDebugLog)
        {
            Debug.Log("[NetworkItemTemplateSync] Host đang đợi ItemTemplateManager load...");
        }

        // Đợi ItemTemplateManager load xong
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!ItemTemplateManager.Instance.IsLoaded() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (!ItemTemplateManager.Instance.IsLoaded())
        {
            Debug.LogError("[NetworkItemTemplateSync] ❌ Timeout khi đợi ItemTemplateManager load!");
            yield break;
        }

        if (enableDebugLog)
        {
            Debug.Log("[NetworkItemTemplateSync] ✅ ItemTemplateManager đã load xong, bắt đầu sync...");
        }

        // Sync item templates
        SyncItemTemplates();
    }

    /// <summary>
    /// Host gọi để sync item templates cho tất cả Clients
    /// </summary>
    public void SyncItemTemplates()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkItemTemplateSync] Chỉ Host mới có thể sync item templates!");
            return;
        }

        if (hasSynced)
        {
            if (enableDebugLog)
            {
                Debug.Log("[NetworkItemTemplateSync] Item templates đã được sync trước đó.");
            }
            return;
        }

        if (ItemTemplateManager.Instance == null || !ItemTemplateManager.Instance.IsLoaded())
        {
            Debug.LogError("[NetworkItemTemplateSync] ItemTemplateManager chưa load xong!");
            return;
        }

        // Lấy tất cả item templates
        var templates = ItemTemplateManager.Instance.GetAllItemTemplates();
        
        if (templates == null || templates.Length == 0)
        {
            Debug.LogWarning("[NetworkItemTemplateSync] Không có item templates để sync!");
            return;
        }

        // Serialize thành JSON
        string json = SerializeItemTemplates(templates);
        
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[NetworkItemTemplateSync] Lỗi khi serialize item templates!");
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log($"[NetworkItemTemplateSync] Host đang sync {templates.Length} item templates cho clients...");
        }

        // Chia nhỏ JSON nếu quá lớn (Unity Netcode giới hạn message size)
        // Mỗi message tối đa ~1KB, nếu JSON lớn hơn thì chia nhỏ
        const int maxChunkSize = 900; // bytes (để buffer cho metadata)
        
        if (json.Length <= maxChunkSize)
        {
            // Gửi 1 lần
            SyncItemTemplatesClientRpc(json, 0, 1);
        }
        else
        {
            // Chia nhỏ và gửi nhiều lần
            int chunkCount = (json.Length + maxChunkSize - 1) / maxChunkSize;
            
            for (int i = 0; i < chunkCount; i++)
            {
                int startIndex = i * maxChunkSize;
                int length = Mathf.Min(maxChunkSize, json.Length - startIndex);
                string chunk = json.Substring(startIndex, length);
                
                SyncItemTemplatesClientRpc(chunk, i, chunkCount);
            }
        }

        hasSynced = true;
    }

    /// <summary>
    /// Serialize item templates thành JSON
    /// </summary>
    private string SerializeItemTemplates(ItemTemplateDto[] templates)
    {
        try
        {
            // Tạo wrapper object để JsonUtility có thể serialize array
            var wrapper = new ItemTemplatesWrapper
            {
                templates = templates
            };
            
            return JsonUtility.ToJson(wrapper);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkItemTemplateSync] Lỗi khi serialize: {ex.Message}");
            return null;
        }
    }

    // Buffer để lưu các chunks nhận được
    private Dictionary<int, string> receivedChunks = new Dictionary<int, string>();
    private int expectedChunkCount = 0;

    /// <summary>
    /// ClientRpc: Gửi item templates (hoặc chunk của nó) từ Host xuống Clients
    /// </summary>
    [ClientRpc]
    private void SyncItemTemplatesClientRpc(string jsonChunk, int chunkIndex, int totalChunks)
    {
        // Host không cần nhận lại (đã có rồi)
        if (IsServer)
        {
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log($"[NetworkItemTemplateSync] Client nhận chunk {chunkIndex + 1}/{totalChunks} (size={jsonChunk.Length})");
        }

        // Lưu chunk vào buffer
        receivedChunks[chunkIndex] = jsonChunk;
        expectedChunkCount = totalChunks;

        // Kiểm tra đã nhận đủ chunks chưa
        if (receivedChunks.Count == totalChunks)
        {
            // Ghép các chunks lại
            string fullJson = "";
            for (int i = 0; i < totalChunks; i++)
            {
                if (receivedChunks.ContainsKey(i))
                {
                    fullJson += receivedChunks[i];
                }
                else
                {
                    Debug.LogError($"[NetworkItemTemplateSync] Thiếu chunk {i}!");
                    return;
                }
            }

            // Deserialize và lưu vào ItemTemplateManager
            DeserializeAndApplyItemTemplates(fullJson);

            // Clear buffer
            receivedChunks.Clear();
            expectedChunkCount = 0;
        }
    }

    /// <summary>
    /// Deserialize JSON và apply vào ItemTemplateManager
    /// </summary>
    private void DeserializeAndApplyItemTemplates(string json)
    {
        try
        {
            // Deserialize từ JSON
            var wrapper = JsonUtility.FromJson<ItemTemplatesWrapper>(json);
            
            if (wrapper == null || wrapper.templates == null)
            {
                Debug.LogError("[NetworkItemTemplateSync] Lỗi khi deserialize item templates!");
                return;
            }

            if (enableDebugLog)
            {
                Debug.Log($"[NetworkItemTemplateSync] ✅ Client nhận {wrapper.templates.Length} item templates từ Host");
            }

            // Apply vào ItemTemplateManager (force update cache)
            if (ItemTemplateManager.Instance != null)
            {
                // Gọi private method OnItemTemplatesLoaded qua reflection
                var method = typeof(ItemTemplateManager).GetMethod("OnItemTemplatesLoaded", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(ItemTemplateManager.Instance, new object[] { wrapper.templates });
                }
                else
                {
                    Debug.LogWarning("[NetworkItemTemplateSync] Không tìm thấy method OnItemTemplatesLoaded, client cần load từ API.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkItemTemplateSync] Lỗi khi deserialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Wrapper class để JsonUtility có thể serialize array
    /// </summary>
    [System.Serializable]
    private class ItemTemplatesWrapper
    {
        public ItemTemplateDto[] templates;
    }
}
