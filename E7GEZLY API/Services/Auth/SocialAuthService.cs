using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Domain.Entities;
using EfExternalLogin = E7GEZLY_API.Models.ExternalLogin;
using ApplicationUser = E7GEZLY_API.Models.ApplicationUser;
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

        private async Task<SocialUserInfoDto?> ValidateAppleTokenAsync(string identityToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                
                // Get Apple's public keys
                var keysResponse = await httpClient.GetAsync("https://appleid.apple.com/auth/keys");
                if (!keysResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to retrieve Apple's public keys");
                    return null;
                }

                var keysJson = await keysResponse.Content.ReadAsStringAsync();
                var keysDoc = JsonDocument.Parse(keysJson);
                
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(identityToken);
                
                // Get the key ID from token header
                var keyId = jsonToken.Header.Kid;
                if (string.IsNullOrEmpty(keyId))
                {
                    _logger.LogError("Apple JWT token missing 'kid' header");
                    return null;
                }

                // Find the matching public key
                var key = FindApplePublicKey(keysDoc, keyId);
                if (key == null)
                {
                    _logger.LogError("No matching Apple public key found for kid: {KeyId}", keyId);
                    return null;
                }

                // Validate token signature and claims
                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Authentication:Apple:ClientId"], // Your Apple app's bundle ID
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                try
                {
                    var claimsPrincipal = handler.ValidateToken(identityToken, validationParameters, out var validatedToken);
                    
                    var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
                    var email = jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
                    
                    // Additional Apple-specific validation
                    var audience = jsonToken.Claims.FirstOrDefault(x => x.Type == "aud")?.Value;
                    var issuer = jsonToken.Claims.FirstOrDefault(x => x.Type == "iss")?.Value;
                    
                    if (issuer != "https://appleid.apple.com")
                    {
                        _logger.LogError("Invalid Apple token issuer: {Issuer}", issuer);
                        return null;
                    }

                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogError("Apple token missing user ID");
                        return null;
                    }

                    _logger.LogInformation("Apple token validated successfully for user: {UserId}", userId);

                    return new SocialUserInfoDto(
                        Id: userId,
                        Email: email,
                        Name: null, // Apple doesn't always provide name in JWT
                        FirstName: null,
                        LastName: null,
                        Picture: null
                    );
                }
                catch (Microsoft.IdentityModel.Tokens.SecurityTokenValidationException ex)
                {
                    _logger.LogError(ex, "Apple token validation failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Apple identity token");
                return null;
            }
        }

        private Microsoft.IdentityModel.Tokens.SecurityKey? FindApplePublicKey(JsonDocument keysDoc, string keyId)
        {
            try
            {
                var keys = keysDoc.RootElement.GetProperty("keys");
                
                foreach (var keyElement in keys.EnumerateArray())
                {
                    if (keyElement.TryGetProperty("kid", out var kidProperty) && 
                        kidProperty.GetString() == keyId)
                    {
                        // Extract RSA parameters
                        var n = keyElement.GetProperty("n").GetString(); // Modulus
                        var e = keyElement.GetProperty("e").GetString(); // Exponent
                        
                        if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(e))
                            continue;
                            
                        // Convert base64url to RSA parameters
                        var modulus = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(n);
                        var exponent = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(e);
                        
                        var rsaParameters = new System.Security.Cryptography.RSAParameters
                        {
                            Modulus = modulus,
                            Exponent = exponent
                        };
                        
                        var rsa = System.Security.Cryptography.RSA.Create();
                        rsa.ImportParameters(rsaParameters);
                        
                        return new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Apple public key");
                return null;
            }
        }

        public async Task<ApplicationUser?> FindOrCreateUserAsync(string provider, SocialUserInfoDto providerUser)
        {
            // First, check if external login exists
            var externalLogin = await _context.ExternalLogins
                .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderUserId == providerUser.Id);

            if (externalLogin != null)
            {
                _logger.LogInformation("Found existing user via external login: {UserId}", externalLogin.UserId);
                
                // Get the user separately since domain entities don't have navigation properties
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == externalLogin.UserId);
                return existingUser;
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
            var customerProfile = CustomerProfile.Create(
                user.Id,
                firstName,
                lastName
            );
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
            var externalLogin = ExternalLogin.Create(
                user.Id,
                provider,
                providerUser.Id,
                providerUser.Email,
                providerUser.Name
            );

            _context.ExternalLogins.Add(externalLogin);
        }

        public async Task UpdateExternalLoginAsync(ApplicationUser user, string provider, string providerUserId)
        {
            var externalLogin = await _context.ExternalLogins
                .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Provider == provider);

            if (externalLogin != null)
            {
                externalLogin.RecordLogin();
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