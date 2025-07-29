// DTOs/Auth/LoginDto.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Auth
{
    public record LoginDto
    {
        [Required(ErrorMessage = "Email or phone number is required")]
        public string EmailOrPhone { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; init; } = string.Empty;
    }
}