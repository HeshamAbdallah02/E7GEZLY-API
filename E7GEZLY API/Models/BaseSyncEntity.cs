namespace E7GEZLY_API.Models
{
    // Models/BaseSyncEntity.cs
    public abstract class BaseSyncEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}