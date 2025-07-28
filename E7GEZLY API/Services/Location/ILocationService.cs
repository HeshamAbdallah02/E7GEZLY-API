using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.Models;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Services.Location
{
    public interface ILocationService
    {
        Task<List<GovernorateDto>> GetGovernoratesAsync();
        Task<List<DistrictDto>> GetDistrictsAsync(int? governorateId);
        Task<AddressValidationResultDto> ValidateAddressAsync(ValidateAddressDto dto);
        Task<District?> FindDistrictAsync(string governorateName, string districtName);
    }
}
