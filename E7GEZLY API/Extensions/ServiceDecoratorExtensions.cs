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

            var objectFactory = ActivatorUtilities.CreateFactory(
                existingService.ImplementationType ?? typeof(TInterface),
                new[] { typeof(IServiceProvider) });

            services.Add(new ServiceDescriptor(
                typeof(TInterface),
                provider =>
                {
                    var instance = (TInterface)objectFactory(provider, null);
                    return decorator(instance, provider);
                },
                existingService.Lifetime));

            return services;
        }
    }
}