using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WaveDungeonRuntime : BaseDungeonInstance
{
    [Header("Config")]
    [SerializeField] private DungeonWaveConfig config;
    [SerializeField] private string apiBaseUrl = "";

    [Header("Wave UI (tự tạo nếu để trống)")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;

    private readonly NetworkVariable<int> _currentRound = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _remainingSeconds = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _maxRounds = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<NetworkObject> _aliveEnemies = new();
    private NetworkObject _bossObject;
    private Coroutine _roundTimerCoroutine;
    private Coroutine _initializeRoutine;
    private bool _bossSpawned;
    private bool _encounterEnded;
    private bool _runtimeInitialized;
    private int _pendingDungeonId = -1;
    private string _activeEncounterKey = string.Empty;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[WaveDungeonRuntime] OnNetworkSpawn scene={gameObject.scene.name} isServer={IsServer} isClient={IsClient} hasConfig={(config != null)} pendingDungeonId={_pendingDungeonId} activeKey='{_activeEncounterKey}'");

        // Auto-create HUD nếu chưa được gán trong Inspector
        if (IsClient)
            EnsureWaveHUD();

    }

    public void BeginEncounter(int dungeonId, int mapId, int zoneId)
    {
        if (!IsServer || dungeonId <= 0 || mapId < 0)
            return;

        string encounterKey = $"{dungeonId}:{mapId}:{zoneId}";
        if (_runtimeInitialized && string.Equals(_activeEncounterKey, encounterKey, StringComparison.Ordinal))
        {
            Debug.Log($"[WaveDungeonRuntime] Encounter {encounterKey} already initialized.");
            return;
        }

        Debug.Log($"[WaveDungeonRuntime] BeginEncounter dungeonId={dungeonId} mapId={mapId} zoneId={zoneId} scene={gameObject.scene.name} isServer={IsServer} isClient={IsClient} runtimeInitialized={_runtimeInitialized} config={(config != null ? config.name : "null")}");

        ResetEncounterRuntime();
        _pendingDungeonId = dungeonId;
        _activeEncounterKey = encounterKey;
        SetEncounterLocation(mapId, zoneId);

        if (_initializeRoutine != null)
            StopCoroutine(_initializeRoutine);
        _initializeRoutine = StartCoroutine(InitializeRuntimeConfigCoroutine());
    }

    // -------------------------------------------------------
    //  Wave HUD — tự tạo canvas nếu thiếu ref
    // -------------------------------------------------------

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

    private void ResetEncounterRuntime()
    {
        if (_roundTimerCoroutine != null)
        {
            StopCoroutine(_roundTimerCoroutine);
            _roundTimerCoroutine = null;
        }

        if (_initializeRoutine != null)
        {
            StopCoroutine(_initializeRoutine);
            _initializeRoutine = null;
        }

        foreach (NetworkObject enemy in _aliveEnemies)
            DespawnNetworkObject(enemy);

        _aliveEnemies.Clear();
        DespawnNetworkObject(_bossObject);
        _bossObject = null;
        _bossSpawned = false;
        _encounterEnded = false;
        _runtimeInitialized = false;
        _currentRound.Value = 0;
        _remainingSeconds.Value = 0;
        _maxRounds.Value = 0;
    }

    private void MarkEncounterFinished()
    {
        _runtimeInitialized = false;
        _pendingDungeonId = -1;
        _activeEncounterKey = string.Empty;
        ClearEncounterLocation();
    }

    private static void DespawnNetworkObject(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        if (networkObject.IsSpawned)
            networkObject.Despawn(true);
        else if (networkObject.gameObject != null)
            Destroy(networkObject.gameObject);
    }

    private void Update()
    {
        if (!IsClient)
            return;

        int maxRounds = _maxRounds.Value > 0
            ? _maxRounds.Value
            : Mathf.Max(1, config != null ? config.maxRounds : 1);

        if (roundText != null)
            roundText.text = $"Vòng {_currentRound.Value}/{maxRounds}";

        if (timerText != null)
        {
            int seconds = Mathf.Max(0, _remainingSeconds.Value);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }

    private IEnumerator InitializeRuntimeConfigCoroutine()
    {
        _runtimeInitialized = true;

        if (config != null)
            config = Instantiate(config);

        bool loadedFromApi = false;
        yield return StartCoroutine(LoadConfigFromApiCoroutine(success => loadedFromApi = success));

        if (!loadedFromApi)
        {
            Debug.LogWarning($"[WaveDungeonRuntime] Không tải được wave config từ API. Fallback sang DungeonWaveConfig trong scene. scene={gameObject.scene.name} dungeonId={_pendingDungeonId} config={(config != null ? config.name : "null")}");
        }

        if (config == null)
        {
            Debug.LogError($"[WaveDungeonRuntime] Không có config khả dụng để khởi tạo wave dungeon. scene={gameObject.scene.name} dungeonId={_pendingDungeonId} activeKey='{_activeEncounterKey}'");
            BroadcastStatus("[Lỗi] Không tải được cấu hình phó bản sóng.");
            _initializeRoutine = null;
            yield break;
        }

        _maxRounds.Value = Mathf.Max(1, config.maxRounds);
        StartRound(1);
        _initializeRoutine = null;
    }

    private IEnumerator LoadConfigFromApiCoroutine(System.Action<bool> onCompleted)
    {
        int dungeonId = ResolveDungeonId();
        if (dungeonId <= 0)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        string resolvedApiUrl = ServerAddressConfig.Instance.ResolveApiUrl(apiBaseUrl);
        string url = $"{resolvedApiUrl}/dungeon/wave/{dungeonId}/config";
        Debug.Log($"[WaveDungeonRuntime] Loading runtime config from API: {url} (scene={gameObject.scene.name}, dungeonId={dungeonId}, forcedMapId={ForcedMapId}, forcedZoneId={ForcedZoneId})");

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
            Debug.LogWarning($"[WaveDungeonRuntime] Load runtime config failed: {request.error}; responseCode={request.responseCode}; text={request.downloadHandler?.text}");
            onCompleted?.Invoke(false);
            yield break;
        }

        Debug.Log($"[WaveDungeonRuntime] Runtime config JSON: {request.downloadHandler.text}");
        DungeonWaveRuntimeResponse response = JsonUtility.FromJson<DungeonWaveRuntimeResponse>(request.downloadHandler.text);
        if (response == null)
        {
            Debug.LogWarning($"[WaveDungeonRuntime] API trả về JSON wave config không parse được. scene={gameObject.scene.name}");
            onCompleted?.Invoke(false);
            yield break;
        }

        ApplyRuntimeResponse(response, dungeonId);
        onCompleted?.Invoke(true);
    }

    private void ApplyRuntimeResponse(DungeonWaveRuntimeResponse response, int dungeonId)
    {
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DungeonWaveConfig>();
            config.returnSceneName = "GameScene";
            config.returnMapId = 0;
            config.returnCountdownSeconds = 5f;
        }

        config.dungeonId = dungeonId;
        if (response.map_id > 0 && ForcedMapId < 0)
            SetEncounterLocation(response.map_id, 0);
        config.roundTimeSeconds = Mathf.Max(1f, response.wave_time_seconds);
        config.maxRounds = Mathf.Max(1, response.max_waves);
        config.roundScalingPercent = Mathf.Max(0f, response.enemy_scale_percent);
        config.bossScalePercent = Mathf.Max(0f, response.boss_scale_percent);
        config.expGoldScalePercent = Mathf.Max(0f, response.exp_gold_scale_percent);
        config.dailyEntryLimit = Mathf.Max(1, response.daily_entry_limit);
        config.entryItemIdPlusOne = response.entry_item_plus1_id;
        config.entryItemIdPlusTwo = response.entry_item_plus2_id;

        config.enemySpawns = new List<DungeonEnemyUnitConfig>();
        if (response.enemy_spawns != null)
        {
            foreach (var spawn in response.enemy_spawns)
                config.enemySpawns.Add(ConvertSpawn(spawn));
        }

        config.bossSpawn = response.boss_spawn != null ? ConvertSpawn(response.boss_spawn) : null;
        config.milestoneRewards = ConvertMilestones(response.milestone_rewards);

        Debug.Log($"[WaveDungeonRuntime] Loaded runtime config from DB: scene={gameObject.scene.name}, dungeonId={dungeonId}, mapId={config.returnMapId}, normalSpawns={config.enemySpawns.Count}, bossEnemyId={config.bossSpawn?.enemyId ?? -1}, maxRounds={config.maxRounds}, milestoneCount={config.milestoneRewards?.Count ?? 0}");
    }

    private static DungeonEnemyUnitConfig ConvertSpawn(DungeonWaveEnemySpawnDto spawn)
    {
        return new DungeonEnemyUnitConfig
        {
            enemyId = spawn.enemy_id,
            displayName = spawn.enemy_name ?? string.Empty,
            spawnPosition = new Vector3(spawn.spawn_x, spawn.spawn_y, 0f),
            maxHp = Mathf.Max(1, spawn.max_hp),
            maxMp = Mathf.Max(0, spawn.max_mp),
            attack = Mathf.Max(1, spawn.base_damage),
            defense = Mathf.Max(0, spawn.base_defense),
            expReward = Mathf.Max(0, spawn.exp_reward),
            level = Mathf.Max(1, spawn.level),
            respawnTime = Mathf.Max(0, spawn.respawn_time),
            moveSpeed = Mathf.Max(0.1f, spawn.move_speed),
            canFly = spawn.can_fly,
            drops = spawn.drops != null ? new List<DropItemEntry>(spawn.drops) : new List<DropItemEntry>()
        };
    }

    private static List<DungeonMilestoneReward> ConvertMilestones(DungeonWaveMilestoneRewardDto[] milestones)
    {
        var result = new List<DungeonMilestoneReward>();
        if (milestones == null)
            return result;

        foreach (var milestone in milestones)
        {
                var reward = new DungeonMilestoneReward
                {
                    atWave = Mathf.Max(1, milestone.wave),
                    bonusExp = milestone.bonus_exp < 0 ? 0L : milestone.bonus_exp,
                    bonusGold = milestone.bonus_gold < 0 ? 0L : milestone.bonus_gold,
                    items = new List<DungeonRewardItemConfig>()
                };

            if (milestone.items != null)
            {
                foreach (var item in milestone.items)
                {
                    if (item.item_template_id <= 0)
                        continue;

                    reward.items.Add(new DungeonRewardItemConfig
                    {
                        itemTemplateId = item.item_template_id,
                        quantity = Mathf.Max(1, item.quantity),
                        upgradeLevel = Mathf.Max(0, item.upgrade_level),
                        strOptions = item.str_options ?? string.Empty
                    });
                }
            }

            result.Add(reward);
        }

        return result;
    }

    private int ResolveDungeonId()
    {
        if (_pendingDungeonId > 0)
            return _pendingDungeonId;

        if (config != null && config.dungeonId > 0)
            return config.dungeonId;

        var dungeonManager = FindAnyObjectByType<DungeonManager>();
        if (dungeonManager != null && dungeonManager.ActiveDungeonId > 0)
            return dungeonManager.ActiveDungeonId;

        return -1;
    }

    private static bool IsDedicatedWorldServer()
        => FindAnyObjectByType<MapWorldBootstrap>() != null;

    private void StartRound(int round)
    {
        _currentRound.Value = round;
        _remainingSeconds.Value = Mathf.Max(1, Mathf.CeilToInt(config.roundTimeSeconds));
        _aliveEnemies.Clear();
        _bossObject = null;
        _bossSpawned = false;
        _encounterEnded = false;

        // Quái thường scale theo roundScalingPercent (lũy thừa)
        float scale = 1f + Mathf.Max(0, round - 1) * (config.roundScalingPercent / 100f);
        // EXP/Gold scale riêng để tránh mất cân bằng ở vòng cao
        float rewardScale = Mathf.Pow(1f + config.expGoldScalePercent / 100f, Mathf.Max(0, round - 1));

        Debug.Log($"[WaveDungeonRuntime] StartRound({round}): scene={gameObject.scene.name}, enemySpawns.Count={config.enemySpawns?.Count ?? 0}, bossSpawn.enemyId={config.bossSpawn?.enemyId}, scale={scale:F2}, rewardScale={rewardScale:F2}, maxRounds={config.maxRounds}");

        foreach (var enemyConfig in config.enemySpawns)
        {
            // Scale exp reward theo vòng trước khi spawn
            var scaledConfig = ScaleReward(enemyConfig, rewardScale);
            NetworkObject enemy = SpawnConfiguredEnemy(scaledConfig, scale, false);
            RegisterEnemy(enemy, false);
        }

        int spawnedCount = _aliveEnemies.Count;
        Debug.Log($"[WaveDungeonRuntime] StartRound({round}): {spawnedCount} enemy đã được đăng ký vào _aliveEnemies.");

        if (spawnedCount == 0)
        {
            Debug.LogError($"[WaveDungeonRuntime] CẢNH BÁO: Vòng {round} không spawn được enemy nào! " +
                "Nguyên nhân có thể: (1) config.enemySpawns rỗng trong DungeonWaveConfig SO, " +
                "(2) enemyId không hợp lệ trong EnemyPrefabManager, " +
                "(3) config.maxHp = 0 (enemy chết ngay). " +
                "Boss sẽ KHÔNG spawn cho đến khi ít nhất 1 enemy được giết. Timer đang chạy.");
            BroadcastStatus($"[Lỗi] Vòng {round}: Không thể spawn quái — kiểm tra DungeonWaveConfig SO.");
        }
        else
        {
            BroadcastStatus($"Vòng {round}: tiêu diệt toàn bộ quái vật.");
        }

        if (_roundTimerCoroutine != null)
            StopCoroutine(_roundTimerCoroutine);
        _roundTimerCoroutine = StartCoroutine(RoundTimerCoroutine());
    }

    private IEnumerator RoundTimerCoroutine()
    {
        while (!_encounterEnded && _remainingSeconds.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            _remainingSeconds.Value = Mathf.Max(0, _remainingSeconds.Value - 1);
        }

        if (_encounterEnded)
            yield break;

        _encounterEnded = true;
        BroadcastStatus("Hết thời gian phó bản.");
        yield return BeginReturnFlow(false, config.returnCountdownSeconds, config.returnMapId, config.returnSceneName);
    }

    private void RegisterEnemy(NetworkObject networkObject, bool isBoss)
    {
        if (networkObject == null)
            return;

        if (!isBoss)
            _aliveEnemies.Add(networkObject);
        else
            _bossObject = networkObject;

        Debug.Log($"[WaveDungeonRuntime] RegisterEnemy: netId={(networkObject != null ? networkObject.NetworkObjectId : 0)} name={networkObject.name} isBoss={isBoss} aliveCount={_aliveEnemies.Count} bossSpawned={_bossSpawned} scene={gameObject.scene.name}");

        UnityAction handler = null;
        if (networkObject.TryGetComponent<NetworkEnemyHealth>(out var networkEnemyHealth))
        {
            handler = () =>
            {
                if (!IsServer)
                    return;
                Debug.Log($"[WaveDungeonRuntime] OnDeath event from NetworkEnemyHealth: name={networkObject.name} netId={networkObject.NetworkObjectId} isBoss={isBoss}");
                networkEnemyHealth.OnDeath.RemoveListener(handler);
                HandleEnemyDeath(networkObject, isBoss);
            };
            networkEnemyHealth.OnDeath.AddListener(handler);
            return;
        }

        if (networkObject.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            handler = () =>
            {
                if (!IsServer)
                    return;
                Debug.Log($"[WaveDungeonRuntime] OnDeath event from EnemyHealth: name={networkObject.name} netId={networkObject.NetworkObjectId} isBoss={isBoss}");
                enemyHealth.OnDeath.RemoveListener(handler);
                HandleEnemyDeath(networkObject, isBoss);
            };
            enemyHealth.OnDeath.AddListener(handler);
        }
        else
        {
            Debug.LogWarning($"[WaveDungeonRuntime] RegisterEnemy: no health component found on {networkObject.name} netId={networkObject.NetworkObjectId}");
        }
    }

    private void HandleEnemyDeath(NetworkObject networkObject, bool isBoss)
    {
        Debug.Log($"[WaveDungeonRuntime] HandleEnemyDeath: name={networkObject?.name ?? "null"} netId={(networkObject != null ? networkObject.NetworkObjectId : 0)} isBoss={isBoss}, aliveEnemies={_aliveEnemies.Count}, bossSpawned={_bossSpawned}, encounterEnded={_encounterEnded}, scene={gameObject.scene.name}");

        if (_encounterEnded)
            return;

        if (!isBoss)
        {
            if (networkObject != null)
                _aliveEnemies.Remove(networkObject);

            Debug.Log($"[WDR] kill nonboss alive={_aliveEnemies.Count} bossSpawned={_bossSpawned} round={_currentRound.Value} scene={gameObject.scene.name}");
            if (_aliveEnemies.Count == 0 && !_bossSpawned)
            {
                Debug.Log($"[WDR] spawn boss round={_currentRound.Value} scene={gameObject.scene.name}");
                SpawnBoss();
            }
            return;
        }

        Debug.Log($"[WaveDungeonRuntime] Boss vòng {_currentRound.Value} đã bị tiêu diệt.");
        StartCoroutine(HandleBossDefeatedCoroutine());
    }

    private void SpawnBoss()
    {
        _bossSpawned = true;
        if (config.bossSpawn == null)
        {
            Debug.LogError($"[WaveDungeonRuntime] SpawnBoss: config.bossSpawn là null! Kiểm tra DungeonWaveConfig SO. scene={gameObject.scene.name} round={_currentRound.Value}");
            // Không có boss config — xử lý như round hoàn thành ngay
            StartCoroutine(HandleBossDefeatedCoroutine());
            return;
        }
        // Boss dùng bossScalePercent riêng (config độc lập với quái thường)
        float bossScale = 1f + Mathf.Max(0, _currentRound.Value - 1) * (config.bossScalePercent / 100f);
        Debug.Log($"[WDR] boss spawn start enemyId={config.bossSpawn.enemyId} round={_currentRound.Value} map={ResolveCurrentMapId()} zone={ResolveCurrentZoneId()} scene={gameObject.scene.name}");
        NetworkObject boss = SpawnConfiguredEnemy(config.bossSpawn, bossScale, true);
        if (boss == null)
        {
            Debug.LogError($"[WDR] boss spawn FAIL null enemyId={config.bossSpawn.enemyId} round={_currentRound.Value} scene={gameObject.scene.name}");
            StartCoroutine(HandleBossDefeatedCoroutine());
            return;
        }
        Debug.Log($"[WDR] boss spawned netId={boss.NetworkObjectId} name={boss.name} scene={gameObject.scene.name}");
        RegisterEnemy(boss, true);
        BroadcastStatus($"Boss vòng {_currentRound.Value} đã xuất hiện.");
    }

    private IEnumerator HandleBossDefeatedCoroutine()
    {
        if (_encounterEnded)
            yield break;

        _encounterEnded = true;
        if (_roundTimerCoroutine != null)
            StopCoroutine(_roundTimerCoroutine);

        int completedWave = _currentRound.Value;
        Debug.Log($"[WaveDungeonRuntime] HandleBossDefeatedCoroutine start: completedWave={completedWave}, maxRounds={config.maxRounds}, scene={gameObject.scene.name}");

        // Milestone reward tại vòng 5 / 10 / 15 / 20
        yield return GrantMilestoneRewardIfAny(completedWave);

        if (completedWave >= Mathf.Max(1, config.maxRounds))
        {
            Debug.Log($"[WaveDungeonRuntime] Dungeon complete at wave {completedWave}. Grant completion rewards.");
            BroadcastStatus("Đã hoàn thành toàn bộ phó bản. Đang phát thưởng.");
            yield return GrantRewardsToAll(config.completionRewards);
            yield return BeginReturnFlow(true, config.returnCountdownSeconds, config.returnMapId, config.returnSceneName);
            yield break;
        }

        BroadcastStatus($"Hoàn thành vòng {completedWave}. Chuẩn bị vòng tiếp theo.");
        yield return new WaitForSeconds(3f);
        Debug.Log($"[WaveDungeonRuntime] Advancing to next wave: {completedWave + 1}");
        StartRound(completedWave + 1);
    }

    // -------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------

    /// <summary>
    /// Tạo bản sao config với expReward đã nhân rewardScale.
    /// Không sửa config gốc trong SO.
    /// </summary>
    private static DungeonEnemyUnitConfig ScaleReward(DungeonEnemyUnitConfig original, float rewardScale)
    {
        if (Mathf.Approximately(rewardScale, 1f))
            return original;
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

    /// <summary>
    /// Grant milestone reward nếu vòng vừa clear khớp với atWave trong config.
    /// </summary>
    private System.Collections.IEnumerator GrantMilestoneRewardIfAny(int completedWave)
    {
        if (config.milestoneRewards == null || config.milestoneRewards.Count == 0)
            yield break;

        foreach (var milestone in config.milestoneRewards)
        {
            if (milestone.atWave != completedWave)
                continue;

            BroadcastStatus($"Milestone vòng {completedWave}! Đang phát thưởng.");
            if (milestone.items != null && milestone.items.Count > 0)
                yield return GrantRewardsToAll(milestone.items);
            break;
        }
    }
}
