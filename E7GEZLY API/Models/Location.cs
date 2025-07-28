namespace E7GEZLY_API.Models
{
    public class Governorate
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public ICollection<District> Districts { get; set; } = new List<District>();
    }

    public class District
    {
        public int Id { get; set; }
        public required string NameEn { get; set; }
        public required string NameAr { get; set; }
        public int GovernorateId { get; set; }
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }

        // Navigation property
        public virtual Governorate Governorate { get; set; } = null!;
        public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();
        public virtual ICollection<CustomerProfile> CustomerProfiles { get; set; } = new List<CustomerProfile>();
    }
}