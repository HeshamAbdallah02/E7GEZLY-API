using E7GEZLY_API.Application.Common.Interfaces;

namespace E7GEZLY_API.Application.Common.Services
{
    /// <summary>
    /// Default implementation of IDateTimeService
    /// </summary>
    public class DateTimeService : IDateTimeService
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}