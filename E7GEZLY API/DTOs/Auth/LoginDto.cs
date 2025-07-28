// DTOs/Auth/LoginDto.cs
namespace E7GEZLY_API.DTOs.Auth
{
    public record LoginDto(
        string Email,
        string Password
    );
}