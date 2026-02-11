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

                // Map sang snake_case cho MySQL
                entity.Property(p => p.PlayerId).HasColumnName("player_id");
                entity.Property(p => p.Level).HasColumnName("level");
                entity.Property(p => p.Experience).HasColumnName("experience");
                entity.Property(p => p.Gold).HasColumnName("gold");
                entity.Property(p => p.MapId).HasColumnName("map_id");
                
                entity.Property(p => p.PositionX).HasColumnName("position_x");
                entity.Property(p => p.PositionY).HasColumnName("position_y");

                entity.Property(p => p.Hp).HasColumnName("hp");
                entity.Property(p => p.MaxHp).HasColumnName("max_hp");
                entity.Property(p => p.Mp).HasColumnName("mp");
                entity.Property(p => p.MaxMp).HasColumnName("max_mp");
                entity.Property(p => p.Attack).HasColumnName("attack");

                entity.Property(p => p.ElementType).HasColumnName("element_type");
                entity.Property(p => p.GeneTier).HasColumnName("gene_tier");
                entity.Property(p => p.IsHybrid).HasColumnName("is_hybrid");
                entity.Property(p => p.SecondaryElement).HasColumnName("secondary_element");
                entity.Property(p => p.Gender).HasColumnName("gender");
                entity.Property(p => p.CharacterName).HasColumnName("character_name");

                entity.Property(p => p.EquipmentJson).HasColumnName("equipment");
                entity.Property(p => p.SkillsJson).HasColumnName("skills");
                entity.Property(p => p.InventoryJson).HasColumnName("inventory");
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
                entity.Property(e => e.SkillPoints).HasColumnName("skill_points");
                entity.Property(e => e.PotentialPoints).HasColumnName("potential_points");
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
        }
    }
}

