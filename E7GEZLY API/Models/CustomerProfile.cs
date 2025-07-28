using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    public class CustomerProfile : BaseSyncEntity
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        public DateTime? DateOfBirth { get; set; }

        // Address fields
        public int? DistrictId { get; set; }
        public District? District { get; set; }

        // Keep the string address
        [StringLength(500)]
        public string? StreetAddress { get; set; }

        // Keep coordinates
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(200)]
        public string? Landmark { get; set; }

        // Computed property for full address
        public string? FullAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(StreetAddress)) parts.Add(StreetAddress);
                if (District != null)
                {
                    parts.Add(District.NameEn); // or NameAr based on culture
                    parts.Add(District.Governorate.NameEn);
                }
                return parts.Any() ? string.Join(", ", parts) : null;
            }
        }
    }
}