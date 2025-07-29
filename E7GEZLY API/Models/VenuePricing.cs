// Models/VenuePricing.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }

    public enum PricingType
    {
        // Court pricing
        MorningHour = 0,
        EveningHour = 1,

        // PlayStation pricing
        PlayStation = 10
    }

    public enum PlayStationModel
    {
        PS4 = 4,
        PS5 = 5
    }

    public enum RoomType
    {
        Classic = 0,
        VIP = 1
    }

    public enum GameMode
    {
        Single = 0,
        Multiplayer = 1
    }

    public enum TimeSlotType
    {
        Morning = 0,
        Evening = 1
    }
}
