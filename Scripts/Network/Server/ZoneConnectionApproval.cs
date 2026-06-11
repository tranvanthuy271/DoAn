using System.Text;
using Unity.Netcode;
using UnityEngine;

// Xử lý Connection Approval của NGO — validate JWT token từ client.
// Flow:
// Client kết nối → NGO gọi callback này → validate JWT → approve / deny.
// Client phải gửi JWT trong NetworkConfig.ConnectionData (byte[] UTF-8).
// Gắn vào: cùng GameObject với NetworkManager trong ServerScene.
// Gọi Initialize() trước khi NetworkManager.StartServer().
[DisallowMultipleComponent]
public class ZoneConnectionApproval : MonoBehaviour
{
    private ZoneServerConfig _config;
    private string _jwtSecret;

    // Khởi tạo với config — phải gọi trước StartServer().
    public void Initialize(ZoneServerConfig config)
    {
        _config = config;

        try
        {
            _jwtSecret = config.GetJwtSecret();
        }
        catch (System.Exception ex)
        {
            { /* Lỗi: {ex.Message} */ }
            enabled = false;
            return;
        }

        // Đăng ký callback
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = HandleApprovalRequest;
            { /* ✓ Connection approval callback đã đăng ký */ }
        }
        else
        {
            { /* Lỗi: NetworkManager.Singleton là null */ }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
    }

    // Approval Logic

    private void HandleApprovalRequest(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        ulong clientId = request.ClientNetworkId;

        // 1 — Decode connection data (JWT hoặc JSON chứa JWT)
        string rawToken = null;
        if (request.Payload != null && request.Payload.Length > 0)
        {
            rawToken = Encoding.UTF8.GetString(request.Payload);
        }

        if (string.IsNullOrEmpty(rawToken))
        {
            { /* Cảnh báo: Client {clientId}: Payload rỗng → Deny */ }
            response.Approved = false;
            response.Reason = "Token rỗng.";
            return;
        }

        // 2 — Hỗ trợ payload dạng JSON: { "token": "...", "entryPointId": 0 }
        //     Hoặc payload đơn giản là raw JWT string
        string jwt = ExtractTokenFromPayload(rawToken);

        // 3 — Validate JWT
        var result = JwtValidator.Validate(jwt, _jwtSecret);
        if (!result.IsValid)
        {
            { /* Cảnh báo: Client {clientId}: JWT invalid  {result.ErrorMessage} → Deny */ }
            response.Approved = false;
            response.Reason = "Token không hợp lệ.";
            return;
        }

        // 4 — Kiểm tra capacity
        if (_config != null && _config.maxPlayers > 0)
        {
            int currentCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (currentCount >= _config.maxPlayers)
            {
                { /* Cảnh báo: Client {clientId}: Zone đầy */ }
                response.Approved = false;
                response.Reason = "Zone đầy người chơi.";
                return;
            }
        }

        // 5 — Approve
        { /* ✓ Client {clientId} approved */ }

        // Gắn user info vào session — ZonePlayerSessionManager sẽ đọc khi client connect
        ZonePlayerSessionManager.Instance?.StoreApprovedUser(clientId, result.UserId, result.Username, rawToken);

        response.Approved = true;
        response.CreatePlayerObject = false; // Player object sẽ được spawn sau khi load xong dữ liệu
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
    }

    // Hỗ trợ cả raw JWT và JSON payload: {"token":"...","entryPointId":0}
    private static string ExtractTokenFromPayload(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("{"))
        {
            // Parse "token" field từ JSON
            string tokenClaim = JwtValidator.ExtractClaimPublic(raw, "token");
            if (!string.IsNullOrEmpty(tokenClaim))
                return tokenClaim;
        }
        return raw; // Coi như raw JWT
    }
}
