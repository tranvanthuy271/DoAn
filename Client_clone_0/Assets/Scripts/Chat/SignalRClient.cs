using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Client SignalR JSON Hub Protocol cho Unity.
// Sử dụng System.Net.WebSockets.ClientWebSocket (không cần thư viện ngoài).
// JWT được truyền qua query param ?access_token= (chuẩn ASP.NET Core SignalR).
public class SignalRClient : MonoBehaviour
{
    // Hằng số dùng chung trong file.
    private const char   RECORD_SEP = '\x1e';
    private const string HANDSHAKE  = "{\"protocol\":\"json\",\"version\":1}\x1e";

    // State
    private ClientWebSocket            _socket;
    private CancellationTokenSource    _cts;
    private string                     _hubUrl;
    private string                     _token;

    // true khi WebSocket đang mở VÀ SignalR handshake đã hoàn tất thành công.
    private bool _handshakeDone = false;
    public bool IsConnected => _handshakeDone && _socket?.State == WebSocketState.Open;

    // Events (dispatch về main thread)
    public event Action          OnConnected;
    public event Action<string>  OnDisconnected;   // reason
    public event Action<string>  OnError;

    // Handlers: key = target method name (lowercase)
    private readonly Dictionary<string, Action<string>> _handlers
        = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);

    // Main-thread dispatch queue
    private readonly ConcurrentQueue<Action> _mainQueue = new ConcurrentQueue<Action>();

    // MonoBehaviour

    private void Update()
    {
        while (_mainQueue.TryDequeue(out var a)) a.Invoke();
    }

    private void OnDestroy()
    {
        _handshakeDone = false;
        _cts?.Cancel();
        _socket?.Dispose();
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Đăng ký callback cho hub method được gọi từ server.
    public void On(string target, Action<string> handler)
        => _handlers[target] = handler;

    // Kết nối đến SignalR Hub. Gọi từ coroutine / Start.
    public void Connect(string hubUrl, string jwtToken)
    {
        _hubUrl = hubUrl;
        _token  = jwtToken;
        { /* SignalR Connect -> {_hubUrl} */ }
        StartCoroutine(ConnectRoutine());
    }

    // Ngắt kết nối chủ động.
    public void Disconnect()
    {
        _cts?.Cancel();
        if (_socket?.State == WebSocketState.Open)
            _ = _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
    }

    // Invoke hub method (fire-and-forget, không cần kết quả).
    public void Invoke(string method, params string[] args)
    {
        if (!IsConnected) return;
        var payload = BuildInvoke(method, args);
        _ = SendRawTask(payload);
    }

    // Connection Coroutine

    private IEnumerator ConnectRoutine()
    {
        // 1. Negotiate
        string connToken = null;
        yield return StartCoroutine(NegotiateRoutine(result => connToken = result));

        if (string.IsNullOrEmpty(connToken))
        {
            _mainQueue.Enqueue(() => OnError?.Invoke("[Chat] Negotiate thất bại"));
            yield break;
        }

        // 2. Mở WebSocket
        _cts    = new CancellationTokenSource();
        _socket = new ClientWebSocket();

        var wsUrl = _hubUrl
            .Replace("https://", "wss://")
            .Replace("http://",  "ws://")
            + $"?id={Uri.EscapeDataString(connToken)}&access_token={Uri.EscapeDataString(_token)}";

        var connectTask = _socket.ConnectAsync(new Uri(wsUrl), _cts.Token);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (!connectTask.IsCompletedSuccessfully)
        {
            var err = connectTask.Exception?.InnerException?.Message ?? "WS connect failed";
            _mainQueue.Enqueue(() => OnError?.Invoke($"[Chat] {err}"));
            yield break;
        }

        // 3. Handshake JSON protocol
        var handshakeTask = SendRawTask(HANDSHAKE);
        yield return new WaitUntil(() => handshakeTask.IsCompleted);

        // Đọc handshake response ({} + record sep)
        var buf      = new byte[512];
        var readTask = _socket.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
        yield return new WaitUntil(() => readTask.IsCompleted);

        if (readTask.IsFaulted)
        {
            var errMsg = readTask.Exception?.InnerException?.Message ?? "Lỗi đọc handshake response";
            _mainQueue.Enqueue(() => OnError?.Invoke($"[Chat] Handshake read lỗi: {errMsg}"));
            yield break;
        }

        // Kiểm tra server không đóng kết nối ngay
        if (readTask.Result.MessageType == WebSocketMessageType.Close)
        {
            _mainQueue.Enqueue(() => OnError?.Invoke("[Chat] Server đóng kết nối trong handshake (JWT không hợp lệ?)"));
            yield break;
        }

        // Kiểm tra handshake response có lỗi không
        var handshakeResponse = Encoding.UTF8
            .GetString(buf, 0, readTask.Result.Count)
            .Replace("\x1e", "").Trim();

        if (handshakeResponse.Contains("\"error\""))
        {
            var errMsg = ExtractString(handshakeResponse, "error") ?? handshakeResponse;
            _mainQueue.Enqueue(() => OnError?.Invoke($"[Chat] Handshake từ chối: {errMsg}"));
            yield break;
        }

        // 4. Kết nối thành công
        _handshakeDone = true;
        _mainQueue.Enqueue(() => OnConnected?.Invoke());

        // Khởi chạy receive + ping loop trên background thread
        _ = ReceiveLoopAsync();
        _ = PingLoopAsync();
    }

    // Negotiate

    private IEnumerator NegotiateRoutine(Action<string> onResult)
    {
        var url = $"{_hubUrl}/negotiate?negotiateVersion=1";

        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", $"Bearer {_token}");
        req.SetRequestHeader("Content-Type",  "application/json");
        req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            { /* Lỗi: Negotiate HTTP {req.responseCode} lỗi: {req.error}\n */ }
            onResult(null);
            yield break;
        }

        var json = req.downloadHandler.text;
        if (string.IsNullOrEmpty(json))
        {
            { /* Lỗi: Negotiate trả về body rỗng */ }
            onResult(null);
            yield break;
        }

        // Server trả về lỗi dạng JSON (ít phổ biến nhưng có thể xảy ra)
        if (json.Contains("\"error\"") && !json.Contains("\"availableTransports\""))
        {
            { /* Lỗi: Negotiate lỗi từ server: {json} */ }
            onResult(null);
            yield break;
        }

        // Ưu tiên connectionToken (negotiateVersion=1), fallback connectionId
        var token = ExtractString(json, "connectionToken")
                 ?? ExtractString(json, "connectionId");

        if (string.IsNullOrEmpty(token))
            { /* Lỗi: Negotiate không tìm thấy connectionToken/connectionId. Response: {json} */ }

        onResult(token);
    }

    // Receive Loop (background task)

    private async Task ReceiveLoopAsync()
    {
        var buf      = new byte[32768];
        var textBuf  = new StringBuilder();

        try
        {
            while (_socket?.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _handshakeDone = false;
                    _mainQueue.Enqueue(() => OnDisconnected?.Invoke("Server đóng kết nối"));
                    return;
                }

                textBuf.Append(Encoding.UTF8.GetString(buf, 0, result.Count));

                if (!result.EndOfMessage) continue;

                var text = textBuf.ToString();
                textBuf.Clear();

                // Mỗi message kết thúc bằng RECORD_SEP
                foreach (var part in text.Split(RECORD_SEP))
                {
                    var trimmed = part.Trim();
                    if (trimmed.Length > 0)
                    {
                        var captured = trimmed;
                        _mainQueue.Enqueue(() => DispatchMessage(captured));
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _handshakeDone = false;
            _mainQueue.Enqueue(() => OnDisconnected?.Invoke(ex.Message));
        }
    }

    // Ping Loop (background task)

    private async Task PingLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(20_000, _cts.Token);
                if (IsConnected)
                    await SendRawTask("{\"type\":6}\x1e");
            }
        }
        catch (OperationCanceledException) { }
    }

    // Message Dispatch

    private void DispatchMessage(string json)
    {
        int type = ExtractInt(json, "type");
        switch (type)
        {
            case 1: // Invocation từ server
            {
                var target = ExtractString(json, "target");
                if (string.IsNullOrEmpty(target)) return;
                var argJson = ExtractFirstArgument(json);
                { /* SignalR Invocation: target='{target}' payload={argJson} */ }
                if (_handlers.TryGetValue(target, out var h))
                    h?.Invoke(argJson);
                else
                    { /* Cảnh báo: Chưa đăng ký handler cho target '{target}' */ }
                break;
            }
            case 7: // Close
            {
                var error = ExtractString(json, "error");
                _handshakeDone = false;
                OnDisconnected?.Invoke(error ?? "Hub đóng");
                break;
            }
            // type 6 = ping, type 3 = completion – bỏ qua
        }
    }

    // Send

    private async Task SendRawTask(string text)
    {
        if (_socket?.State != WebSocketState.Open) return;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await _socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: _cts?.Token ?? CancellationToken.None);
        }
        catch (Exception ex) { { /* Cảnh báo: Send error: {ex.Message} */ } }
    }

    // JSON Helpers

    private static int ExtractInt(string json, string key)
    {
        int i = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (i < 0) return -1;
        int c = json.IndexOf(':', i) + 1;
        while (c < json.Length && json[c] == ' ') c++;
        int e = c;
        while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '-')) e++;
        return int.TryParse(json.Substring(c, e - c), out int v) ? v : -1;
    }

    private static string ExtractString(string json, string key)
    {
        int i = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (i < 0) return null;
        int c = json.IndexOf(':', i) + 1;
        while (c < json.Length && json[c] == ' ') c++;
        if (c >= json.Length || json[c] != '"') return null;
        c++;
        var sb = new StringBuilder();
        while (c < json.Length && json[c] != '"')
        {
            if (json[c] == '\\' && c + 1 < json.Length) { sb.Append(json[++c]); c++; }
            else sb.Append(json[c++]);
        }
        return sb.ToString();
    }

    // Trích xuất phần tử đầu tiên của mảng "arguments".
    private static string ExtractFirstArgument(string json)
    {
        int i = json.IndexOf("\"arguments\"", StringComparison.Ordinal);
        if (i < 0) return "{}";
        int bracket = json.IndexOf('[', i);
        if (bracket < 0) return "{}";
        int start = bracket + 1;
        while (start < json.Length && json[start] == ' ') start++;
        if (start >= json.Length || json[start] == ']') return "{}";

        if (json[start] == '{')
        {
            int depth = 0, j = start;
            while (j < json.Length)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}') { depth--; if (depth == 0) return json.Substring(start, j - start + 1); }
                j++;
            }
        }
        // Primitive string argument
        if (json[start] == '"')
        {
            int end = json.IndexOf('"', start + 1);
            return end > start ? json.Substring(start, end - start + 1) : "{}";
        }
        return "{}";
    }

    // Invocation Builder

    private static string BuildInvoke(string method, string[] args)
    {
        var sb = new StringBuilder();
        sb.Append("{\"type\":1,\"target\":\"");
        sb.Append(Esc(method));
        sb.Append("\",\"arguments\":[");
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Esc(args[i])).Append('"');
        }
        sb.Append("]}");
        sb.Append(RECORD_SEP);
        return sb.ToString();
    }

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"")
         .Replace("\n", "\\n").Replace("\r", "\\r");
}
