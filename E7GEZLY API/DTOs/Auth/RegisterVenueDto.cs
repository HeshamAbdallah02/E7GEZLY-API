// DTOs/Auth/RegisterVenueDto.cs
using System.ComponentModel.DataAnnotations;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.DTOs.Auth
{
    public record RegisterVenueDto
    {
        [Required(ErrorMessage = "Venue name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Venue name must be between 3 and 200 characters")]
        public string VenueName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Venue type is required")]
        public VenueType VenueType { get; init; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^01[0125]\d{8}$", ErrorMessage = "Phone number must be in format 01xxxxxxxxx (11 digits starting with 010, 011, 012, or 015)")]
        public string PhoneNumber { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; init; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}