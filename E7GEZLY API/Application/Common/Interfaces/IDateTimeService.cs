namespace E7GEZLY_API.Application.Common.Interfaces
{
    /// <summary>
    /// Date time service interface for testability
    /// </summary>
    public interface IDateTimeService
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }
}