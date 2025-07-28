// Exceptions/GeocodingException.cs
namespace E7GEZLY_API.Exceptions
{
    public class GeocodingException : Exception
    {
        public GeocodingErrorType ErrorType { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }

        public GeocodingException(string message, GeocodingErrorType errorType, double? latitude = null, double? longitude = null)
            : base(message)
        {
            ErrorType = errorType;
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public enum GeocodingErrorType
    {
        ServiceUnavailable,
        RateLimitExceeded,
        InvalidCoordinates,
        NoDistrictFound,
        NetworkError
    }
}