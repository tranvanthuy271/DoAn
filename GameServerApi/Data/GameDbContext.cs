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
        }
    }
}

