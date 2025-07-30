namespace E7GEZLY_API.Configuration
{
    /// <summary>
    /// Configuration for distributed caching
    /// </summary>
    public class CacheConfiguration
    {
        public string ConnectionString { get; set; } = "localhost:6379";
        public string InstanceName { get; set; } = "E7GEZLY";
        public int DefaultExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Cache duration settings for different data types
        /// </summary>
        public CacheDurationSettings Durations { get; set; } = new();

        /// <summary>
        /// Enable/disable caching for specific features
        /// </summary>
        public CacheFeatureSettings Features { get; set; } = new();
    }

    public class CacheDurationSettings
    {
        public int LocationDataMinutes { get; set; } = 1440; // 24 hours
        public int VenueDetailsMinutes { get; set; } = 30;
        public int VenueSearchMinutes { get; set; } = 15;
        public int UserSessionMinutes { get; set; } = 240; // 4 hours
        public int RateLimitMinutes { get; set; } = 60;
        public int GeocodingResultDays { get; set; } = 7;
    }

    public class CacheFeatureSettings
    {
        public bool EnableLocationCache { get; set; } = true;
        public bool EnableVenueCache { get; set; } = true;
        public bool EnableSessionCache { get; set; } = true;
        public bool EnableRateLimitCache { get; set; } = true;
        public bool EnableGeocodingCache { get; set; } = true;
    }

    /// <summary>
    /// Standard cache key patterns
    /// </summary>
    public static class CacheKeys
    {
        // Location keys
        public const string AllGovernorates = "location:governorates:all";
        public const string GovernorateById = "location:governorate:{0}";
        public const string DistrictsByGovernorate = "location:districts:gov:{0}";
        public const string DistrictById = "location:district:{0}";

        // Venue keys
        public const string VenueById = "venue:details:{0}";
        public const string VenuesByDistrict = "venue:district:{0}:type:{1}";
        public const string VenueSearch = "venue:search:{0}"; // hash of search params
        public const string VenueAvailability = "venue:availability:{0}:date:{1}";

        // User/Session keys
        public const string UserSession = "session:user:{0}:device:{1}";
        public const string UserRateLimit = "ratelimit:user:{0}:rule:{1}";

        // Geocoding keys
        public const string GeocodingResult = "geocoding:lat:{0}:lng:{1}";

        // Cache tags for invalidation
        public const string VenueTag = "tag:venue:{0}";
        public const string DistrictTag = "tag:district:{0}";
        public const string LocationTag = "tag:location";
    }
}