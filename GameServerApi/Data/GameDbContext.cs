using GameServerApi.Models;
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
                entity.Property(m => m.SpawnPointsJson).HasColumnName("spawn_points_json");
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
                entity.Property(e => e.DropItemsJson).HasColumnName("drop_items_json");
                entity.Property(e => e.ElementType).HasColumnName("element_type");
                entity.Property(e => e.EnemyType).HasColumnName("enemy_type");
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
        }
    }
}

