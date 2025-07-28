using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    public class ExternalLogin : BaseSyncEntity
    {
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = null!; // Facebook, Google, Apple

        [Required]
        [StringLength(255)]
        public string ProviderUserId { get; set; } = null!;

        [StringLength(500)]
        public string? ProviderEmail { get; set; }

        [StringLength(200)]
        public string? ProviderDisplayName { get; set; }

        public DateTime? LastLoginAt { get; set; }
    }
}