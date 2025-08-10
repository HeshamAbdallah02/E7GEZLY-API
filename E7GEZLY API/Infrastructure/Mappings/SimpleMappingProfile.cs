using AutoMapper;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Infrastructure.Mappings
{
    /// <summary>
    /// Simplified AutoMapper profile focusing only on critical Venue entity mapping
    /// This resolves immediate compilation errors while maintaining functionality
    /// </summary>
    public class SimpleMappingProfile : Profile
    {
        public SimpleMappingProfile()
        {
            CreateVenueMappings();
            CreateVenueDetailMappings();
        }

        private void CreateVenueMappings()
        {
            // Core Venue mapping
            CreateMap<Domain.Entities.Venue, Models.Venue>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Name))
                .ForMember(dest => dest.VenueType, opt => opt.MapFrom(src => src.VenueType))
                .ForMember(dest => dest.Features, opt => opt.MapFrom(src => src.Features))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.Address.StreetAddress))
                .ForMember(dest => dest.Landmark, opt => opt.MapFrom(src => src.Address.Landmark))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address.Coordinates != null ? src.Address.Coordinates.Latitude : (double?)null))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address.Coordinates != null ? src.Address.Coordinates.Longitude : (double?)null))
                .ForMember(dest => dest.DistrictId, opt => opt.MapFrom(src => src.DistrictSystemId))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.District, opt => opt.Ignore())
                .ForMember(dest => dest.SubUsers, opt => opt.Ignore())
                .ForMember(dest => dest.AuditLogs, opt => opt.Ignore())
                .ForMember(dest => dest.WorkingHours, opt => opt.Ignore())
                .ForMember(dest => dest.Pricing, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.PlayStationDetails, opt => opt.Ignore());

            CreateMap<Models.Venue, Domain.Entities.Venue>()
                .ConstructUsing(src => Domain.Entities.Venue.CreateExistingVenue(
                    src.Id,
                    src.Name,
                    src.VenueType,
                    src.Features,
                    src.StreetAddress,
                    src.Landmark,
                    src.Latitude,
                    src.Longitude,
                    src.DistrictId,
                    src.IsProfileComplete,
                    src.RequiresSubUserSetup,
                    src.CreatedAt,
                    src.UpdatedAt
                ));

            // VenueSubUser mapping
            CreateMap<Domain.Entities.VenueSubUser, Models.VenueSubUser>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions));

            CreateMap<Models.VenueSubUser, Domain.Entities.VenueSubUser>()
                .ConstructUsing(src => Domain.Entities.VenueSubUser.Create(
                    src.VenueId,
                    src.Username,
                    src.PasswordHash,
                    src.Role,
                    src.Permissions,
                    src.CreatedBySubUserId ?? Guid.Empty
                ));

            // VenueSubUserSession mapping
            CreateMap<Domain.Entities.VenueSubUserSession, Models.VenueSubUserSession>();
            CreateMap<Models.VenueSubUserSession, Domain.Entities.VenueSubUserSession>()
                .ConstructUsing(src => Domain.Entities.VenueSubUserSession.Create(
                    src.SubUserId,
                    src.RefreshToken,
                    src.RefreshTokenExpiry,
                    src.DeviceName,
                    src.DeviceType,
                    src.IpAddress,
                    src.UserAgent,
                    src.AccessTokenJti
                ));
        }

        private void CreateVenueDetailMappings()
        {
            // VenueWorkingHours mapping
            CreateMap<Domain.Entities.VenueWorkingHours, Models.VenueWorkingHours>();
            CreateMap<Models.VenueWorkingHours, Domain.Entities.VenueWorkingHours>()
                .ConstructUsing(src => Domain.Entities.VenueWorkingHours.Create(
                    src.VenueId,
                    src.DayOfWeek,
                    src.OpenTime,
                    src.CloseTime,
                    src.IsClosed,
                    src.MorningStartTime,
                    src.MorningEndTime,
                    src.EveningStartTime,
                    src.EveningEndTime
                ));

            // VenuePricing mapping
            CreateMap<Domain.Entities.VenuePricing, Models.VenuePricing>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.PlayStationModel, opt => opt.MapFrom(src => src.PlayStationModel))
                .ForMember(dest => dest.RoomType, opt => opt.MapFrom(src => src.RoomType))
                .ForMember(dest => dest.GameMode, opt => opt.MapFrom(src => src.GameMode))
                .ForMember(dest => dest.TimeSlotType, opt => opt.MapFrom(src => src.TimeSlotType));

            CreateMap<Models.VenuePricing, Domain.Entities.VenuePricing>()
                .ConstructUsing(src => Domain.Entities.VenuePricing.Create(
                    src.VenueId,
                    src.Type,
                    src.Price,
                    src.Description,
                    src.PlayStationModel,
                    src.RoomType,
                    src.GameMode,
                    src.TimeSlotType,
                    src.DepositPercentage
                ));

            // VenueImage mapping
            CreateMap<Domain.Entities.VenueImage, Models.VenueImage>();
            CreateMap<Models.VenueImage, Domain.Entities.VenueImage>()
                .ConstructUsing(src => Domain.Entities.VenueImage.Create(
                    src.VenueId,
                    src.ImageUrl,
                    src.Caption,
                    src.DisplayOrder,
                    src.IsPrimary
                ));

            // VenuePlayStationDetails mapping
            CreateMap<Domain.Entities.VenuePlayStationDetails, Models.VenuePlayStationDetails>();
            CreateMap<Models.VenuePlayStationDetails, Domain.Entities.VenuePlayStationDetails>()
                .ConstructUsing(src => Domain.Entities.VenuePlayStationDetails.Create(
                    src.VenueId,
                    src.NumberOfRooms,
                    src.HasPS4,
                    src.HasPS5,
                    src.HasVIPRooms,
                    src.HasCafe,
                    src.HasWiFi,
                    src.ShowsMatches
                ));

            // VenueAuditLog mapping
            CreateMap<Domain.Entities.VenueAuditLog, Models.VenueAuditLog>()
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.CreatedAt));
            
            CreateMap<Models.VenueAuditLog, Domain.Entities.VenueAuditLog>()
                .ConstructUsing(src => Domain.Entities.VenueAuditLog.Create(
                    src.VenueId,
                    src.Action,
                    src.EntityType,
                    src.EntityId,
                    src.OldValues,
                    src.NewValues,
                    src.SubUserId,
                    src.IpAddress,
                    src.UserAgent,
                    src.AdditionalData
                ));
        }
    }
}