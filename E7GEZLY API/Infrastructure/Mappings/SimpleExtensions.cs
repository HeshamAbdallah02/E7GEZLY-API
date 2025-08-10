using AutoMapper;
using E7GEZLY_API.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using ApplicationUser = E7GEZLY_API.Models.ApplicationUser;
using Venue = E7GEZLY_API.Models.Venue;
using VenueSubUser = E7GEZLY_API.Models.VenueSubUser;
using VenueSubUserSession = E7GEZLY_API.Models.VenueSubUserSession;
using VenueWorkingHours = E7GEZLY_API.Models.VenueWorkingHours;
using VenuePricing = E7GEZLY_API.Models.VenuePricing;
using VenueImage = E7GEZLY_API.Models.VenueImage;
using VenuePlayStationDetails = E7GEZLY_API.Models.VenuePlayStationDetails;
using VenueAuditLog = E7GEZLY_API.Models.VenueAuditLog;
using DomainVenueWorkingHours = E7GEZLY_API.Domain.Entities.VenueWorkingHours;
using DomainVenuePricing = E7GEZLY_API.Domain.Entities.VenuePricing;
using DomainVenueImage = E7GEZLY_API.Domain.Entities.VenueImage;
using DomainVenuePlayStationDetails = E7GEZLY_API.Domain.Entities.VenuePlayStationDetails;
using DomainVenueAuditLog = E7GEZLY_API.Domain.Entities.VenueAuditLog;

namespace E7GEZLY_API.Infrastructure.Mappings
{
    /// <summary>
    /// Simple extension methods for entity conversion using AutoMapper
    /// These extensions use the service provider to get the mapper instance
    /// </summary>
    public static class SimpleExtensions
    {
        private static IMapper? _staticMapper;

        // Initialize the static mapper from the service provider
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _staticMapper = serviceProvider.GetRequiredService<IMapper>();
        }

        private static IMapper GetMapper()
        {
            if (_staticMapper == null)
                throw new InvalidOperationException("SimpleExtensions not initialized. Call Initialize() first.");
            return _staticMapper;
        }

        // Domain to Model conversions
        public static Venue ToModel(this Domain.Entities.Venue entity)
        {
            return GetMapper().Map<Venue>(entity);
        }

        public static VenueSubUser ToModel(this Domain.Entities.VenueSubUser entity)
        {
            return GetMapper().Map<VenueSubUser>(entity);
        }

        public static VenueSubUserSession ToModel(this Domain.Entities.VenueSubUserSession entity)
        {
            return GetMapper().Map<VenueSubUserSession>(entity);
        }

        public static VenueWorkingHours ToModel(this Domain.Entities.VenueWorkingHours entity)
        {
            return GetMapper().Map<VenueWorkingHours>(entity);
        }

        public static VenuePricing ToModel(this Domain.Entities.VenuePricing entity)
        {
            return GetMapper().Map<VenuePricing>(entity);
        }

        public static VenueImage ToModel(this Domain.Entities.VenueImage entity)
        {
            return GetMapper().Map<VenueImage>(entity);
        }

        public static VenuePlayStationDetails ToModel(this Domain.Entities.VenuePlayStationDetails entity)
        {
            return GetMapper().Map<VenuePlayStationDetails>(entity);
        }

        public static VenueAuditLog ToModel(this DomainVenueAuditLog entity)
        {
            return GetMapper().Map<VenueAuditLog>(entity);
        }

        // Model to Domain conversions
        public static Domain.Entities.Venue ToDomainEntity(this Venue model)
        {
            return GetMapper().Map<Domain.Entities.Venue>(model);
        }

        public static Domain.Entities.VenueSubUser ToDomainEntity(this VenueSubUser model)
        {
            return GetMapper().Map<Domain.Entities.VenueSubUser>(model);
        }

        public static Domain.Entities.VenueSubUserSession ToDomainEntity(this VenueSubUserSession model)
        {
            return GetMapper().Map<Domain.Entities.VenueSubUserSession>(model);
        }

        public static DomainVenueWorkingHours ToDomainEntity(this VenueWorkingHours model)
        {
            return GetMapper().Map<DomainVenueWorkingHours>(model);
        }

        public static DomainVenuePricing ToDomainEntity(this VenuePricing model)
        {
            return GetMapper().Map<DomainVenuePricing>(model);
        }

        public static DomainVenueImage ToDomainEntity(this VenueImage model)
        {
            return GetMapper().Map<DomainVenueImage>(model);
        }

        public static DomainVenuePlayStationDetails ToDomainEntity(this VenuePlayStationDetails model)
        {
            return GetMapper().Map<DomainVenuePlayStationDetails>(model);
        }

        public static DomainVenueAuditLog ToDomainEntity(this VenueAuditLog model)
        {
            return GetMapper().Map<DomainVenueAuditLog>(model);
        }
        
        // ApplicationUser to Domain User mapping
        public static Domain.Entities.User ToDomainEntity(this ApplicationUser model)
        {
            return GetMapper().Map<Domain.Entities.User>(model);
        }
        
        // Domain User to ApplicationUser mapping
        public static ApplicationUser ToModel(this Domain.Entities.User entity)
        {
            return GetMapper().Map<ApplicationUser>(entity);
        }

        // Collection extensions for Models to Domain
        public static IEnumerable<Domain.Entities.Venue> ToDomainEntities(this IEnumerable<Venue> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        public static IEnumerable<Domain.Entities.VenueSubUser> ToDomainEntities(this IEnumerable<VenueSubUser> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        public static IEnumerable<Domain.Entities.VenueSubUserSession> ToDomainEntities(this IEnumerable<VenueSubUserSession> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        public static IEnumerable<DomainVenueWorkingHours> ToDomainEntities(this IEnumerable<VenueWorkingHours> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        public static IEnumerable<DomainVenuePricing> ToDomainEntities(this IEnumerable<VenuePricing> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        public static IEnumerable<DomainVenueImage> ToDomainEntities(this IEnumerable<VenueImage> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        public static IEnumerable<DomainVenueAuditLog> ToDomainEntities(this IEnumerable<VenueAuditLog> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }

        // List extensions for Models to Domain
        public static List<Domain.Entities.Venue> ToDomainEntities(this List<Venue> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }

        public static List<Domain.Entities.VenueSubUser> ToDomainEntities(this List<VenueSubUser> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }

        public static List<Domain.Entities.VenueSubUserSession> ToDomainEntities(this List<VenueSubUserSession> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }

        public static List<DomainVenueWorkingHours> ToDomainEntities(this List<VenueWorkingHours> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }

        public static List<DomainVenuePricing> ToDomainEntities(this List<VenuePricing> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }

        public static List<DomainVenueImage> ToDomainEntities(this List<VenueImage> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }

        public static List<DomainVenueAuditLog> ToDomainEntities(this List<VenueAuditLog> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }
        
        // ApplicationUser collection mappings
        public static IEnumerable<Domain.Entities.User> ToDomainEntities(this IEnumerable<ApplicationUser> models)
        {
            return models.Select(m => m.ToDomainEntity());
        }
        
        public static List<Domain.Entities.User> ToDomainEntities(this List<ApplicationUser> models)
        {
            return models.Select(m => m.ToDomainEntity()).ToList();
        }
    }
}