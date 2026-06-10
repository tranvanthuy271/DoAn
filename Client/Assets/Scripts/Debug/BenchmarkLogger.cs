/// <summary>
/// BenchmarkLogger – ghi FPS và RTT ra file CSV để dùng trong báo cáo.
///
/// Cách dùng:
///   1. Gắn script này lên một GameObject rỗng trong Scene chính (vd: "BenchmarkLogger").
///   2. Vào Inspector: bật enableLogging, chỉnh sampleIntervalSeconds (mặc định 0.5s).
///   3. Chạy game, script tự ghi vào Application.persistentDataPath/benchmark_YYYYMMDD_HHMMSS.csv
///   4. Tìm file tại:
///      Windows: %APPDATA%\..\LocalLow\<CompanyName>\<ProductName>\
///      Android:  /data/data/<package>/files/
///      macOS:    ~/Library/Application Support/<CompanyName>/<ProductName>/
///
/// Cột CSV: timestamp_s, fps, rtt_ms, connected_clients, is_host, is_server
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Netcode;

public class BenchmarkLogger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Bật/tắt ghi log – tắt khi build release")]
    public bool enableLogging = true;

    [Tooltip("Khoảng cách giữa hai mẫu (giây)")]
    [Range(0.1f, 5f)]
    public float sampleIntervalSeconds = 0.5f;

    [Tooltip("Số mẫu tối đa rồi tự flush ra disk (0 = flush khi dừng)")]
    public int autoFlushEvery = 100;

    // ─── Private state ────────────────────────────────────────────────────────

    private string _csvPath;
    private StreamWriter _writer;

    // FPS tracking
    private int   _frameCount;
    private float _fpsAccum;
    private float _currentFps;

    // RTT tracking – Netcode for GameObjects
    // NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId)
    // hoặc dùng NetworkTime.ServerTime - NetworkTime.LocalTime * 2 (estimate)
    private float _lastRttMs;

    private bool  _logging;
    private int   _sampleCount;

    // ─── Unity callbacks ───────────────────────────────────────────────────────

    private void Start()
    {
        if (!enableLogging) return;
        StartLogging();
    }

    private void Update()
    {
        if (!enableLogging || !_logging) return;

        // Tích lũy FPS mỗi frame
        _frameCount++;
        _fpsAccum += Time.unscaledDeltaTime;
    }

    private void OnApplicationQuit()
    {
        StopLogging();
    }

    private void OnDestroy()
    {
        StopLogging();
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    public void StartLogging()
    {
        if (_logging) return;
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _csvPath = Path.Combine(Application.persistentDataPath, $"benchmark_{timestamp}.csv");

        _writer = new StreamWriter(_csvPath, false, Encoding.UTF8);
        _writer.WriteLine("timestamp_s,fps,rtt_ms,connected_clients,is_host,is_server");
        _writer.Flush();

        _logging = true;
        _frameCount = 0;
        _fpsAccum   = 0f;
        _sampleCount = 0;

        StartCoroutine(SampleLoop());
        Debug.Log($"[BenchmarkLogger] Bắt đầu ghi vào: {_csvPath}");
    }

    public void StopLogging()
    {
        if (!_logging) return;
        _logging = false;
        StopAllCoroutines();

        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }

        Debug.Log($"[BenchmarkLogger] Đã ghi {_sampleCount} mẫu vào: {_csvPath}");
        Debug.Log($"[BenchmarkLogger] Tóm tắt: xem file CSV tại {_csvPath}");
    }

    // ─── Sample coroutine ──────────────────────────────────────────────────────

    private IEnumerator SampleLoop()
    {
        while (_logging)
        {
            yield return new WaitForSecondsRealtime(sampleIntervalSeconds);
            if (!_logging) break;

            CollectSample();
        }
    }

    private void CollectSample()
    {
        // FPS: trung bình trong khoảng interval
        float fps = _fpsAccum > 0f ? _frameCount / _fpsAccum : 0f;
        _currentFps = fps;
        _frameCount = 0;
        _fpsAccum   = 0f;

        // RTT
        float rttMs = MeasureRtt();

        // Netcode info
        int   connectedClients = 0;
        bool  isHost   = false;
        bool  isServer = false;

        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            isHost   = nm.IsHost;
            isServer = nm.IsServer;
            if (nm.IsServer || nm.IsHost)
                connectedClients = nm.ConnectedClients.Count;
        }

        // Ghi CSV
        string line = string.Format("{0:F2},{1:F1},{2:F1},{3},{4},{5}",
            Time.realtimeSinceStartup,
            fps,
            rttMs,
            connectedClients,
            isHost  ? 1 : 0,
            isServer ? 1 : 0);

        _writer?.WriteLine(line);
        _sampleCount++;

        if (autoFlushEvery > 0 && _sampleCount % autoFlushEvery == 0)
            _writer?.Flush();
    }

    // ─── RTT measurement ───────────────────────────────────────────────────────

    private float MeasureRtt()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsConnectedClient) return 0f;

        try
        {
            // Netcode for GameObjects: NetworkTime sử dụng server time để tính offset
            // RTT ≈ (localTime - serverTime) * 2  khi không có clock skew
            // Cách chính xác hơn: dùng transport GetCurrentRtt nếu transport hỗ trợ
            var transport = nm.NetworkConfig?.NetworkTransport;
            if (transport != null && nm.IsClient && !nm.IsHost)
            {
                // Unity Transport (UTP) hỗ trợ GetCurrentRtt
                ulong serverId = NetworkManager.ServerClientId;
                ulong rttRaw   = transport.GetCurrentRtt(serverId);
                if (rttRaw > 0)
                {
                    _lastRttMs = rttRaw; // đơn vị ms
                    return _lastRttMs;
                }
            }

            // Fallback: ước tính RTT từ hiệu số ServerTime – LocalTime
            if (nm.NetworkTimeSystem != null)
            {
                double serverT = nm.NetworkTimeSystem.ServerTime;
                double localT  = nm.NetworkTimeSystem.LocalTime;
                float estimated = (float)(Math.Abs(serverT - localT) * 2.0 * 1000.0);
                _lastRttMs = estimated;
                return estimated;
            }
        }
        catch
        {
            // Ignore – không phải mọi transport đều expose RTT
        }

        return _lastRttMs; // giữ giá trị cuối nếu không đo được
    }

    // ─── Editor helper ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Benchmark/Open Log Folder")]
    static void OpenLogFolder()
    {
        UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
#endif
}
