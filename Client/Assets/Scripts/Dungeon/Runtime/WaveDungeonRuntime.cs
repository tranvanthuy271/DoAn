using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

// WaveDungeonRuntime — Quản lý logic wave (vòng) cho phó bản.
// MULTI-ZONE ISOLATION (mỗi người chơi trong zone riêng của họ):
// BeginEncounter(dungeonId, mapId, zoneId) tạo ZoneEncounterState độc lập cho mỗi zone.
// Các zone KHÔNG ảnh hưởng nhau — timer, quái, boss chạy song song.
// Người chơi khác nhau ở zone khác nhau: không nhìn thấy nhau, không chia sẻ quái.
// SESSION RECONNECT:
// WaveSessionManager.UpdateSessionStateByZone được gọi mỗi giây.
// Khi reconnect, ZoneTransitionController phục hồi người chơi về zone cũ.
public class WaveDungeonRuntime : BaseDungeonInstance
{
    [Header("Config (fallback nếu API không có)")]
    [SerializeField] private DungeonWaveConfig config;
    [SerializeField] private string apiBaseUrl = "";

    [Header("Wave UI (fallback — ưu tiên WaveHUD trên canvas riêng)")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;

    // NetworkVariables: cập nhật theo zone gần nhất để HUD fallback hoạt động.
    // Client nhận snapshot khi vào/reconnect/chuyển vòng và tự đếm ngược cục bộ.
    private readonly NetworkVariable<int> _currentRound     = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _remainingSeconds = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _maxRounds        = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Public accessors cho WaveHUD fallback
    public int CurrentRound     => _currentRound.Value;
    public int RemainingSeconds => _remainingSeconds.Value;
    public int MaxRounds        => _maxRounds.Value > 0 ? _maxRounds.Value : (config != null ? config.maxRounds : 1);

    // Per-zone encounter state
    // Mỗi zone (ZoneRoom.ZoneId) có một ZoneEncounterState độc lập.
    // BeginEncounter tạo entry mới; StopZoneEncounter dọn dẹp khi zone kết thúc.
    private readonly Dictionary<int, ZoneEncounterState> _activeZones = new();

    private sealed class ZoneEncounterState
    {
        public int    DungeonId;
        public int    MapId;
        public int    ZoneId;
        public string EncounterKey;     // "dungeonId:mapId:zoneId" — dùng để idempotent check

        public DungeonWaveConfig Config;

        // Wave runtime
        public int  CurrentRound;
        public int  MaxRounds;
        public int  RemainingSeconds;
        public bool Ended;
        public bool BossSpawned;

        // Enemies của zone này
        public readonly List<NetworkObject> AliveEnemies = new();
        public NetworkObject                BossObject;

        // Coroutine references để StopCoroutine đúng zone
        public Coroutine TimerCoroutine;
        public Coroutine InitCoroutine;
    }

    private void Awake()
    {
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
            return;

        // Dedicated server dùng runtime này như coordinator nội bộ.
        // Client không load ServerScene, nên tuyệt đối không replicate in-scene object này xuống client.
        networkObject.SpawnWithObservers = false;
        MapSceneManager.ConfigureNetworkObjectForServerOnlyScene(networkObject);
    }

    private void Start()
    {
        // ServerScene chỉ load trên dedicated server (SpawnWithObservers=false →
        // client không bao giờ có instance này). Vì vậy KHÔNG deactivate GameObject
        // dựa vào IsServer — tại thời điểm Start() lần đầu, NetworkManager có thể
        // chưa kịp StartServer, khiến IsServer=false và GameObject bị tắt vĩnh viễn,
        // làm FindAnyObjectByType<WaveDungeonRuntime>() trả về null sau này.
        Debug.Log($"[WaveDungeonRuntime] Start() scene={gameObject.scene.name} active={gameObject.activeInHierarchy} IsServer={(NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)}");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        var dm = DungeonManager.Instance;
        if (dm == null) return;
        dm.OnWaveStateChanged  -= ClientOnWaveStateChanged;
        dm.OnDungeonEntered    -= ClientOnDungeonEntered;
        dm.OnDungeonExited     -= ClientOnDungeonExited;
    }

    private void ClientOnWaveStateChanged(int round, int maxRounds, int remaining)
    {
        gameObject.SetActive(round > 0);
        if (roundText != null)
            roundText.text = $"Vòng {round} / {maxRounds}";
        if (timerText != null)
        {
            int sec = Mathf.Max(0, remaining);
            timerText.text  = $"{sec / 60:00}:{sec % 60:00}";
            timerText.color = sec < 30 ? Color.red : Color.white;
        }
    }

    private void ClientOnDungeonEntered() => gameObject.SetActive(false); // ẩn, chờ wave state

    private void ClientOnDungeonExited()
    {
        gameObject.SetActive(false);
        if (roundText  != null) roundText.text  = "Vòng -/-";
        if (timerText  != null) timerText.text  = "00:00";
    }

    private void ClientRefreshDisplay()
    {
        var dm = DungeonManager.Instance;
        if (dm == null || !dm.IsInDungeon || dm.CurrentWaveRound <= 0)
        {
            gameObject.SetActive(false);
            return;
        }
        ClientOnWaveStateChanged(dm.CurrentWaveRound, dm.CurrentWaveMaxRounds, dm.CurrentWaveRemainingSeconds);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[WaveDungeonRuntime] OnNetworkSpawn scene={gameObject.scene.name} isServer={IsServer} isClient={IsClient}");

        if (IsClient)
            EnsureWaveHUD();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsServer) return;

        foreach (var kv in _activeZones)
            StopZoneEncounter(kv.Value);
        _activeZones.Clear();
        Debug.Log("[WaveDungeonRuntime] OnNetworkDespawn — tất cả zone encounter đã dừng.");
    }

    // Public API (gọi từ ZoneTransitionController)

    // Khởi động encounter cho zone chỉ định.
    // KHÔNG reset zone khác đang chạy — mỗi zone hoạt động hoàn toàn độc lập.
    public void BeginEncounter(int dungeonId, int mapId, int zoneId)
    {
        if (!IsServer || dungeonId <= 0 || mapId < 0)
        {
            Debug.LogWarning($"[WaveDungeonRuntime] BeginEncounter bị chặn: IsServer={IsServer} dungeonId={dungeonId} mapId={mapId}");
            return;
        }

        string encounterKey = $"{dungeonId}:{mapId}:{zoneId}";

        // Idempotent: nếu zone này đã chạy đúng encounter và chưa kết thúc → bỏ qua
        if (_activeZones.TryGetValue(zoneId, out var existing) &&
            string.Equals(existing.EncounterKey, encounterKey, StringComparison.Ordinal) &&
            !existing.Ended)
        {
            Debug.Log($"[WaveDungeonRuntime] BeginEncounter zone={zoneId} encounter '{encounterKey}' đã chạy → idempotent skip.");
            return;
        }

        // Nếu zone này có encounter cũ → dừng trước (không ảnh hưởng zone khác)
        if (_activeZones.TryGetValue(zoneId, out var oldState))
        {
            Debug.Log($"[WaveDungeonRuntime] BeginEncounter zone={zoneId} — dừng encounter cũ '{oldState.EncounterKey}'.");
            StopZoneEncounter(oldState);
        }

        var state = new ZoneEncounterState
        {
            DungeonId    = dungeonId,
            MapId        = mapId,
            ZoneId       = zoneId,
            EncounterKey = encounterKey,
        };
        _activeZones[zoneId] = state;

        Debug.Log($"[WaveDungeonRuntime] BeginEncounter zone={zoneId} dungeonId={dungeonId} mapId={mapId} " +
                  $"activeZones={_activeZones.Count} scene={gameObject.scene.name}");

        state.InitCoroutine = StartCoroutine(InitializeZoneConfigCoroutine(state));
    }

    // Dừng và dọn dẹp một zone encounter (KHÔNG ảnh hưởng zone khác).
    private void StopZoneEncounter(ZoneEncounterState state)
    {
        if (state == null) return;
        if (state.TimerCoroutine != null) { StopCoroutine(state.TimerCoroutine); state.TimerCoroutine = null; }
        if (state.InitCoroutine  != null) { StopCoroutine(state.InitCoroutine);  state.InitCoroutine  = null; }

        foreach (var enemy in state.AliveEnemies)
            DespawnNetworkObject(enemy);
        state.AliveEnemies.Clear();

        DespawnNetworkObject(state.BossObject);
        state.BossObject = null;
        state.Ended      = true;

        WaveSessionManager.Instance?.EndSessionsByZone(state.ZoneId);
        Debug.Log($"[WaveDungeonRuntime] StopZoneEncounter zone={state.ZoneId} key='{state.EncounterKey}'");
    }

    private static void DespawnNetworkObject(NetworkObject networkObject)
    {
        if (networkObject == null) return;
        if (networkObject.IsSpawned) networkObject.Despawn(true);
        else if (networkObject.gameObject != null) Destroy(networkObject.gameObject);
    }

    //  Wave HUD — tự tạo canvas nếu thiếu ref

    private void EnsureWaveHUD()
    {
        TryBindExistingWaveHUD();

        if (roundText == null && timerText != null)
            roundText = CreateSiblingLabel(timerText, "RoundText", new Vector2(0f, 60f), "Vòng -/-");
        else if (timerText == null && roundText != null)
            timerText = CreateSiblingLabel(roundText, "TimerText", new Vector2(0f, -60f), "00:00");

        if (roundText == null || timerText == null)
        {
            // Tìm canvas hiện có trong scene (ưu tiên dùng lại)
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("WaveDungeonHUD");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Panel nền nửa trong suốt ở góc trên-trái
            var panel = new GameObject("WaveInfoPanel");
            panel.transform.SetParent(canvas.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot     = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -16f);
            panelRect.sizeDelta = new Vector2(240f, 80f);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            if (roundText == null)
                roundText = CreateLabel(panel.transform, "RoundText",
                    new Vector2(8f, -8f), new Vector2(224f, 32f), 20, "Vòng -/-");

            if (timerText == null)
                timerText = CreateLabel(panel.transform, "TimerText",
                    new Vector2(8f, -44f), new Vector2(224f, 28f), 18, "00:00");
        }

        EnsureAuxiliaryDungeonTexts();
    }

    private void TryBindExistingWaveHUD()
    {
        if (roundText != null && timerText != null)
            return;

        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        if (labels == null || labels.Length == 0)
            return;

        Array.Sort(labels, (left, right) => GetAnchoredY(right).CompareTo(GetAnchoredY(left)));

        if (roundText == null)
        {
            roundText = TryResolveRoundLabel(labels);
            if (roundText == null && labels.Length > 0)
                roundText = labels[0];
        }

        if (timerText == null)
        {
            timerText = TryResolveTimerLabel(labels, roundText);
            if (timerText == null)
            {
                foreach (TMP_Text label in labels)
                {
                    if (label != null && label != roundText)
                    {
                        timerText = label;
                        break;
                    }
                }
            }
        }

        if (timerText == roundText)
            timerText = null;
    }

    private void EnsureAuxiliaryDungeonTexts()
    {
        if (statusText == roundText || statusText == timerText)
            statusText = null;

        if (countdownText == roundText || countdownText == timerText)
            countdownText = null;

        TMP_Text referenceLabel = timerText != null ? timerText : roundText;
        if (referenceLabel == null)
            return;

        if (statusText == null)
        {
            statusText = CreateSiblingLabel(referenceLabel, "DungeonStatusText", new Vector2(0f, -60f), string.Empty, 0.75f);
            statusText.gameObject.SetActive(false);
        }

        if (countdownText == null)
        {
            TMP_Text countdownReference = statusText != null ? statusText : referenceLabel;
            countdownText = CreateSiblingLabel(countdownReference, "DungeonCountdownText", new Vector2(0f, -60f), string.Empty, 0.75f);
            countdownText.gameObject.SetActive(false);
        }
    }

    private static TMP_Text CreateLabel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta, int fontSize, string defaultText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot     = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text     = defaultText;
        text.fontSize = fontSize;
        text.color    = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private static TMP_Text CreateSiblingLabel(TMP_Text reference, string name, Vector2 offset, string defaultText, float fontScale = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(reference.rectTransform.parent, false);

        RectTransform referenceRect = reference.rectTransform;
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = referenceRect.anchorMin;
        rect.anchorMax = referenceRect.anchorMax;
        rect.pivot = referenceRect.pivot;
        rect.anchoredPosition = referenceRect.anchoredPosition + offset;
        rect.sizeDelta = referenceRect.sizeDelta;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.font = reference.font;
        text.fontSharedMaterial = reference.fontSharedMaterial;
        text.fontSize = Mathf.Max(16f, reference.fontSize * fontScale);
        text.color = reference.color;
        text.alignment = reference.alignment;
        text.enableWordWrapping = reference.enableWordWrapping;
        text.overflowMode = reference.overflowMode;
        text.margin = reference.margin;
        text.raycastTarget = false;
        return text;
    }

    private static TMP_Text TryResolveRoundLabel(IEnumerable<TMP_Text> labels)
    {
        foreach (TMP_Text label in labels)
        {
            if (label == null)
                continue;

            string labelName = label.gameObject.name;
            string text = label.text ?? string.Empty;
            if (labelName.IndexOf("round", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("vòng", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("vong", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return label;
            }
        }

        return null;
    }

    private static TMP_Text TryResolveTimerLabel(IEnumerable<TMP_Text> labels, TMP_Text roundLabel)
    {
        foreach (TMP_Text label in labels)
        {
            if (label == null || label == roundLabel)
                continue;

            string labelName = label.gameObject.name;
            string text = (label.text ?? string.Empty).Trim();
            if (labelName.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0
                || labelName.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf(':') >= 0
                || text.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        return null;
    }

    private static float GetAnchoredY(TMP_Text label)
    {
        if (label == null)
            return float.MinValue;

        return label.rectTransform.anchoredPosition.y;
    }

    // Config initialisation (per-zone)

    private IEnumerator InitializeZoneConfigCoroutine(ZoneEncounterState state)
    {
        // Bản sao config riêng cho zone (không modify shared ScriptableObject)
        DungeonWaveConfig baseCfg = config != null ? Instantiate(config) : null;

        bool loadedFromApi = false;
        yield return StartCoroutine(LoadZoneConfigFromApiCoroutine(state, baseCfg, success => loadedFromApi = success));

        if (!loadedFromApi)
            Debug.LogWarning($"[WaveDungeonRuntime] zone={state.ZoneId} — Không tải được config từ API. Fallback sang inspector config. dungeonId={state.DungeonId}");

        if (state.Config == null && baseCfg != null)
            state.Config = baseCfg;

        if (state.Config == null)
        {
            Debug.LogError($"[WaveDungeonRuntime] zone={state.ZoneId} — Không có config khả dụng. Encounter bị huỷ. dungeonId={state.DungeonId}");
            BroadcastStatusToZone(state, "[Lỗi] Không tải được cấu hình phó bản sóng.");
            _activeZones.Remove(state.ZoneId);
            state.InitCoroutine = null;
            yield break;
        }

        state.MaxRounds = Mathf.Max(1, state.Config.maxRounds);
        _maxRounds.Value = state.MaxRounds; // NetworkVariable HUD fallback
        SyncWaveStateToZone(state, false);

        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} — Config sẵn sàng. dungeonId={state.DungeonId} maxRounds={state.MaxRounds} " +
                  $"enemySpawns={state.Config.enemySpawns?.Count ?? 0} bossId={state.Config.bossSpawn?.enemyId ?? -1}");

        StartRoundForZone(state, 1);
        state.InitCoroutine = null;
    }

    private IEnumerator LoadZoneConfigFromApiCoroutine(ZoneEncounterState state, DungeonWaveConfig fallbackConfig, Action<bool> onCompleted)
    {
        int dungeonId = state.DungeonId;
        if (dungeonId <= 0) { onCompleted?.Invoke(false); yield break; }

        string resolvedApiUrl = ServerAddressConfig.Instance.ResolveApiUrl(apiBaseUrl);
        string url = $"{resolvedApiUrl}/dungeon/wave/{dungeonId}/config";
        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} — Fetch config: {url}");

        using UnityWebRequest request = UnityWebRequest.Get(url);
        if (IsDedicatedWorldServer())
        {
            string apiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey();
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.SetRequestHeader("X-Zone-Api-Key", apiKey);
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[WaveDungeonRuntime] zone={state.ZoneId} — API lỗi: {request.error} code={request.responseCode} body={request.downloadHandler?.text}");
            onCompleted?.Invoke(false);
            yield break;
        }

        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} — Config JSON: {request.downloadHandler.text}");
        DungeonWaveRuntimeResponse response = JsonUtility.FromJson<DungeonWaveRuntimeResponse>(request.downloadHandler.text);
        if (response == null)
        {
            Debug.LogWarning($"[WaveDungeonRuntime] zone={state.ZoneId} — Parse JSON thất bại.");
            onCompleted?.Invoke(false);
            yield break;
        }

        ApplyRuntimeResponseToZone(state, response, dungeonId, fallbackConfig);
        onCompleted?.Invoke(true);
    }

    private void ApplyRuntimeResponseToZone(ZoneEncounterState state, DungeonWaveRuntimeResponse response, int dungeonId, DungeonWaveConfig fallback)
    {
        var cfg = fallback != null ? Instantiate(fallback) : ScriptableObject.CreateInstance<DungeonWaveConfig>();
        cfg.dungeonId = dungeonId;

        if (response.map_id > 0)
            state.MapId = response.map_id;

        cfg.roundTimeSeconds      = Mathf.Max(1f,  response.wave_time_seconds);
        cfg.maxRounds             = Mathf.Max(1,   response.max_waves);
        cfg.roundScalingPercent   = Mathf.Max(0f,  response.enemy_scale_percent);
        cfg.bossScalePercent      = Mathf.Max(0f,  response.boss_scale_percent);
        cfg.expGoldScalePercent   = Mathf.Max(0f,  response.exp_gold_scale_percent);
        cfg.dailyEntryLimit       = Mathf.Max(1,   response.daily_entry_limit);
        cfg.entryItemIdPlusOne    = response.entry_item_plus1_id;
        cfg.entryItemIdPlusTwo = response.entry_item_plus2_id;

        // The API response DTO does not include explicit return_* fields or completion_rewards.
        // Preserve return settings from the fallback config. Optionally override the
        // return scene with the response's `scene_name` if provided (this is the
        // dungeon scene name in the DTO and may be useful as a fallback).
        if (!string.IsNullOrWhiteSpace(response.scene_name))
            cfg.returnSceneName = response.scene_name;

        // Ensure completionRewards list exists (use fallback values if any).
        if (cfg.completionRewards == null)
            cfg.completionRewards = new List<DungeonRewardItemConfig>();

        cfg.enemySpawns = new List<DungeonEnemyUnitConfig>();
        if (response.enemy_spawns != null)
            foreach (var spawn in response.enemy_spawns)
                cfg.enemySpawns.Add(ConvertSpawn(spawn));

        cfg.bossSpawn       = response.boss_spawn != null ? ConvertSpawn(response.boss_spawn) : null;
        cfg.milestoneRewards = ConvertMilestones(response.milestone_rewards);

        state.Config = cfg;
        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} ApplyRuntimeResponseToZone dungeonId={dungeonId} mapId={state.MapId} " +
                  $"maxRounds={cfg.maxRounds} enemySpawns={cfg.enemySpawns.Count} bossId={cfg.bossSpawn?.enemyId ?? -1} milestones={cfg.milestoneRewards?.Count ?? 0}");

        // Diagnostic: dump first few enemy spawns to verify the API actually returned them
        if (cfg.enemySpawns.Count > 0)
        {
            int dumpCount = Mathf.Min(3, cfg.enemySpawns.Count);
            for (int i = 0; i < dumpCount; i++)
            {
                var es = cfg.enemySpawns[i];
                Debug.Log($"[WaveDungeonRuntime][DIAG] enemySpawn[{i}] enemyId={es.enemyId} pos={es.spawnPosition} hp={es.maxHp} level={es.level}");
            }
        }
        else
        {
            Debug.LogError($"[WaveDungeonRuntime][DIAG] enemySpawns RỖNG cho dungeonId={dungeonId} mapId={state.MapId}! Kiểm tra API /dungeon/wave/{dungeonId}/config có trả về enemy_spawns không.");
        }
        if (cfg.bossSpawn != null)
            Debug.Log($"[WaveDungeonRuntime][DIAG] bossSpawn enemyId={cfg.bossSpawn.enemyId} pos={cfg.bossSpawn.spawnPosition} hp={cfg.bossSpawn.maxHp}");
    }

    // Wave round logic (per-zone)

    private void StartRoundForZone(ZoneEncounterState state, int round)
    {
        state.CurrentRound    = round;
        state.RemainingSeconds = Mathf.Max(1, Mathf.CeilToInt(state.Config.roundTimeSeconds));
        state.AliveEnemies.Clear();
        state.BossObject  = null;
        state.BossSpawned = false;
        state.Ended       = false;

        // Update NetworkVariable HUD fallback (sẽ bị ghi đè bởi zone kế tiếp nếu có)
        _currentRound.Value     = round;
        _remainingSeconds.Value = state.RemainingSeconds;
        _maxRounds.Value        = state.MaxRounds;

        SyncWaveStateToZone(state, true);

        float scale       = 1f + Mathf.Max(0, round - 1) * (state.Config.roundScalingPercent / 100f);
        float rewardScale = Mathf.Pow(1f + state.Config.expGoldScalePercent / 100f, Mathf.Max(0, round - 1));

        Debug.Log($"[WaveDungeonRuntime] StartRoundForZone zone={state.ZoneId} round={round} scale={scale:F2} rewardScale={rewardScale:F2} " +
                  $"enemyCount={state.Config.enemySpawns?.Count ?? 0} bossId={state.Config.bossSpawn?.enemyId ?? -1}");

        if (state.Config.enemySpawns != null)
            foreach (var ec in state.Config.enemySpawns)
            {
                var scaled = ScaleReward(ec, rewardScale);
                NetworkObject enemy = SpawnEnemyForZone(state, scaled, scale, false);
                RegisterEnemyForZone(state, enemy, false);
            }

        int spawned = state.AliveEnemies.Count;
        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} round={round} spawned={spawned}");

        if (spawned == 0)
        {
            Debug.LogError($"[WaveDungeonRuntime] zone={state.ZoneId} round={round} — Không spawn được quái! Kiểm tra config.");
            BroadcastStatusToZone(state, $"[Lỗi] Vòng {round}: Không thể spawn quái.");
        }
        else
        {
            BroadcastStatusToZone(state, $"Vòng {round}: Tiêu diệt toàn bộ quái vật!");
        }

        if (state.TimerCoroutine != null) StopCoroutine(state.TimerCoroutine);
        state.TimerCoroutine = StartCoroutine(RoundTimerCoroutineForZone(state));
    }

    private IEnumerator RoundTimerCoroutineForZone(ZoneEncounterState state)
    {
        while (!state.Ended && state.RemainingSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            if (state.Ended) yield break;
            state.RemainingSeconds = Mathf.Max(0, state.RemainingSeconds - 1);
            _remainingSeconds.Value = state.RemainingSeconds; // HUD fallback
            SyncWaveStateToZone(state, false);
        }

        if (!state.Ended)
        {
            Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} — Hết thời gian vòng {state.CurrentRound}. Kết thúc phó bản.");
            state.Ended = true;
            ShowTimeUpToZone(state);
            yield return new WaitForSeconds(3f);
            yield return BeginReturnFlowForZone(state, false, state.Config.returnCountdownSeconds, state.Config.returnMapId, state.Config.returnSceneName);
            FinalizeZone(state);
        }
    }

    // Enemy registration and death handling (per-zone, using closure)

    private void RegisterEnemyForZone(ZoneEncounterState state, NetworkObject networkObject, bool isBoss)
    {
        if (networkObject == null) return;

        if (!isBoss) state.AliveEnemies.Add(networkObject);
        else         state.BossObject = networkObject;

        Debug.Log($"[WaveDungeonRuntime] RegisterEnemyForZone zone={state.ZoneId} netId={networkObject.NetworkObjectId} name={networkObject.name} isBoss={isBoss} aliveCount={state.AliveEnemies.Count}");

        UnityAction handler = null;
        if (networkObject.TryGetComponent<NetworkEnemyHealth>(out var neh))
        {
            handler = () =>
            {
                if (!IsServer) return;
                neh.OnDeath.RemoveListener(handler);
                HandleEnemyDeathForZone(state, networkObject, isBoss);
            };
            neh.OnDeath.AddListener(handler);
            return;
        }

        if (networkObject.TryGetComponent<EnemyHealth>(out var eh))
        {
            handler = () =>
            {
                if (!IsServer) return;
                eh.OnDeath.RemoveListener(handler);
                HandleEnemyDeathForZone(state, networkObject, isBoss);
            };
            eh.OnDeath.AddListener(handler);
        }
        else
        {
            Debug.LogWarning($"[WaveDungeonRuntime] zone={state.ZoneId} — {networkObject.name} không có health component.");
        }
    }

    private void HandleEnemyDeathForZone(ZoneEncounterState state, NetworkObject networkObject, bool isBoss)
    {
        if (state.Ended) return;

        Debug.Log($"[WaveDungeonRuntime] HandleEnemyDeath zone={state.ZoneId} name={networkObject?.name ?? "null"} isBoss={isBoss} alive={state.AliveEnemies.Count}");

        if (!isBoss)
        {
            if (networkObject != null) state.AliveEnemies.Remove(networkObject);
            if (state.AliveEnemies.Count == 0 && !state.BossSpawned)
                SpawnBossForZone(state);
            return;
        }

        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} — Boss vòng {state.CurrentRound} bị tiêu diệt.");
        StartCoroutine(HandleBossDefeatedCoroutineForZone(state));
    }

    private void SpawnBossForZone(ZoneEncounterState state)
    {
        state.BossSpawned = true;
        if (state.Config.bossSpawn == null)
        {
            Debug.LogError($"[WaveDungeonRuntime] zone={state.ZoneId} — bossSpawn null! Xử lý như round hoàn thành.");
            StartCoroutine(HandleBossDefeatedCoroutineForZone(state));
            return;
        }
        float bossScale = 1f + Mathf.Max(0, state.CurrentRound - 1) * (state.Config.bossScalePercent / 100f);
        NetworkObject boss = SpawnEnemyForZone(state, state.Config.bossSpawn, bossScale, true);
        if (boss == null)
        {
            Debug.LogError($"[WaveDungeonRuntime] zone={state.ZoneId} — SpawnBoss FAIL null enemyId={state.Config.bossSpawn.enemyId}");
            StartCoroutine(HandleBossDefeatedCoroutineForZone(state));
            return;
        }
        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} — Boss spawned netId={boss.NetworkObjectId}");
        RegisterEnemyForZone(state, boss, true);
        BroadcastStatusToZone(state, $"Boss vòng {state.CurrentRound} đã xuất hiện!");
    }

    private IEnumerator HandleBossDefeatedCoroutineForZone(ZoneEncounterState state)
    {
        if (state.Ended) yield break;
        state.Ended = true;
        if (state.TimerCoroutine != null) { StopCoroutine(state.TimerCoroutine); state.TimerCoroutine = null; }

        int completedWave = state.CurrentRound;
        yield return GrantMilestoneRewardIfAnyForZone(state, completedWave);

        if (completedWave >= Mathf.Max(1, state.MaxRounds))
        {
            BroadcastStatusToZone(state, "Hoàn thành phó bản! Đang phát thưởng...");
            yield return GrantRewardsToZone(state, state.Config.completionRewards);
            yield return BeginReturnFlowForZone(state, true, state.Config.returnCountdownSeconds, state.Config.returnMapId, state.Config.returnSceneName);
            FinalizeZone(state);
            yield break;
        }

        ShowWaveCompleteToZone(state, completedWave, completedWave + 1);
        yield return new WaitForSeconds(3f);

        if (_activeZones.ContainsKey(state.ZoneId))
        {
            state.Ended = false;
            StartRoundForZone(state, completedWave + 1);
        }
    }

    private void FinalizeZone(ZoneEncounterState state)
    {
        _activeZones.Remove(state.ZoneId);
        WaveSessionManager.Instance?.EndSessionsByZone(state.ZoneId);
        Debug.Log($"[WaveDungeonRuntime] FinalizeZone zone={state.ZoneId} — activeZones còn={_activeZones.Count}");
    }

    // Enemy spawning (set zone context before calling BaseDungeonInstance)

    private NetworkObject SpawnEnemyForZone(ZoneEncounterState state, DungeonEnemyUnitConfig enemyConfig, float scale, bool isBoss)
    {
        SetEncounterLocation(state.MapId, state.ZoneId);
        Debug.Log($"[WaveDungeonRuntime][DIAG] SpawnEnemyForZone zone={state.ZoneId} map={state.MapId} enemyId={enemyConfig?.enemyId ?? -1} pos={enemyConfig?.spawnPosition} isBoss={isBoss}");
        var no = SpawnConfiguredEnemy(enemyConfig, scale, isBoss);
        if (no == null)
            Debug.LogError($"[WaveDungeonRuntime][DIAG] SpawnConfiguredEnemy TRẢ VỀ NULL cho enemyId={enemyConfig?.enemyId ?? -1} (isBoss={isBoss}). Kiểm tra EnemyPrefabManager + map physics scene.");
        return no;
    }

    // Reward (per-zone only)

    private IEnumerator GrantRewardsToZone(ZoneEncounterState state, IReadOnlyList<DungeonRewardItemConfig> rewards)
    {
        if (rewards == null || rewards.Count == 0 || NetworkManager.Singleton == null) yield break;

        ZoneRoom zoneRoom = ZoneRoomRegistry.Instance?.GetRoom(state.MapId, state.ZoneId);
        if (zoneRoom != null)
        {
            ulong[] clients = zoneRoom.GetClientSnapshot();
            Debug.Log($"[WaveDungeonRuntime] GrantRewardsToZone zone={state.ZoneId} clients={clients?.Length ?? 0} items={rewards.Count}");
            if (clients != null)
                foreach (ulong clientId in clients)
                    yield return DungeonRewardGrantService.GrantRewardsToClient(clientId, rewards);
        }
        else
        {
            Debug.LogWarning($"[WaveDungeonRuntime] GrantRewardsToZone zone={state.ZoneId} — ZoneRoom null, fallback GrantRewardsToAll.");
            yield return GrantRewardsToAll(rewards);
        }
    }

    private IEnumerator GrantMilestoneRewardIfAnyForZone(ZoneEncounterState state, int completedWave)
    {
        if (state.Config.milestoneRewards == null || state.Config.milestoneRewards.Count == 0) yield break;
        foreach (var ms in state.Config.milestoneRewards)
        {
            if (ms.atWave != completedWave) continue;
            BroadcastStatusToZone(state, $"Milestone vòng {completedWave}! Phát thưởng...");
            if (ms.items != null && ms.items.Count > 0)
                yield return GrantRewardsToZone(state, ms.items);
            break;
        }
    }

    // State sync & zone-targeted notifications

    private void SyncWaveStateToZone(ZoneEncounterState state, bool broadcastToZone)
    {
        if (!IsServer) return;
        WaveSessionManager.Instance?.UpdateSessionStateByZone(state.ZoneId, state.CurrentRound, state.MaxRounds, state.RemainingSeconds);
        if (!broadcastToZone)
            return;

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        Debug.Log($"[WaveDungeonRuntime][DIAG] SyncWaveStateToZone broadcast=true map={state.MapId} zone={state.ZoneId} round={state.CurrentRound}/{state.MaxRounds} remaining={state.RemainingSeconds} ztc={(ztc != null ? ztc.name : "NULL")}");
        ztc?.BroadcastWaveStateToZone(state.MapId, state.ZoneId, state.CurrentRound, state.MaxRounds, state.RemainingSeconds);
    }

    private void BroadcastStatusToZone(ZoneEncounterState state, string message)
    {
        Debug.Log($"[WaveDungeonRuntime] zone={state.ZoneId} Status: {message}");
        if (!IsServer) return;
        FindAnyObjectByType<ZoneTransitionController>()
            ?.BroadcastDungeonStatusToZone(state.MapId, state.ZoneId, message ?? string.Empty);
    }

    private void ShowTimeUpToZone(ZoneEncounterState state)
    {
        FindAnyObjectByType<ZoneTransitionController>()
            ?.ShowGlobalNotificationToZone(state.MapId, state.ZoneId,
                "Hết Thời Gian", "Bạn đã hết thời gian! Đang đưa về bản đồ chính...", 4f, "Xác nhận");
    }

    private void ShowWaveCompleteToZone(ZoneEncounterState state, int completedWave, int nextWave)
    {
        FindAnyObjectByType<ZoneTransitionController>()
            ?.ShowGlobalNotificationToZone(state.MapId, state.ZoneId,
                "Vòng Hoàn Thành", $"Hoàn thành vòng {completedWave}! Chuẩn bị vòng {nextWave}...", 2.5f, "OK");
    }

    private IEnumerator BeginReturnFlowForZone(ZoneEncounterState state, bool completed, float countdownSeconds, int returnMapId, string returnSceneName)
    {
        int secs = Mathf.Max(1, Mathf.CeilToInt(countdownSeconds));
        FindAnyObjectByType<ZoneTransitionController>()
            ?.BeginDungeonReturnFlowToZone(state.MapId, state.ZoneId, completed, secs, returnMapId,
                string.IsNullOrWhiteSpace(returnSceneName) ? "GameScene" : returnSceneName);
        yield return new WaitForSeconds(secs);
    }

    // Static helpers

    private static DungeonEnemyUnitConfig ScaleReward(DungeonEnemyUnitConfig original, float rewardScale)
    {
        if (Mathf.Approximately(rewardScale, 1f)) return original;
        return new DungeonEnemyUnitConfig
        {
            enemyId       = original.enemyId,
            displayName   = original.displayName,
            spawnPosition = original.spawnPosition,
            maxHp         = original.maxHp,
            maxMp         = original.maxMp,
            attack        = original.attack,
            defense       = original.defense,
            expReward     = Mathf.RoundToInt(original.expReward * rewardScale),
            level         = original.level,
            respawnTime   = original.respawnTime,
            moveSpeed     = original.moveSpeed,
            canFly        = original.canFly,
            drops         = original.drops
        };
    }

    private static DungeonEnemyUnitConfig ConvertSpawn(DungeonWaveEnemySpawnDto spawn)
    {
        return new DungeonEnemyUnitConfig
        {
            enemyId       = spawn.enemy_id,
            displayName   = spawn.enemy_name ?? string.Empty,
            spawnPosition = new Vector3(spawn.spawn_x, spawn.spawn_y, 0f),
            maxHp         = Mathf.Max(1,    spawn.max_hp),
            maxMp         = Mathf.Max(0,    spawn.max_mp),
            attack        = Mathf.Max(1,    spawn.base_damage),
            defense       = Mathf.Max(0,    spawn.base_defense),
            expReward     = Mathf.Max(0,    spawn.exp_reward),
            level         = Mathf.Max(1,    spawn.level),
            respawnTime   = Mathf.Max(0,    spawn.respawn_time),
            moveSpeed     = Mathf.Max(0.1f, spawn.move_speed),
            canFly        = spawn.can_fly,
            elementType   = string.IsNullOrEmpty(spawn.element_type) ? "None" : spawn.element_type,
            drops         = spawn.drops != null ? new List<DropItemEntry>(spawn.drops) : new List<DropItemEntry>()
        };
    }

    private static List<DungeonMilestoneReward> ConvertMilestones(DungeonWaveMilestoneRewardDto[] milestones)
    {
        var result = new List<DungeonMilestoneReward>();
        if (milestones == null) return result;
        foreach (var ms in milestones)
        {
            var reward = new DungeonMilestoneReward
            {
                atWave    = Mathf.Max(1, ms.wave),
                bonusExp  = ms.bonus_exp  < 0 ? 0L : ms.bonus_exp,
                bonusGold = ms.bonus_gold < 0 ? 0L : ms.bonus_gold,
                items     = new List<DungeonRewardItemConfig>()
            };
            if (ms.items != null)
                foreach (var item in ms.items)
                {
                    if (item.item_template_id <= 0) continue;
                    reward.items.Add(new DungeonRewardItemConfig
                    {
                        itemTemplateId = item.item_template_id,
                        quantity       = Mathf.Max(1, item.quantity),
                        upgradeLevel   = Mathf.Max(0, item.upgrade_level),
                        strOptions     = item.str_options ?? string.Empty
                    });
                }
            result.Add(reward);
        }
        return result;
    }

    private static bool IsDedicatedWorldServer()
        => FindAnyObjectByType<MapWorldBootstrap>() != null;
}
