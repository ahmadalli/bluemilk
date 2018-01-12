﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Baseline;
using BlueMilk.IoC.Instances;
using Microsoft.Extensions.DependencyInjection;

namespace BlueMilk.Scanning.Conventions
{
    public class ConnectedConcretions : List<Type>
    {
        
    }
    
    public static class ServiceCollectionExtensions
    {
        public static ConnectedConcretions ConnectedConcretions(this IServiceCollection services)
        {
            var concretions = services.FirstOrDefault(x => x.ServiceType == typeof(ConnectedConcretions))
                ?.ImplementationInstance as ConnectedConcretions;

            if (concretions == null)
            {
                concretions = new ConnectedConcretions();
                services.AddSingleton(concretions);
            }

            return concretions;
        }
        
        public static void Add(this IServiceCollection services, Instance instance)
        {
            services.Add(new ServiceDescriptor(instance.ServiceType, instance));
        }

        public static bool Matches(this ServiceDescriptor descriptor, Type serviceType, Type implementationType)
        {
            if (descriptor.ServiceType != serviceType) return false;

            if (descriptor.ImplementationType == implementationType) return true;

            if (descriptor.ImplementationInstance is Instance)
                return descriptor.ImplementationInstance.As<Instance>().ImplementationType == implementationType;

            return false;
        }
        
        public static Instance AddType(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            var hasAlready = services.Any(x => x.Matches(serviceType, implementationType));
            if (!hasAlready)
            {
                var instance = new ConstructorInstance(serviceType, implementationType, ServiceLifetime.Transient);
                
                services.Add(instance);

                return instance;
            }

            return null;
        }

        public static ServiceDescriptor FindDefault<T>(this IServiceCollection services)
        {
            return services.FindDefault(typeof(T));
        }

        public static ServiceDescriptor FindDefault(this IServiceCollection services, Type serviceType)
        {
            return services.LastOrDefault(x => x.ServiceType == serviceType);
        }

        public static Type[] RegisteredTypesFor<T>(this IServiceCollection services)
        {
            return services
                .Where(x => x.ServiceType == typeof(T) && x.ImplementationType != null)
                .Select(x => x.ImplementationType)
                .ToArray();
        }

        public static async Task ApplyScannedTypes(this IServiceCollection services)
        {
            foreach (var scanner in services.Select(x => x.ImplementationInstance).OfType<AssemblyScanner>().ToArray())
            {
                await scanner.ApplyRegistrations(services);
            }
        }

        public static async Task<IServiceCollection> Combine(this IServiceCollection[] serviceCollections)
        {
            if (!serviceCollections.Any()) return new ServiceRegistry();

            foreach (var services in serviceCollections)
            {
                await services.ApplyScannedTypes();
            }

            if (serviceCollections.Length == 1) return serviceCollections[0];

            var response = new ServiceRegistry();
            response.AddRange(serviceCollections.SelectMany(x => x));

            return response;
        }
    }
}
