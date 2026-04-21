using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<PlayerData> PlayerData => Set<PlayerData>();
        public DbSet<ExpRequirement> ExpRequirements => Set<ExpRequirement>();
        public DbSet<MapConfig> MapConfigs => Set<MapConfig>();
        public DbSet<Enemy> Enemies => Set<Enemy>();
        public DbSet<EnemySpawn> EnemySpawns => Set<EnemySpawn>();
        public DbSet<ItemTemplate> ItemTemplates => Set<ItemTemplate>();
        public DbSet<SkillTemplate> SkillTemplates => Set<SkillTemplate>();
        public DbSet<EquipmentUpgradeConfig> EquipmentUpgradeConfigs  => Set<EquipmentUpgradeConfig>();
        public DbSet<GeneUpgradeConfig>       GeneUpgradeConfigs       => Set<GeneUpgradeConfig>();
        public DbSet<GeneTierStatConfig>      GeneTierStatConfigs      => Set<GeneTierStatConfig>();
        public DbSet<DungeonConfig>           DungeonConfigs           => Set<DungeonConfig>();
        public DbSet<DungeonSession>          DungeonSessions          => Set<DungeonSession>();
        public DbSet<GeneMultiConfig>         GeneMultiConfigs         => Set<GeneMultiConfig>();
        public DbSet<GeneHybridConfig>        GeneHybridConfigs        => Set<GeneHybridConfig>();
        public DbSet<GeneHybridSkill>         GeneHybridSkills         => Set<GeneHybridSkill>();
        public DbSet<NpcConfig>                NpcConfigs               => Set<NpcConfig>();
        public DbSet<NpcShopItem>              NpcShopItems             => Set<NpcShopItem>();
        public DbSet<NpcDialogue>              NpcDialogues             => Set<NpcDialogue>();
        public DbSet<MapPortal>                MapPortals               => Set<MapPortal>();
        public DbSet<BossConfig>               BossConfigs              => Set<BossConfig>();
        public DbSet<ItemEffectTemplate>       ItemEffectTemplates       => Set<ItemEffectTemplate>();
        public DbSet<OptionTemplate>           OptionTemplates          => Set<OptionTemplate>();
        public DbSet<MapEnemyDrop>             MapEnemyDrops            => Set<MapEnemyDrop>();

        // ── Normalized player data tables (Phase 2) ──────────────────
        public DbSet<PlayerEquipment>   PlayerEquipments   => Set<PlayerEquipment>();
        public DbSet<PlayerInventory>   PlayerInventories  => Set<PlayerInventory>();
        public DbSet<PlayerSkillRecord> PlayerSkillRecords => Set<PlayerSkillRecord>();
        public DbSet<PlayerActionLog>   PlayerActionLogs   => Set<PlayerActionLog>();

        // ── Chat & Social ─────────────────────────────────────────────────────
        public DbSet<GameServerApi.Models.Entities.FriendRelation> FriendRelations => Set<GameServerApi.Models.Entities.FriendRelation>();

        // ── Dungeon Wave (entry limit + session reconnect) ────────────────────
        public DbSet<GameServerApi.Models.Entities.DungeonWaveEntry>   DungeonWaveEntries   => Set<GameServerApi.Models.Entities.DungeonWaveEntry>();
        public DbSet<GameServerApi.Models.Entities.DungeonWaveSession> DungeonWaveSessions  => Set<GameServerApi.Models.Entities.DungeonWaveSession>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(u => u.UserId);

                // Map sang tên cột snake_case trong MySQL
                entity.Property(u => u.UserId).HasColumnName("user_id");
                entity.Property(u => u.Username).HasColumnName("username");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.LastLogin).HasColumnName("last_login");

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // ── FriendRelation ──────────────────────────────────────────────
            modelBuilder.Entity<GameServerApi.Models.Entities.FriendRelation>(entity =>
            {
                entity.HasIndex(r => new { r.UserId, r.FriendId }).IsUnique();
                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Friend)
                      .WithMany()
                      .HasForeignKey(r => r.FriendId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlayerData>(entity =>
            {
                entity.ToTable("player_data");
                entity.HasKey(p => p.PlayerId);

                entity.Property(p => p.PlayerId).HasColumnName("player_id");
                entity.Property(p => p.CharacterName).HasColumnName("character_name");
                entity.Property(p => p.Gender).HasColumnName("gender");

                // Single JSON column for all character stats
                entity.Property(p => p.InfoCharJson).HasColumnName("info_char");

                entity.Property(p => p.EquipmentJson).HasColumnName("equipment");
                entity.Property(p => p.InventoryJson).HasColumnName("inventory");
                entity.Property(p => p.SkillsJson).HasColumnName("skills");
                entity.Property(p => p.PotentialStatsJson).HasColumnName("potential_stats");
                entity.Property(p => p.ActiveBuffsJson).HasColumnName("active_buffs");

                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<ExpRequirement>(entity =>
            {
                entity.ToTable("exp_requirements");
                entity.HasKey(e => e.Level);

                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.ExpRequired).HasColumnName("exp_required");
                entity.Property(e => e.BaseStatIncreaseJson).HasColumnName("base_stat_increase");
                entity.Property(e => e.SkillPoints).HasColumnName("skill_points_reward");
                entity.Property(e => e.PotentialPoints).HasColumnName("potential_points_reward");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<MapConfig>(entity =>
            {
                entity.ToTable("map_config");
                entity.HasKey(m => m.MapId);

                entity.Property(m => m.MapId).HasColumnName("map_id");
                entity.Property(m => m.MapName).HasColumnName("map_name");
                entity.Property(m => m.SceneName).HasColumnName("scene_name").HasMaxLength(100);
                entity.Property(m => m.SpawnPointsJson).HasColumnName("spawn_points_json");
                entity.Property(m => m.MinLevel).HasColumnName("min_level");
                entity.Property(m => m.MaxLevel).HasColumnName("max_level");
                entity.Property(m => m.CreatedAt).HasColumnName("created_at");
                entity.Property(m => m.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Enemy>(entity =>
            {
                entity.ToTable("enemy");
                entity.HasKey(e => e.EnemyId);

                entity.Property(e => e.EnemyId).HasColumnName("enemy_id");
                entity.Property(e => e.EnemyName).HasColumnName("enemy_name");
                entity.Property(e => e.EnemyDescription).HasColumnName("enemy_description");
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.BaseHp).HasColumnName("base_hp");
                entity.Property(e => e.BaseMp).HasColumnName("base_mp");
                entity.Property(e => e.BaseDamage).HasColumnName("base_damage");
                entity.Property(e => e.BaseDefense).HasColumnName("base_defense");
                entity.Property(e => e.MoveSpeed).HasColumnName("move_speed");
                entity.Property(e => e.AttackSpeed).HasColumnName("attack_speed");
                entity.Property(e => e.ExpReward).HasColumnName("exp_reward");
                entity.Property(e => e.GoldReward).HasColumnName("gold_reward");
                entity.Property(e => e.SilverReward).HasColumnName("silver_reward");
                entity.Property(e => e.DropItemsJson).HasColumnName("drop_items_json");
                entity.Property(e => e.ElementType).HasColumnName("element_type");
                entity.Property(e => e.EnemyType).HasColumnName("enemy_type");
                entity.Property(e => e.SkillsJson).HasColumnName("skills_json");
                entity.Property(e => e.KhangHoa).HasColumnName("khang_hoa");
                entity.Property(e => e.KhangThuy).HasColumnName("khang_thuy");
                entity.Property(e => e.KhangTho).HasColumnName("khang_tho");
                entity.Property(e => e.KhangMoc).HasColumnName("khang_moc");
                entity.Property(e => e.KhangKim).HasColumnName("khang_kim");
                entity.Property(e => e.KhangPhong).HasColumnName("khang_phong");
                entity.Property(e => e.TangDameHoa).HasColumnName("tang_dame_hoa");
                entity.Property(e => e.TangDameThuy).HasColumnName("tang_dame_thuy");
                entity.Property(e => e.TangDameTho).HasColumnName("tang_dame_tho");
                entity.Property(e => e.TangDameMoc).HasColumnName("tang_dame_moc");
                entity.Property(e => e.TangDameKim).HasColumnName("tang_dame_kim");
                entity.Property(e => e.TangDamePhong).HasColumnName("tang_dame_phong");
                entity.Property(e => e.HpRegenPerSec).HasColumnName("hp_regen_per_sec");
                entity.Property(e => e.EvasionRate).HasColumnName("evasion_rate");
                entity.Property(e => e.CounterRate).HasColumnName("counter_rate");
                entity.Property(e => e.PhasesJson).HasColumnName("phases_json");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<EnemySpawn>(entity =>
            {
                entity.ToTable("enemy_spawns");
                entity.HasKey(e => e.SpawnId);

                entity.Property(e => e.SpawnId).HasColumnName("spawn_id");
                entity.Property(e => e.MapId).HasColumnName("map_id");
                entity.Property(e => e.EnemyTypeId).HasColumnName("enemy_type_id");
                entity.Property(e => e.SpawnX).HasColumnName("spawn_x");
                entity.Property(e => e.SpawnY).HasColumnName("spawn_y");
                entity.Property(e => e.MaxSpawnCount).HasColumnName("max_spawn_count");
                entity.Property(e => e.RespawnTime).HasColumnName("respawn_time");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                
                // Foreign keys
                entity.HasOne(e => e.Enemy)
                    .WithMany()
                    .HasForeignKey(e => e.EnemyTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemTemplate>(entity =>
            {
                entity.ToTable("item_template");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Id).HasColumnName("id");
                entity.Property(i => i.Name).HasColumnName("name");
                entity.Property(i => i.Detail).HasColumnName("detail");
                entity.Property(i => i.IsXepChong).HasColumnName("isXepChong");
                entity.Property(i => i.GioiTinh).HasColumnName("gioiTinh");
                entity.Property(i => i.Type).HasColumnName("type");
                entity.Property(i => i.IdClass).HasColumnName("idClass");
                entity.Property(i => i.IdIcon).HasColumnName("idIcon");
                entity.Property(i => i.LevelNeed).HasColumnName("levelNeed");
                entity.Property(i => i.TaiPhuNeed).HasColumnName("taiPhuNeed");
                entity.Property(i => i.IdMob).HasColumnName("idMob");
                entity.Property(i => i.IdChar).HasColumnName("idChar");
            });

            modelBuilder.Entity<SkillTemplate>(entity =>
            {
                entity.ToTable("skill_template");
                entity.HasKey(s => s.SkillId);

                entity.Property(s => s.SkillId).HasColumnName("skill_id");
                entity.Property(s => s.SkillCode).HasColumnName("skill_code");
                entity.Property(s => s.SkillName).HasColumnName("skill_name");
                entity.Property(s => s.Description).HasColumnName("description");
                entity.Property(s => s.ElementType).HasColumnName("element_type");
                entity.Property(s => s.MaxLevel).HasColumnName("max_level");
                entity.Property(s => s.LevelToUnlock).HasColumnName("level_to_unlock");
                entity.Property(s => s.GeneTierRequired).HasColumnName("gene_tier_required");
                entity.Property(s => s.HybridId).HasColumnName("hybrid_id");
                entity.Property(s => s.LevelsJson).HasColumnName("levels_json");
                entity.Property(s => s.IconId).HasColumnName("icon_id");
                entity.Property(s => s.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(s => s.SkillCode).IsUnique();
            });

            modelBuilder.Entity<GeneUpgradeConfig>(entity =>
            {
                entity.ToTable("gene_upgrade_config");
                entity.HasKey(e => new { e.TierFrom, e.ElementType });

                entity.Property(e => e.TierFrom).HasColumnName("tier_from");
                entity.Property(e => e.ElementType).HasColumnName("element_type");
                entity.Property(e => e.GeneExpRequired).HasColumnName("gene_exp_required");
                entity.Property(e => e.GoldCost).HasColumnName("silver_cost");
                entity.Property(e => e.ItemId).HasColumnName("stone_id");
                entity.Property(e => e.ItemsNeeded).HasColumnName("stone_needed");
                entity.Property(e => e.ItemsMin).HasColumnName("stone_min");
                entity.Property(e => e.BaseSuccessRate).HasColumnName("base_success_rate");
            });

            modelBuilder.Entity<GeneTierStatConfig>(entity =>
            {
                entity.ToTable("gene_tier_stat_config");
                entity.HasKey(e => new { e.ElementType, e.TierTo });

                entity.Property(e => e.ElementType).HasColumnName("element_type").HasMaxLength(10);
                entity.Property(e => e.TierTo).HasColumnName("tier_to");
                entity.Property(e => e.HpBonus).HasColumnName("hp_bonus");
                entity.Property(e => e.MpBonus).HasColumnName("mp_bonus");
                entity.Property(e => e.AttackBonus).HasColumnName("attack_bonus");
                entity.Property(e => e.DefenseBonus).HasColumnName("defense_bonus");
            });

            modelBuilder.Entity<DungeonConfig>(entity =>
            {
                entity.ToTable("dungeon_config");
                entity.HasKey(d => d.DungeonId);

                entity.Property(d => d.DungeonId).HasColumnName("dungeon_id");
                entity.Property(d => d.DungeonName).HasColumnName("dungeon_name").HasMaxLength(100);
                entity.Property(d => d.DungeonType).HasColumnName("dungeon_type").HasMaxLength(10);
                entity.Property(d => d.MapId).HasColumnName("map_id");
                entity.Property(d => d.SceneName).HasColumnName("scene_name").HasMaxLength(100);
                entity.Property(d => d.MaxPlayers).HasColumnName("max_players");
                entity.Property(d => d.MinLevelRequired).HasColumnName("min_level_required");
                entity.Property(d => d.TimeLimitSeconds).HasColumnName("time_limit_seconds");
                entity.Property(d => d.Description).HasColumnName("description");
                entity.Property(d => d.BossEnemyId).HasColumnName("boss_enemy_id");
                entity.Property(d => d.RewardJson).HasColumnName("reward_json");
                entity.Property(d => d.ThumbnailIconId).HasColumnName("thumbnail_icon_id").HasMaxLength(50);
                entity.Property(d => d.IsActive).HasColumnName("is_active");
                entity.Property(d => d.CreatedAt).HasColumnName("created_at");
                entity.Property(d => d.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(d => d.Map)
                    .WithMany()
                    .HasForeignKey(d => d.MapId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.BossEnemy)
                    .WithMany()
                    .HasForeignKey(d => d.BossEnemyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DungeonSession>(entity =>
            {
                entity.ToTable("dungeon_session");
                entity.HasKey(s => s.SessionId);

                entity.Property(s => s.SessionId).HasColumnName("session_id");
                entity.Property(s => s.DungeonConfigId).HasColumnName("dungeon_config_id");
                entity.Property(s => s.HostIp).HasColumnName("host_ip").HasMaxLength(45);
                entity.Property(s => s.HostPort).HasColumnName("host_port");
                entity.Property(s => s.CurrentPlayers).HasColumnName("current_players");
                entity.Property(s => s.MaxPlayers).HasColumnName("max_players");
                entity.Property(s => s.Status).HasColumnName("status").HasMaxLength(10);
                entity.Property(s => s.CreatedAt).HasColumnName("created_at");
                entity.Property(s => s.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(s => s.DungeonConfig)
                    .WithMany()
                    .HasForeignKey(s => s.DungeonConfigId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GeneMultiConfig>(entity =>
            {
                entity.ToTable("gene_multi_config");
                entity.HasKey(e => new { e.TierFrom, e.ElementType });

                entity.Property(e => e.TierFrom).HasColumnName("tier_from");
                entity.Property(e => e.ElementType).HasColumnName("element_type").HasMaxLength(10);
                entity.Property(e => e.GeneExpRequired).HasColumnName("gene_exp_required");
                entity.Property(e => e.GoldCost).HasColumnName("silver_cost");
                entity.Property(e => e.ItemId).HasColumnName("stone_id");
                entity.Property(e => e.ItemsNeeded).HasColumnName("stone_needed");
                entity.Property(e => e.ItemsMin).HasColumnName("stone_min");
                entity.Property(e => e.BaseSuccessRate).HasColumnName("base_success_rate");
            });

            modelBuilder.Entity<GeneHybridConfig>(entity =>
            {
                entity.ToTable("gene_hybrid_config");
                entity.HasKey(e => e.HybridId);

                entity.Property(e => e.HybridId).HasColumnName("hybrid_id").ValueGeneratedOnAdd();
                entity.Property(e => e.ElementA).HasColumnName("element_a").HasMaxLength(10);
                entity.Property(e => e.ElementB).HasColumnName("element_b").HasMaxLength(10);
                entity.Property(e => e.HybridName).HasColumnName("hybrid_name").HasMaxLength(100);
                entity.Property(e => e.HybridDescription).HasColumnName("hybrid_description").HasMaxLength(500);
                entity.Property(e => e.BonusTargetElements).HasColumnName("bonus_target_elements").HasMaxLength(100);
                entity.Property(e => e.ImmuneElements).HasColumnName("immune_elements").HasMaxLength(100);
                entity.Property(e => e.FusionGoldCost).HasColumnName("fusion_silver_cost");
                entity.Property(e => e.FusionItemId).HasColumnName("fusion_item_id");
                entity.Property(e => e.FusionItemCount).HasColumnName("fusion_item_count");
                entity.Property(e => e.AtkBonusPercent).HasColumnName("atk_bonus_percent");
                entity.Property(e => e.StatBonusHp).HasColumnName("stat_bonus_hp");
                entity.Property(e => e.StatBonusMp).HasColumnName("stat_bonus_mp");
                entity.Property(e => e.StatBonusAtk).HasColumnName("stat_bonus_atk");
                entity.Property(e => e.StatBonusDef).HasColumnName("stat_bonus_def");

                entity.HasIndex(e => new { e.ElementA, e.ElementB }).IsUnique();
            });

            modelBuilder.Entity<NpcConfig>(entity =>
            {
                entity.ToTable("npc_config");
                entity.HasKey(n => n.NpcId);
                entity.Property(n => n.NpcId).HasColumnName("npc_id");
                entity.Property(n => n.NpcName).HasColumnName("npc_name").HasMaxLength(100);
                entity.Property(n => n.NpcType).HasColumnName("npc_type").HasMaxLength(20);
                entity.Property(n => n.MapId).HasColumnName("map_id");
                entity.Property(n => n.PosX).HasColumnName("pos_x");
                entity.Property(n => n.PosY).HasColumnName("pos_y");
                entity.Property(n => n.DialogueKey).HasColumnName("dialogue_key").HasMaxLength(50);
                entity.Property(n => n.IconId).HasColumnName("icon_id").HasMaxLength(50);
                entity.Property(n => n.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<NpcShopItem>(entity =>
            {
                entity.ToTable("npc_shop_item");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Id).HasColumnName("id");
                entity.Property(n => n.NpcId).HasColumnName("npc_id");
                entity.Property(n => n.ItemTemplateId).HasColumnName("item_template_id");
                entity.Property(n => n.PriceSilver).HasColumnName("price_silver");
                entity.Property(n => n.PriceGold).HasColumnName("price_gold");
                entity.Property(n => n.Stock).HasColumnName("stock");
                entity.Property(n => n.RequiredLevel).HasColumnName("required_level");
            });

            modelBuilder.Entity<NpcDialogue>(entity =>
            {
                entity.ToTable("npc_dialogue");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Id).HasColumnName("id");
                entity.Property(n => n.NpcId).HasColumnName("npc_id");
                entity.Property(n => n.DialogueKey).HasColumnName("dialogue_key").HasMaxLength(50);
                entity.Property(n => n.TextVi).HasColumnName("text_vi").HasMaxLength(1000);
                entity.Property(n => n.NextKey).HasColumnName("next_key").HasMaxLength(50);
                entity.Property(n => n.ActionType).HasColumnName("action_type").HasMaxLength(20);
                entity.HasIndex(n => new { n.NpcId, n.DialogueKey }).IsUnique();
            });

            modelBuilder.Entity<MapPortal>(entity =>
            {
                entity.ToTable("map_portal");
                entity.HasKey(p => p.PortalId);
                entity.Property(p => p.PortalId).HasColumnName("portal_id").ValueGeneratedOnAdd();
                entity.Property(p => p.PortalName).HasColumnName("portal_name").HasMaxLength(100);
                entity.Property(p => p.SourceMapId).HasColumnName("source_map_id");
                entity.Property(p => p.SrcX).HasColumnName("src_x");
                entity.Property(p => p.SrcY).HasColumnName("src_y");
                entity.Property(p => p.SrcRadius).HasColumnName("src_radius");
                entity.Property(p => p.DestMapId).HasColumnName("dest_map_id");
                entity.Property(p => p.DestSceneName).HasColumnName("dest_scene_name").HasMaxLength(100);
                entity.Property(p => p.DestX).HasColumnName("dest_x");
                entity.Property(p => p.DestY).HasColumnName("dest_y");
                entity.Property(p => p.PortalType).HasColumnName("portal_type").HasMaxLength(30);
                entity.Property(p => p.PortalDirection).HasColumnName("portal_direction").HasMaxLength(10);
                entity.Property(p => p.RequiredItemId).HasColumnName("required_item_id");
                entity.Property(p => p.DungeonId).HasColumnName("dungeon_id");
                entity.Property(p => p.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<BossConfig>(entity =>
            {
                entity.ToTable("boss_config");
                entity.HasKey(b => b.BossId);
                entity.Property(b => b.BossId).HasColumnName("boss_id");
                entity.Property(b => b.MapId).HasColumnName("map_id");
                entity.Property(b => b.SpawnX).HasColumnName("spawn_x");
                entity.Property(b => b.SpawnY).HasColumnName("spawn_y");
                entity.Property(b => b.MinSpawnHour).HasColumnName("min_spawn_hour");
                entity.Property(b => b.MaxSpawnHour).HasColumnName("max_spawn_hour");
                entity.Property(b => b.RespawnMinutes).HasColumnName("respawn_minutes");
                entity.Property(b => b.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<OptionTemplate>(entity =>
            {
                entity.ToTable("option_template");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).HasColumnName("id");
                entity.Property(o => o.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(o => o.Type).HasColumnName("type");
                entity.Property(o => o.Level).HasColumnName("level");
                entity.Property(o => o.StrOption).HasColumnName("strOption");
            });

            modelBuilder.Entity<MapEnemyDrop>(entity =>
            {
                entity.ToTable("map_enemy_drop");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id).HasColumnName("id");
                entity.Property(d => d.MapId).HasColumnName("map_id");
                entity.Property(d => d.EnemyId).HasColumnName("enemy_id");
                entity.Property(d => d.ItemId).HasColumnName("item_id");
                entity.Property(d => d.DropChance).HasColumnName("drop_chance");
                entity.Property(d => d.QtyMin).HasColumnName("qty_min");
                entity.Property(d => d.QtyMax).HasColumnName("qty_max");
                entity.Property(d => d.IsActive).HasColumnName("is_active");
            });

            // map_zone_config không còn là nguồn dữ liệu chính.
            // Zone thường được Unity server tự sinh theo MapWorldConfig,
            // còn zone riêng/phó bản tồn tại runtime trong memory.

            // ── Normalized player data tables ──────────────────────────────
            modelBuilder.Entity<PlayerEquipment>(entity =>
            {
                entity.ToTable("player_equipment");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.PlayerId).HasColumnName("player_id");
                entity.Property(e => e.Slot).HasColumnName("slot");
                entity.Property(e => e.ItemTemplateId).HasColumnName("item_template_id");
                entity.Property(e => e.UpgradeLevel).HasColumnName("upgrade_level");
                entity.Property(e => e.StrOptions).HasColumnName("str_options");
                entity.Property(e => e.EquippedAt).HasColumnName("equipped_at");

                entity.HasIndex(e => new { e.PlayerId, e.Slot }).IsUnique();
                entity.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlayerInventory>(entity =>
            {
                entity.ToTable("player_inventory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.PlayerId).HasColumnName("player_id");
                entity.Property(e => e.ItemTemplateId).HasColumnName("item_template_id");
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.SlotIndex).HasColumnName("slot_index");
                entity.Property(e => e.UpgradeLevel).HasColumnName("upgrade_level");
                entity.Property(e => e.StrOptions).HasColumnName("str_options");
                entity.Property(e => e.IsLocked).HasColumnName("is_locked");
                entity.Property(e => e.AcquiredAt).HasColumnName("acquired_at");

                entity.HasIndex(e => e.PlayerId);
                entity.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlayerSkillRecord>(entity =>
            {
                entity.ToTable("player_skill_record");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.PlayerId).HasColumnName("player_id");
                entity.Property(e => e.SkillId).HasColumnName("skill_id");
                entity.Property(e => e.SkillLevel).HasColumnName("skill_level");
                entity.Property(e => e.IsEquipped).HasColumnName("is_equipped");
                entity.Property(e => e.HotbarSlot).HasColumnName("hotbar_slot");

                entity.HasIndex(e => new { e.PlayerId, e.SkillId }).IsUnique();
                entity.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlayerActionLog>(entity =>
            {
                entity.ToTable("player_action_log");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.PlayerId).HasColumnName("player_id");
                entity.Property(e => e.ActionType).HasColumnName("action_type");
                entity.Property(e => e.DetailJson).HasColumnName("detail_json");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(e => e.PlayerId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemEffectTemplate>(entity =>
            {
                entity.ToTable("item_effect_template");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ItemTemplateId).HasColumnName("item_template_id");
                entity.Property(e => e.EffectType).HasColumnName("effect_type");
                entity.Property(e => e.Value).HasColumnName("value");
                entity.Property(e => e.DurationSec).HasColumnName("duration_sec");
                entity.Property(e => e.IconId).HasColumnName("icon_id");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Detail).HasColumnName("detail");
                entity.Property(e => e.SortOrder).HasColumnName("sort_order");
                entity.HasIndex(e => e.ItemTemplateId);
            });

            // ── Dungeon Wave Entry (giới hạn lượt vào hàng ngày) ──────────────
            modelBuilder.Entity<GameServerApi.Models.Entities.DungeonWaveEntry>(entity =>
            {
                entity.ToTable("dungeon_wave_entry");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.PlayerId).HasColumnName("character_id");
                entity.Property(e => e.DungeonId).HasColumnName("dungeon_id");
                entity.Property(e => e.EntryDate).HasColumnName("entry_date");
                entity.Property(e => e.EntriesUsed).HasColumnName("entries_used");
                entity.Property(e => e.EntriesLimit).HasColumnName("entries_limit");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(e => new { e.PlayerId, e.DungeonId, e.EntryDate }).IsUnique()
                      .HasDatabaseName("uq_player_dungeon_date");
            });

            // ── Dungeon Wave Session (reconnect / timeout state) ──────────────
            modelBuilder.Entity<GameServerApi.Models.Entities.DungeonWaveSession>(entity =>
            {
                entity.ToTable("dungeon_wave_session");
                entity.HasKey(e => e.SessionId);
                entity.Property(e => e.SessionId).HasColumnName("session_id").ValueGeneratedOnAdd();
                entity.Property(e => e.PlayerId).HasColumnName("character_id");
                entity.Property(e => e.DungeonId).HasColumnName("dungeon_id");
                entity.Property(e => e.CurrentWave).HasColumnName("current_wave");
                entity.Property(e => e.CurrentPhase).HasColumnName("current_phase").HasMaxLength(10);
                entity.Property(e => e.SessionStartedAt).HasColumnName("session_started_at");
                entity.Property(e => e.WaveStartedAt).HasColumnName("wave_started_at");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.ExitReason).HasColumnName("exit_reason").HasMaxLength(20);
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(e => e.PlayerId);
            });
        }
    }
}

