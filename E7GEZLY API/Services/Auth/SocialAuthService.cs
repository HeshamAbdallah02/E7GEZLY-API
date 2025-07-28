using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace E7GEZLY_API.Services.Auth
{
    public class SocialAuthService : ISocialAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SocialAuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IVerificationService _verificationService;

        public SocialAuthService(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<SocialAuthService> logger,
            IConfiguration configuration,
            IVerificationService verificationService)
        {
            _userManager = userManager;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _verificationService = verificationService;
        }

        public async Task<SocialUserInfoDto?> ValidateProviderTokenAsync(string provider, string accessToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                return provider.ToLower() switch
                {
                    "facebook" => await ValidateFacebookTokenAsync(httpClient, accessToken),
                    "google" => await ValidateGoogleTokenAsync(httpClient, accessToken),
                    "apple" => await ValidateAppleTokenAsync(accessToken),
                    _ => throw new NotSupportedException($"Provider {provider} is not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating {Provider} token", provider);
                return null;
            }
        }

        private async Task<SocialUserInfoDto?> ValidateFacebookTokenAsync(HttpClient httpClient, string accessToken)
        {
            var response = await httpClient.GetAsync(
                $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,name,picture&access_token={accessToken}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(content);

            return new SocialUserInfoDto(
                Id: data.RootElement.GetProperty("id").GetString()!,
                Email: data.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null,
                Name: data.RootElement.GetProperty("name").GetString(),
                FirstName: data.RootElement.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : null,
                LastName: data.RootElement.TryGetProperty("last_name", out var lastName) ? lastName.GetString() : null,
                Picture: data.RootElement.TryGetProperty("picture", out var picture)
                    ? picture.GetProperty("data").GetProperty("url").GetString() : null
            );
        }

        private async Task<SocialUserInfoDto?> ValidateGoogleTokenAsync(HttpClient httpClient, string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(content);

            return new SocialUserInfoDto(
                Id: data.RootElement.GetProperty("id").GetString()!,
                Email: data.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null,
                Name: data.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null,
                FirstName: data.RootElement.TryGetProperty("given_name", out var givenName) ? givenName.GetString() : null,
                LastName: data.RootElement.TryGetProperty("family_name", out var familyName) ? familyName.GetString() : null,
                Picture: data.RootElement.TryGetProperty("picture", out var picture) ? picture.GetString() : null
            );
        }

        private Task<SocialUserInfoDto?> ValidateAppleTokenAsync(string identityToken)
        {
            // Apple Sign In validation is more complex - this is a simplified version
            // In production, you'd validate the JWT signature with Apple's public keys
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(identityToken);

                var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
                var email = jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Task.FromResult<SocialUserInfoDto?>(null);

                var result = new SocialUserInfoDto(
                    Id: userId,
                    Email: email,
                    Name: null, // Apple doesn't always provide name
                    FirstName: null,
                    LastName: null,
                    Picture: null
                );

                return Task.FromResult<SocialUserInfoDto?>(result);
            }
            catch
            {
                return Task.FromResult<SocialUserInfoDto?>(null);
            }
        }

        public async Task<ApplicationUser?> FindOrCreateUserAsync(string provider, SocialUserInfoDto providerUser)
        {
            // First, check if external login exists
            var externalLogin = await _context.ExternalLogins
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderUserId == providerUser.Id);

            if (externalLogin != null)
            {
                _logger.LogInformation("Found existing user via external login: {UserId}", externalLogin.UserId);
                return externalLogin.User;
            }

            // If email provided, check if user exists with that email
            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(providerUser.Email))
            {
                user = await _userManager.FindByEmailAsync(providerUser.Email);
                if (user != null)
                {
                    _logger.LogInformation("Found existing user by email: {Email}", providerUser.Email);
                    // Link this social login to existing user
                    CreateExternalLogin(user, provider, providerUser);
                    await _context.SaveChangesAsync();
                    return user;
                }
            }

            // Create new CUSTOMER user (social login is not available for venues)
            user = new ApplicationUser
            {
                UserName = providerUser.Email ?? $"{provider}_{providerUser.Id}",
                Email = providerUser.Email,
                EmailConfirmed = !string.IsNullOrEmpty(providerUser.Email), // Set by Identity
                IsEmailVerified = !string.IsNullOrEmpty(providerUser.Email), // Set to true for social login
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsPhoneNumberVerified = false // Phone still needs verification if added later
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return null;
            }

            // Add to Customer role by default (social logins are customers)
            await _userManager.AddToRoleAsync(user, "Customer");

            // Parse name for customer profile
            string firstName = providerUser.FirstName ?? "User";
            string lastName = providerUser.LastName ?? "";

            // If we only have a full name, try to split it
            if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(providerUser.Name))
            {
                var nameParts = providerUser.Name.Split(' ', 2);
                firstName = nameParts[0];
                lastName = nameParts.Length > 1 ? nameParts[1] : "";
            }

            // Create customer profile
            var customerProfile = new CustomerProfile
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomerProfiles.Add(customerProfile);

            // Create external login record
            CreateExternalLogin(user, provider, providerUser);

            await _context.SaveChangesAsync();

            // Send welcome email (don't fail registration if email fails)
            if (!string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    await _verificationService.SendWelcomeEmailAsync(
                        user.Email,
                        firstName,
                        "Customer"
                    );
                    _logger.LogInformation("Welcome email sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}, but registration succeeded", user.Email);
                    // Don't fail - registration is already complete
                }
            }

            _logger.LogInformation("Created new user via {Provider}: {UserId}, Email: {Email}", provider, user.Id, user.Email);
            return user;
        }

        private void CreateExternalLogin(ApplicationUser user, string provider, SocialUserInfoDto providerUser)
        {
            var externalLogin = new ExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = providerUser.Id,
                ProviderEmail = providerUser.Email,
                ProviderDisplayName = providerUser.Name,
                LastLoginAt = DateTime.UtcNow
            };

            _context.ExternalLogins.Add(externalLogin);
        }

        public async Task UpdateExternalLoginAsync(ApplicationUser user, string provider, string providerUserId)
        {
            var externalLogin = await _context.ExternalLogins
                .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Provider == provider);

            if (externalLogin != null)
            {
                externalLogin.LastLoginAt = DateTime.UtcNow;
                externalLogin.UpdatedAt = DateTime.UtcNow;
            }
        }

        public IEnumerable<string> GetAvailableProviders(bool isAppleDevice)
        {
            var providers = new List<string> { "facebook", "google" };

            if (isAppleDevice)
                providers.Add("apple");

            return providers;
        }
    }
}