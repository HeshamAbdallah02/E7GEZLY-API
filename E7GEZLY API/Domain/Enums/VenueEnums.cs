namespace E7GEZLY_API.Domain.Enums;

/// <summary>
/// Types of venues supported by the E7GEZLY platform
/// </summary>
public enum VenueType
{
    PlayStationVenue = 0,
    FootballCourt = 1,
    PadelCourt = 2,
    MultiPurpose = 99
}

/// <summary>
/// Features available at venues (flags enum for multiple features)
/// </summary>
[Flags]
public enum VenueFeatures : long
{
    None = 0,
    // Basic amenities
    WiFi = 1 << 0,              // 1
    Parking = 1 << 1,           // 2
    AirConditioning = 1 << 2,   // 4
    Restrooms = 1 << 3,         // 8
    
    // Food & Beverage
    Cafe = 1 << 4,              // 16
    Snacks = 1 << 5,            // 32
    Beverages = 1 << 6,         // 64
    
    // Entertainment
    TVScreens = 1 << 7,         // 128
    SportsChannel = 1 << 8,     // 256
    SoundSystem = 1 << 9,       // 512
    
    // PlayStation specific
    PS4Available = 1 << 10,     // 1024
    PS5Available = 1 << 11,     // 2048
    VIPRooms = 1 << 12,         // 4096
    PrivateRooms = 1 << 13,     // 8192
    
    // Court specific
    IndoorCourt = 1 << 14,      // 16384
    OutdoorCourt = 1 << 15,     // 32768
    LightingSystem = 1 << 16,   // 65536
    
    // Services
    OnlineBooking = 1 << 17,    // 131072
    PhoneBooking = 1 << 18,     // 262144
    Coaching = 1 << 19,         // 524288
    Equipment = 1 << 20         // 1048576
}

/// <summary>
/// Roles for venue sub-users
/// </summary>
public enum VenueSubUserRole
{
    Admin = 0,      // Full access except deleting venue
    Coworker = 1,   // Limited access based on permissions
    Operator = 2,   // Standard operational access
    Staff = 3       // Basic staff access
}

/// <summary>
/// Permissions for venue sub-users (flags enum for granular control)
/// </summary>
[Flags]
public enum VenuePermissions : long
{
    None = 0,

    // Venue Management
    ViewVenueDetails = 1L << 0,        // 1
    EditVenueDetails = 1L << 1,        // 2
    ManagePricing = 1L << 2,           // 4
    ManageWorkingHours = 1L << 3,      // 8
    ManageVenueImages = 1L << 4,       // 16

    // Sub-User Management
    ViewSubUsers = 1L << 5,            // 32
    CreateSubUsers = 1L << 6,          // 64
    EditSubUsers = 1L << 7,            // 128
    DeleteSubUsers = 1L << 8,          // 256
    ResetSubUserPasswords = 1L << 9,   // 512

    // Booking Management
    ViewBookings = 1L << 10,           // 1024
    CreateBookings = 1L << 11,         // 2048
    EditBookings = 1L << 12,           // 4096
    CancelBookings = 1L << 13,         // 8192

    // Customer Management
    ViewCustomers = 1L << 14,          // 16384
    ManageCustomers = 1L << 15,        // 32768

    // Financial
    ViewFinancials = 1L << 16,         // 65536
    ManageFinancials = 1L << 17,       // 131072
    ProcessRefunds = 1L << 18,         // 262144

    // Reporting
    ViewReports = 1L << 19,            // 524288
    ExportReports = 1L << 20,          // 1048576

    // Tracking
    ViewAuditLogs = 1L << 21,          // 2097152
    ViewCoworkerActivity = 1L << 22,   // 4194304

    // Default permission sets
    AdminPermissions = ~None,          // All permissions
    CoworkerPermissions = ViewVenueDetails | ViewBookings | CreateBookings |
                         EditBookings | ViewCustomers | ViewReports,
    OperatorPermissions = ViewVenueDetails | ViewBookings | CreateBookings |
                         EditBookings | CancelBookings | ViewCustomers | ManageCustomers |
                         ViewReports,
    StaffPermissions = ViewVenueDetails | ViewBookings | CreateBookings | ViewCustomers
}

/// <summary>
/// Types of pricing structures
/// </summary>
public enum PricingType
{
    // Court pricing
    MorningHour = 0,
    EveningHour = 1,

    // PlayStation pricing
    PlayStation = 10
}

/// <summary>
/// PlayStation models supported
/// </summary>
public enum PlayStationModel
{
    PS4 = 4,
    PS5 = 5
}

/// <summary>
/// Room types for PlayStation venues
/// </summary>
public enum RoomType
{
    Classic = 0,
    VIP = 1
}

/// <summary>
/// Game modes for PlayStation venues
/// </summary>
public enum GameMode
{
    Single = 0,
    Multiplayer = 1
}

/// <summary>
/// Time slot types for courts
/// </summary>
public enum TimeSlotType
{
    Morning = 0,
    Evening = 1
}