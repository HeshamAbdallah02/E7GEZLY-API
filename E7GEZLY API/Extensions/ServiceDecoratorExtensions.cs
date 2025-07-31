// E7GEZLY API/Extensions/ServiceDecoratorExtensions.cs
namespace E7GEZLY_API.Extensions
{
    /// <summary>
    /// Extension methods for decorating services
    /// </summary>
    public static class ServiceDecoratorExtensions
    {
        /// <summary>
        /// Decorates an existing service registration
        /// </summary>
        public static IServiceCollection Decorate<TInterface>(
            this IServiceCollection services,
            Func<TInterface, IServiceProvider, TInterface> decorator)
            where TInterface : class
        {
            var existingService = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));
            if (existingService == null)
            {
                throw new InvalidOperationException($"Service of type {typeof(TInterface).Name} is not registered.");
            }

            services.Remove(existingService);

            // Create a new service descriptor that properly handles dependency injection
            services.Add(new ServiceDescriptor(
                typeof(TInterface),
                provider =>
                {
                    // Create the original service instance using the service provider
                    TInterface originalInstance;

                    if (existingService.ImplementationInstance != null)
                    {
                        originalInstance = (TInterface)existingService.ImplementationInstance;
                    }
                    else if (existingService.ImplementationFactory != null)
                    {
                        originalInstance = (TInterface)existingService.ImplementationFactory(provider);
                    }
                    else if (existingService.ImplementationType != null)
                    {
                        originalInstance = (TInterface)ActivatorUtilities.CreateInstance(provider, existingService.ImplementationType);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unable to create instance of {typeof(TInterface).Name}");
                    }

                    // Apply the decorator
                    return decorator(originalInstance, provider);
                },
                existingService.Lifetime));

            return services;
        }
    }
}