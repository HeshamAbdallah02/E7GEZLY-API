using AutoMapper;
using E7GEZLY_API.Application.Features.Authentication.Commands.Register;
using E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteCourtProfile;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Application.Common.Mappings
{
    /// <summary>
    /// AutoMapper profile for E7GEZLY Application layer
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterVenueDto, RegisterVenueCommand>();
            
            CreateMap<CompleteCourtProfileDto, CompleteCourtProfileCommand>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId comes from context
            
            // Domain Venue mappings
            CreateMap<Domain.Entities.Venue, VenueDetailsDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.VenueType.ToString()))
                .ForMember(dest => dest.TypeValue, opt => opt.MapFrom(src => (int)src.VenueType))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => 
                    src.Address.Coordinates != null ?
                        new
                        {
                            latitude = src.Address.Coordinates.Latitude,
                            longitude = src.Address.Coordinates.Longitude,
                            streetAddress = src.Address.StreetAddress,
                            landmark = src.Address.Landmark,
                            districtNameEn = src.District != null ? src.District.NameEn : null,
                            districtNameAr = src.District != null ? src.District.NameAr : null,
                            governorateNameEn = src.District != null && src.District.Governorate != null ? src.District.Governorate.NameEn : null,
                            governorateNameAr = src.District != null && src.District.Governorate != null ? src.District.Governorate.NameAr : null,
                            fullAddress = src.GetFullAddress()
                        } : null));

            // Models Venue mappings (for backward compatibility)
            CreateMap<E7GEZLY_API.Models.Venue, VenueDetailsDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.VenueType.ToString()))
                .ForMember(dest => dest.TypeValue, opt => opt.MapFrom(src => (int)src.VenueType))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => 
                    src.Latitude.HasValue && src.Longitude.HasValue ?
                        new
                        {
                            latitude = src.Latitude,
                            longitude = src.Longitude,
                            streetAddress = src.StreetAddress,
                            landmark = src.Landmark,
                            districtNameEn = src.District != null ? src.District.NameEn : null,
                            districtNameAr = src.District != null ? src.District.NameAr : null,
                            governorateNameEn = src.District != null && src.District.Governorate != null ? src.District.Governorate.NameEn : null,
                            governorateNameAr = src.District != null && src.District.Governorate != null ? src.District.Governorate.NameAr : null,
                            fullAddress = src.FullAddress
                        } : null));

            // User mappings
            CreateMap<ApplicationUser, UserAuthInfoDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.IsPhoneVerified, opt => opt.MapFrom(src => src.IsPhoneNumberVerified))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified));

            // Sub user mappings (Domain entity)
            CreateMap<Domain.Entities.VenueSubUser, VenueSubUserResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsFounderAdmin, opt => opt.MapFrom(src => src.IsFounderAdmin))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt))
                .ForMember(dest => dest.CreatedByUsername, opt => opt.Ignore()); // Requires additional lookup
            
            // Domain to Model entity mappings
            CreateMap<Domain.Entities.Venue, E7GEZLY_API.Models.Venue>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenueSubUser, E7GEZLY_API.Models.VenueSubUser>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenueSubUserSession, E7GEZLY_API.Models.VenueSubUserSession>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenueWorkingHours, E7GEZLY_API.Models.VenueWorkingHours>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenuePricing, E7GEZLY_API.Models.VenuePricing>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenueImage, E7GEZLY_API.Models.VenueImage>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenuePlayStationDetails, E7GEZLY_API.Models.VenuePlayStationDetails>()
                .ReverseMap();
            CreateMap<Domain.Entities.VenueAuditLog, E7GEZLY_API.Models.VenueAuditLog>()
                .ReverseMap();
            CreateMap<Domain.Entities.User, E7GEZLY_API.Models.ApplicationUser>()
                .ReverseMap();
        }
    }
}