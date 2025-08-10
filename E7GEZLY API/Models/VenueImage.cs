// Models/VenueImage.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// Represents images uploaded for venues
    /// </summary>
    public class VenueImage : BaseSyncEntity
    {
        [Required]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = null!;

        [StringLength(200)]
        public string? Caption { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;
        
        // Handler compatibility properties
        public bool IsActive { get; set; } = true;
        public string ImageType { get; set; } = "Image";
        public bool IsMainImage => IsPrimary;
    }
}
