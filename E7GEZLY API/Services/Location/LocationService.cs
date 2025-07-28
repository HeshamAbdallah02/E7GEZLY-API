using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.Models;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Services.Location
{
    public class LocationService : ILocationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LocationService> _logger;

        // Egypt's approximate boundaries
        private const double EgyptMinLat = 22.0;
        private const double EgyptMaxLat = 31.7;
        private const double EgyptMinLng = 25.0;
        private const double EgyptMaxLng = 35.0;

        public LocationService(AppDbContext context, ILogger<LocationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GovernorateDto>> GetGovernoratesAsync()
        {
            return await _context.Governorates
                .OrderBy(g => g.NameEn)
                .Select(g => new GovernorateDto(g.Id, g.NameEn, g.NameAr))
                .ToListAsync();
        }

        public async Task<List<DistrictDto>> GetDistrictsAsync(int? governorateId)
        {
            var query = _context.Districts.AsQueryable();

            if (governorateId.HasValue)
            {
                query = query.Where(d => d.GovernorateId == governorateId.Value);
            }

            return await query
                .OrderBy(d => d.NameEn)
                .Select(d => new DistrictDto(d.Id, d.NameEn, d.NameAr, d.GovernorateId))
                .ToListAsync();
        }

        public async Task<AddressValidationResultDto> ValidateAddressAsync(ValidateAddressDto dto)
        {
            var errors = new List<string>();

            // Validate coordinates if provided and not zero (0,0 means not provided)
            if (dto.Latitude.HasValue && dto.Longitude.HasValue &&
                (dto.Latitude.Value != 0 || dto.Longitude.Value != 0))
            {
                if (dto.Latitude.Value < EgyptMinLat || dto.Latitude.Value > EgyptMaxLat ||
                    dto.Longitude.Value < EgyptMinLng || dto.Longitude.Value > EgyptMaxLng)
                {
                    errors.Add("The coordinates appear to be outside of Egypt");
                }
            }

            // Validate governorate/district combination if provided
            if (!string.IsNullOrWhiteSpace(dto.Governorate) && !string.IsNullOrWhiteSpace(dto.District))
            {
                var governorateExists = await _context.Governorates
                    .AnyAsync(g => g.NameEn.ToLower() == dto.Governorate.ToLower() ||
                                   g.NameAr == dto.Governorate);

                if (!governorateExists)
                {
                    errors.Add($"Governorate '{dto.Governorate}' not found");
                }
                else
                {
                    var districtExists = await _context.Districts
                        .Include(d => d.Governorate)
                        .AnyAsync(d => (d.NameEn.ToLower() == dto.District.ToLower() || d.NameAr == dto.District) &&
                                      (d.Governorate.NameEn.ToLower() == dto.Governorate.ToLower() ||
                                       d.Governorate.NameAr == dto.Governorate));

                    if (!districtExists)
                    {
                        errors.Add($"District '{dto.District}' not found in governorate '{dto.Governorate}'");
                    }
                }
            }

            // At least one location method should be provided
            if (string.IsNullOrWhiteSpace(dto.Governorate) &&
                string.IsNullOrWhiteSpace(dto.District) &&
                (!dto.Latitude.HasValue || !dto.Longitude.HasValue ||
                 dto.Latitude.Value == 0 && dto.Longitude.Value == 0))
            {
                errors.Add("Please provide either governorate/district or valid map coordinates");
            }

            return new AddressValidationResultDto(
                IsValid: !errors.Any(),
                Message: errors.Any() ? "Validation failed" : "Address is valid",
                Errors: errors.Any() ? errors : null
            );
        }

        public async Task<District?> FindDistrictAsync(string governorateName, string districtName)
        {
            return await _context.Districts
                .Include(d => d.Governorate)
                .FirstOrDefaultAsync(d =>
                    (d.NameEn.ToLower() == districtName.ToLower() || d.NameAr == districtName) &&
                    (d.Governorate.NameEn.ToLower() == governorateName.ToLower() ||
                     d.Governorate.NameAr == governorateName));
        }
    }
}