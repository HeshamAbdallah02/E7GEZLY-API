using E7GEZLY_API.Application.Common.Behaviors;
using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Services;
using E7GEZLY_API.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace E7GEZLY_API.Application
{
    /// <summary>
    /// Dependency injection configuration for Application layer
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Add Application layer services to the container
        /// </summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR registration with assembly scanning
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // AutoMapper registration
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // FluentValidation registration
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // MediatR behaviors (order matters!)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Application services
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddTransient<IDateTimeService, DateTimeService>();

            // Domain services - these contain business logic and should be registered here
            // as they are used by Application layer handlers
            services.AddScoped<IVenueProfileCompletionService, VenueProfileCompletionService>();
            services.AddScoped<IUserVerificationService, UserVerificationService>();

            return services;
        }
    }
}