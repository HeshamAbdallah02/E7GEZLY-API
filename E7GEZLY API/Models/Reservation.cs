namespace E7GEZLY_API.Models
{
    public class Reservation : BaseSyncEntity
    {
        public string RoomName { get; set; } = null!;
        
        // Add VenueId directly to Reservation
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;
        
        // Add CustomerId for the person making the reservation
        public string CustomerId { get; set; } = null!;
        public virtual ApplicationUser Customer { get; set; } = null!;
        
        // Other reservation properties you'll add later
        // public DateTime ReservationDate { get; set; }
        // public TimeSpan Duration { get; set; }
        // etc.
    }
}