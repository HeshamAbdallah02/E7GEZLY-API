// Controllers/Venue/VenueBookingController.cs (EXAMPLE)
using E7GEZLY_API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/venue/bookings")]
[Authorize(Roles = "VenueAdmin")]
[RequireCompleteProfile] // This ensures only venues with complete profiles can access
public class VenueBookingController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBookings()
    {
        // This endpoint is only accessible to venues with complete profiles
        return Ok(new { message = "Bookings retrieved" });
    }
}