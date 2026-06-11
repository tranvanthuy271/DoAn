using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    // Quan hệ bạn bè giữa hai người chơi.
    // Một hàng đại diện cho một chiều của quan hệ:
    // - UserId = người gửi lời mời
    // - FriendId = người nhận lời mời
    // - Status: pending | accepted | blocked
    [Table("friend_relations")]
    public class FriendRelation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("friend_id")]
        public int FriendId { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending | accepted | blocked

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(FriendId))]
        public User? Friend { get; set; }
    }
}
