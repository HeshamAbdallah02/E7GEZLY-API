// Controllers/Auth/BaseAuthController.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{ 
    public abstract class BaseAuthController : ControllerBase
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly SignInManager<ApplicationUser> _signInManager;
        protected readonly ITokenService _tokenService;
        protected readonly IVerificationService _verificationService;
        protected readonly ILocationService _locationService;
        protected readonly IGeocodingService _geocodingService;
        protected readonly AppDbContext _context;
        protected readonly ILogger _logger;
        protected readonly IWebHostEnvironment _environment;

        protected BaseAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _verificationService = verificationService;
            _locationService = locationService;
            _geocodingService = geocodingService;
            _context = context;
            _logger = logger;
            _environment = environment;
        }
    }
}