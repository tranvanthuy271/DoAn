using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServerApi.Services
{
    /// <summary>
    /// Cache tất cả config tables vào memory khi server khởi động.
    /// Giảm latency cho API không cần query DB mỗi request.
    ///
    /// Sử dụng: inject IGameConfigCache vào controller.
    ///   var enemy = _cache.GetEnemy(enemyId);
    ///   var spawns = _cache.GetSpawnsByMap(mapId);
    ///
    /// Reload: POST /api/admin/reload-config hoặc gọi ReloadAllAsync()
    /// </summary>
    public interface IGameConfigCache
    {
        // ── Enemy ──
        Enemy? GetEnemy(int enemyId);
        IReadOnlyList<Enemy> GetAllEnemies();

        // ── Spawn ──
        IReadOnlyList<EnemySpawn> GetSpawnsByMap(int mapId);
        IReadOnlyList<EnemySpawn> GetAllSpawns();

        // ── Boss ──
        BossConfig? GetBossConfig(int bossId);

        // ── Item ──
        ItemTemplate? GetItem(int itemId);
        IReadOnlyList<ItemTemplate> GetAllItems();
        IReadOnlyList<ItemEffectTemplate> GetItemEffects(int itemTemplateId);

        // ── Skill ──
        SkillTemplate? GetSkill(int skillId);
        SkillTemplate? GetSkillByCode(string skillCode);
        IReadOnlyList<SkillTemplate> GetAllSkills();

        // ── Option Template ──
        OptionTemplate? GetOption(int optionId);
        IReadOnlyList<OptionTemplate> GetAllOptions();

        // ── Upgrade ──
        EquipmentUpgradeConfig? GetUpgradeConfig(int level);

        // ── Exp ──
        ExpRequirement? GetExpRequirement(int level);

        // ── Gene ──
        GeneUpgradeConfig? GetGeneUpgrade(int tierFrom, string elementType);
        GeneMultiConfig? GetGeneMulti(int tierFrom, string elementType);
        GeneTierStatConfig? GetGeneTierStat(string elementType, int tierTo);
        GeneHybridConfig? GetHybrid(int hybridId);
        IReadOnlyList<GeneHybridSkill> GetHybridSkills(int hybridId);

        // ── Map ──
        MapConfig? GetMap(int mapId);
        IReadOnlyList<MapPortal> GetPortalsBySourceMap(int sourceMapId);

        // ── NPC ──
        NpcConfig? GetNpc(int npcId);
        IReadOnlyList<NpcDialogue> GetDialogues(int npcId);

        // ── Dungeon ──
        DungeonConfig? GetDungeon(int dungeonId);
        IReadOnlyList<DungeonConfig> GetAllDungeons();

        // ── Reload ──
        Task ReloadAllAsync(CancellationToken ct = default);
    }

    public class GameConfigCache : IGameConfigCache, IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GameConfigCache> _logger;

        // ── Dictionaries ──
        private ConcurrentDictionary<int, Enemy> _enemies = new();
        private ConcurrentDictionary<int, List<EnemySpawn>> _spawnsByMap = new();
        private List<EnemySpawn> _allSpawns = new();
        private ConcurrentDictionary<int, BossConfig> _bossConfigs = new();
        private ConcurrentDictionary<int, ItemTemplate> _items = new();
        private ConcurrentDictionary<int, List<ItemEffectTemplate>> _itemEffects = new();
        private ConcurrentDictionary<int, SkillTemplate> _skills = new();
        private ConcurrentDictionary<string, SkillTemplate> _skillsByCode = new();
        private ConcurrentDictionary<int, OptionTemplate> _options = new();
        private ConcurrentDictionary<int, EquipmentUpgradeConfig> _upgradeConfigs = new();
        private ConcurrentDictionary<int, ExpRequirement> _expReqs = new();
        private ConcurrentDictionary<string, GeneUpgradeConfig> _geneUpgrades = new();
        private ConcurrentDictionary<string, GeneMultiConfig> _geneMultis = new();
        private ConcurrentDictionary<string, GeneTierStatConfig> _geneTierStats = new();
        private ConcurrentDictionary<int, GeneHybridConfig> _hybrids = new();
        private ConcurrentDictionary<int, List<GeneHybridSkill>> _hybridSkills = new();
        private ConcurrentDictionary<int, MapConfig> _maps = new();
        private ConcurrentDictionary<int, List<MapPortal>> _portalsBySource = new();
        private ConcurrentDictionary<int, NpcConfig> _npcs = new();
        private ConcurrentDictionary<int, List<NpcDialogue>> _dialogues = new();
        private ConcurrentDictionary<int, DungeonConfig> _dungeons = new();

        public GameConfigCache(IServiceScopeFactory scopeFactory, ILogger<GameConfigCache> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── IHostedService ──────────────────────────────────
        public async Task StartAsync(CancellationToken ct)
        {
            _logger.LogInformation("GameConfigCache: Loading all config tables...");
            await ReloadAllAsync(ct);
            _logger.LogInformation("GameConfigCache: All config loaded.");
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

        // ── Reload ──────────────────────────────────────────
        public async Task ReloadAllAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            // Enemy
            var enemies = await db.Enemies.AsNoTracking().ToListAsync(ct);
            _enemies = new ConcurrentDictionary<int, Enemy>(enemies.ToDictionary(e => e.EnemyId));

            // Spawns
            _allSpawns = await db.EnemySpawns.AsNoTracking().ToListAsync(ct);
            _spawnsByMap = new ConcurrentDictionary<int, List<EnemySpawn>>(
                _allSpawns.GroupBy(s => s.MapId).ToDictionary(g => g.Key, g => g.ToList()));

            // Boss
            var bosses = await db.BossConfigs.AsNoTracking().ToListAsync(ct);
            _bossConfigs = new ConcurrentDictionary<int, BossConfig>(bosses.ToDictionary(b => b.BossId));

            // Items
            var items = await db.ItemTemplates.AsNoTracking().ToListAsync(ct);
            _items = new ConcurrentDictionary<int, ItemTemplate>(items.ToDictionary(i => (int)i.Id));

            // Item Effects
            var effects = await db.ItemEffectTemplates.AsNoTracking().ToListAsync(ct);
            _itemEffects = new ConcurrentDictionary<int, List<ItemEffectTemplate>>(
                effects.GroupBy(e => e.ItemTemplateId).ToDictionary(g => g.Key, g => g.ToList()));

            // Skills
            var skills = await db.SkillTemplates.AsNoTracking().ToListAsync(ct);
            _skills = new ConcurrentDictionary<int, SkillTemplate>(skills.ToDictionary(s => s.SkillId));
            _skillsByCode = new ConcurrentDictionary<string, SkillTemplate>(
                skills.ToDictionary(s => s.SkillCode, System.StringComparer.OrdinalIgnoreCase));

            // Options
            var options = await db.OptionTemplates.AsNoTracking().ToListAsync(ct);
            _options = new ConcurrentDictionary<int, OptionTemplate>(options.ToDictionary(o => o.Id));

            // Upgrade
            var upgrades = await db.EquipmentUpgradeConfigs.AsNoTracking().ToListAsync(ct);
            _upgradeConfigs = new ConcurrentDictionary<int, EquipmentUpgradeConfig>(
                upgrades.ToDictionary(u => u.UpgradeLevel));

            // Exp
            var exps = await db.ExpRequirements.AsNoTracking().ToListAsync(ct);
            _expReqs = new ConcurrentDictionary<int, ExpRequirement>(exps.ToDictionary(e => e.Level));

            // Gene Upgrade
            var geneUp = await db.GeneUpgradeConfigs.AsNoTracking().ToListAsync(ct);
            _geneUpgrades = new ConcurrentDictionary<string, GeneUpgradeConfig>(
                geneUp.ToDictionary(g => $"{g.TierFrom}:{g.ElementType}"));

            // Gene Multi
            var geneMulti = await db.GeneMultiConfigs.AsNoTracking().ToListAsync(ct);
            _geneMultis = new ConcurrentDictionary<string, GeneMultiConfig>(
                geneMulti.ToDictionary(g => $"{g.TierFrom}:{g.ElementType}"));

            // Gene Tier Stat
            var geneTier = await db.GeneTierStatConfigs.AsNoTracking().ToListAsync(ct);
            _geneTierStats = new ConcurrentDictionary<string, GeneTierStatConfig>(
                geneTier.ToDictionary(g => $"{g.ElementType}:{g.TierTo}"));

            // Hybrids
            var hybrids = await db.GeneHybridConfigs.AsNoTracking().ToListAsync(ct);
            _hybrids = new ConcurrentDictionary<int, GeneHybridConfig>(hybrids.ToDictionary(h => h.HybridId));

            var hybridSkills = await db.GeneHybridSkills.AsNoTracking().ToListAsync(ct);
            _hybridSkills = new ConcurrentDictionary<int, List<GeneHybridSkill>>(
                hybridSkills.GroupBy(s => s.HybridId).ToDictionary(g => g.Key, g => g.ToList()));

            // Maps
            var maps = await db.MapConfigs.AsNoTracking().ToListAsync(ct);
            _maps = new ConcurrentDictionary<int, MapConfig>(maps.ToDictionary(m => m.MapId));

            // Portals
            var portals = await db.MapPortals.AsNoTracking().ToListAsync(ct);
            _portalsBySource = new ConcurrentDictionary<int, List<MapPortal>>(
                portals.GroupBy(p => p.SourceMapId).ToDictionary(g => g.Key, g => g.ToList()));

            // NPCs
            var npcs = await db.NpcConfigs.AsNoTracking().ToListAsync(ct);
            _npcs = new ConcurrentDictionary<int, NpcConfig>(npcs.ToDictionary(n => n.NpcId));

            var dialogues = await db.NpcDialogues.AsNoTracking().ToListAsync(ct);
            _dialogues = new ConcurrentDictionary<int, List<NpcDialogue>>(
                dialogues.GroupBy(d => d.NpcId).ToDictionary(g => g.Key, g => g.ToList()));

            // Dungeons
            var dungeons = await db.DungeonConfigs.AsNoTracking().ToListAsync(ct);
            _dungeons = new ConcurrentDictionary<int, DungeonConfig>(dungeons.ToDictionary(d => d.DungeonId));

            _logger.LogInformation(
                "Config loaded: {enemies} enemies, {spawns} spawns, {items} items, {skills} skills, {options} options",
                _enemies.Count, _allSpawns.Count, _items.Count, _skills.Count, _options.Count);
        }

        // ── Getters ─────────────────────────────────────────

        public Enemy? GetEnemy(int enemyId) => _enemies.GetValueOrDefault(enemyId);
        public IReadOnlyList<Enemy> GetAllEnemies() => _enemies.Values.ToList();

        public IReadOnlyList<EnemySpawn> GetSpawnsByMap(int mapId) =>
            _spawnsByMap.GetValueOrDefault(mapId) ?? (IReadOnlyList<EnemySpawn>)System.Array.Empty<EnemySpawn>();
        public IReadOnlyList<EnemySpawn> GetAllSpawns() => _allSpawns;

        public BossConfig? GetBossConfig(int bossId) => _bossConfigs.GetValueOrDefault(bossId);

        public ItemTemplate? GetItem(int itemId) => _items.GetValueOrDefault(itemId);
        public IReadOnlyList<ItemTemplate> GetAllItems() => _items.Values.ToList();
        public IReadOnlyList<ItemEffectTemplate> GetItemEffects(int itemTemplateId) =>
            _itemEffects.GetValueOrDefault(itemTemplateId) ?? (IReadOnlyList<ItemEffectTemplate>)System.Array.Empty<ItemEffectTemplate>();

        public SkillTemplate? GetSkill(int skillId) => _skills.GetValueOrDefault(skillId);
        public SkillTemplate? GetSkillByCode(string skillCode) =>
            _skillsByCode.GetValueOrDefault(skillCode);
        public IReadOnlyList<SkillTemplate> GetAllSkills() => _skills.Values.ToList();

        public OptionTemplate? GetOption(int optionId) => _options.GetValueOrDefault(optionId);
        public IReadOnlyList<OptionTemplate> GetAllOptions() => _options.Values.ToList();

        public EquipmentUpgradeConfig? GetUpgradeConfig(int level) => _upgradeConfigs.GetValueOrDefault(level);

        public ExpRequirement? GetExpRequirement(int level) => _expReqs.GetValueOrDefault(level);

        public GeneUpgradeConfig? GetGeneUpgrade(int tierFrom, string elementType) =>
            _geneUpgrades.GetValueOrDefault($"{tierFrom}:{elementType}");
        public GeneMultiConfig? GetGeneMulti(int tierFrom, string elementType) =>
            _geneMultis.GetValueOrDefault($"{tierFrom}:{elementType}");
        public GeneTierStatConfig? GetGeneTierStat(string elementType, int tierTo) =>
            _geneTierStats.GetValueOrDefault($"{elementType}:{tierTo}");
        public GeneHybridConfig? GetHybrid(int hybridId) => _hybrids.GetValueOrDefault(hybridId);
        public IReadOnlyList<GeneHybridSkill> GetHybridSkills(int hybridId) =>
            _hybridSkills.GetValueOrDefault(hybridId) ?? (IReadOnlyList<GeneHybridSkill>)System.Array.Empty<GeneHybridSkill>();

        public MapConfig? GetMap(int mapId) => _maps.GetValueOrDefault(mapId);
        public IReadOnlyList<MapPortal> GetPortalsBySourceMap(int sourceMapId) =>
            _portalsBySource.GetValueOrDefault(sourceMapId) ?? (IReadOnlyList<MapPortal>)System.Array.Empty<MapPortal>();

        public NpcConfig? GetNpc(int npcId) => _npcs.GetValueOrDefault(npcId);
        public IReadOnlyList<NpcDialogue> GetDialogues(int npcId) =>
            _dialogues.GetValueOrDefault(npcId) ?? (IReadOnlyList<NpcDialogue>)System.Array.Empty<NpcDialogue>();

        public DungeonConfig? GetDungeon(int dungeonId) => _dungeons.GetValueOrDefault(dungeonId);
        public IReadOnlyList<DungeonConfig> GetAllDungeons() => _dungeons.Values.ToList();
    }
}
