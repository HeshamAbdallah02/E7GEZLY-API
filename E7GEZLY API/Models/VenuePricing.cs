// Models/VenuePricing.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// Represents pricing structure for venues
    /// </summary>
    public class VenuePricing : BaseSyncEntity
    {
        [Required]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        [Required]
        public PricingType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        // For PlayStation venues
        public PlayStationModel? PlayStationModel { get; set; }
        public RoomType? RoomType { get; set; }
        public GameMode? GameMode { get; set; }

        // For courts
        public TimeSlotType? TimeSlotType { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DepositPercentage { get; set; }
        
        // Handler compatibility properties
        public bool IsActive { get; set; } = true;
        public string Name => GenerateName();
        public decimal PricePerHour => Price;
        
        private string GenerateName()
        {
            var nameParts = new List<string> { Type.ToString() };
            
            if (PlayStationModel.HasValue) nameParts.Add($"PS{(int)PlayStationModel.Value}");
            if (RoomType.HasValue) nameParts.Add(RoomType.Value.ToString());
            if (GameMode.HasValue) nameParts.Add(GameMode.Value.ToString());
            if (TimeSlotType.HasValue) nameParts.Add(TimeSlotType.Value.ToString());
            
            return string.Join(" - ", nameParts);
        }
    }
}
