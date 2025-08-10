// Models/VenueWorkingHours.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// Represents working hours for a venue
    /// </summary>
    public class VenueWorkingHours : BaseSyncEntity
    {
        [Required]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan OpenTime { get; set; }

        [Required]
        public TimeSpan CloseTime { get; set; }

        public bool IsClosed { get; set; } = false;

        // For courts: morning/evening hours
        public TimeSpan? MorningStartTime { get; set; }
        public TimeSpan? MorningEndTime { get; set; }
        public TimeSpan? EveningStartTime { get; set; }
        public TimeSpan? EveningEndTime { get; set; }
        
        // Status field for handler compatibility
        public bool IsActive { get; set; } = true;
    }
}