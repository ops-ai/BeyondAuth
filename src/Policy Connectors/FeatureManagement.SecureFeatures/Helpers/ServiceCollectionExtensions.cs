using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeatureManagement.SecureFeatures.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static void Decorate<TInterface, TDecorator>(this IServiceCollection services)
          where TInterface : class
          where TDecorator : class, TInterface
        {
            // grab the existing registration
            var wrappedDescriptor = services.FirstOrDefault(
              s => s.ServiceType == typeof(TInterface));

            // check it's valid
            if (wrappedDescriptor == null)
                throw new InvalidOperationException($"{typeof(TInterface).Name} is not registered");

            // create the object factory for our decorator type,
            // specifying that we will supply TInterface explicitly
            var objectFactory = ActivatorUtilities.CreateFactory(
              typeof(TDecorator),
              new[] { typeof(TInterface) });

            // replace the existing registration with one
            // that passes an instance of the existing registration
            // to the object factory for the decorator
            services.Replace(ServiceDescriptor.Describe(
              typeof(TInterface),
              s => (TInterface)objectFactory(s, new[] { s.CreateInstance(wrappedDescriptor) }),
              wrappedDescriptor.Lifetime)
            );
        }

        private static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(services);

            return ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType!);
        }
    }
}
