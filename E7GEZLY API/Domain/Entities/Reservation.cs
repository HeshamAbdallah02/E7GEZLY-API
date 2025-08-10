using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity representing a reservation/booking at a venue
/// Currently simplified - will be expanded in future iterations
/// </summary>
public sealed class Reservation : BaseEntity
{
    private Reservation(string roomName, Guid venueId, string customerId) : base()
    {
        RoomName = roomName;
        VenueId = venueId;
        CustomerId = customerId;
    }

    public static Reservation Create(string roomName, Guid venueId, string customerId)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            throw new BusinessRuleViolationException("Room name is required for reservation");

        if (venueId == Guid.Empty)
            throw new BusinessRuleViolationException("Venue ID is required for reservation");

        if (string.IsNullOrWhiteSpace(customerId))
            throw new BusinessRuleViolationException("Customer ID is required for reservation");

        return new Reservation(roomName, venueId, customerId);
    }

    public string RoomName { get; private set; }
    public Guid VenueId { get; private set; }
    public string CustomerId { get; private set; }

    // TODO: Add reservation-specific properties in future iterations:
    // - DateTime ReservationDate
    // - TimeSpan Duration  
    // - ReservationStatus Status
    // - Decimal TotalAmount
    // - Decimal DepositAmount
    // - PaymentStatus PaymentStatus
    // - DateTime? CheckInTime
    // - DateTime? CheckOutTime
    // - String? SpecialRequests
    // - CancellationReason? CancellationReason

    public void UpdateRoomName(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            throw new BusinessRuleViolationException("Room name is required for reservation");

        RoomName = roomName;
        MarkAsUpdated();
    }

    public override string ToString()
    {
        return $"Reservation: {RoomName} for Customer {CustomerId}";
    }
}