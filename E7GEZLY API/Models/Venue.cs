// Models/Venue.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    public class Venue : BaseSyncEntity
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = null!;
        
        [Required]
        public VenueType VenueType { get; set; }
        
        public VenueFeatures Features { get; set; }
        
        // Business Details (optional for initial registration, can be added later)
        /*[StringLength(200)]
        public string? BusinessName { get; set; }
        
        [StringLength(50)]
        public string? TaxRegistrationNumber { get; set; }
        
        [StringLength(50)]
        public string? CommercialRegistrationNumber { get; set; }*/

        // Required Location fields
        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }
        
        public int? DistrictId { get; set; }
        public District? District { get; set; }
        
        [StringLength(500)]
        public string? StreetAddress { get; set; }
        
        [StringLength(200)]
        public string? Landmark { get; set; }

        // Computed property for full address
        public string FullAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(StreetAddress)) parts.Add(StreetAddress);
                if (District != null)
                {
                    parts.Add(District.NameEn);
                    parts.Add(District.Governorate.NameEn);
                }
                return string.Join(", ", parts);
            }
        }

        // Status
        /*public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedByUserId { get; set; }*/

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public bool IsProfileComplete { get; set; } = false;

        // Profile completion related
        public virtual ICollection<VenueWorkingHours> WorkingHours { get; set; } = new List<VenueWorkingHours>();
        public virtual ICollection<VenuePricing> Pricing { get; set; } = new List<VenuePricing>();
        public virtual ICollection<VenueImage> Images { get; set; } = new List<VenueImage>();
        public virtual VenuePlayStationDetails? PlayStationDetails { get; set; }
    }

    public enum VenueType
    {
        PlayStationVenue = 0,
        FootballCourt = 1,
        PadelCourt = 2,
        MultiPurpose = 99
    }

    [Flags]
    public enum VenueFeatures
    {
        // features would go here
    }
}