// Models/VenuePlayStationDetails.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// PlayStation-specific venue details
    /// </summary>
    public class VenuePlayStationDetails : BaseSyncEntity
    {
        [Required]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        public int NumberOfRooms { get; set; }

        public bool HasPS4 { get; set; }
        public bool HasPS5 { get; set; }
        public bool HasVIPRooms { get; set; }

        // Features
        public bool HasCafe { get; set; }
        public bool HasWiFi { get; set; }
        public bool ShowsMatches { get; set; }
    }
}
