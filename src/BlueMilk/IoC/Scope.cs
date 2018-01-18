﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Baseline;
using Baseline.Reflection;
using BlueMilk.IoC.Diagnostics;
using BlueMilk.IoC.Instances;
using Microsoft.Extensions.DependencyInjection;

namespace BlueMilk.IoC
{
    public class Scope : IContainer, IServiceScope, IServiceProvider, ISupportRequiredService, IServiceScopeFactory
    {
        public static Scope Empty()
        {
            return new Scope(new ServiceRegistry());
        }
        


        public Scope(IServiceCollection services)
        {
            ServiceGraph = new ServiceGraph(services, this);
            ServiceGraph.Initialize();
        }

        public Scope(ServiceGraph serviceGraph)
        {
            ServiceGraph = serviceGraph;
        }

        public DisposalLock DisposalLock { get; set; } = DisposalLock.Unlocked;


        public IModel Model => ServiceGraph;

        internal ServiceGraph ServiceGraph { get; }


        // TODO -- hide this from the public class?
        public IList<IDisposable> Disposables { get; } = new List<IDisposable>();

        public void RegisterDisposable(IDisposable disposable)
        {
            Disposables.Add(disposable);
        }
        
        internal readonly Dictionary<int, object> Services = new Dictionary<int, object>();


        public virtual void Dispose()
        {
            foreach (var disposable in Disposables)
            {
                disposable.SafeDispose();
            }
        }

        public IServiceProvider ServiceProvider => this;
        
        public object GetService(Type serviceType)
        {
            return TryGetInstance(serviceType);
        }
        
        public T GetInstance<T>()
        {
            // TODO -- sad path, not found
            // TODO -- validate object disposed
            return (T) GetInstance(typeof(T));
        }

        public T GetInstance<T>(string name)
        {
            return (T) GetInstance(typeof(T), name);
        }

        public object GetInstance(Type serviceType)
        {
            var resolver = ServiceGraph.FindResolver(serviceType);
            
            // TODO -- validate the existence of the resolver first
            if (resolver == null)
            {
                throw new BlueMilkMissingRegistrationException(serviceType);
            }
            
            return resolver.Resolve(this);
        }

        public object GetInstance(Type serviceType, string name)
        {
            // TODO -- sad path, not found
            // TODO -- validate object disposed
            var resolver = ServiceGraph.FindResolver(serviceType, name);
            if (resolver == null)
            {
                throw new BlueMilkMissingRegistrationException(serviceType, name);
            }
            
            return resolver.Resolve(this);
        }
        
        public T TryGetInstance<T>()
        {
            return (T)(TryGetInstance(typeof(T)) ?? default(T));
        }

        public T TryGetInstance<T>(string name)
        {
            return (T)(TryGetInstance(typeof(T), name) ?? default(T));
        }

        public object TryGetInstance(Type serviceType)
        {
            var resolver = ServiceGraph.FindResolver(serviceType);
            return resolver?.Resolve(this) ?? null;
        }

        public object TryGetInstance(Type serviceType, string name)
        {
            var resolver = ServiceGraph.FindResolver(serviceType, name);
            return resolver?.Resolve(this) ?? null;
        }

        public T QuickBuild<T>()
        {
            return (T) QuickBuild(typeof(T));

        }

        public object QuickBuild(Type objectType)
        {
            if (!objectType.IsConcrete()) throw new InvalidOperationException("Type must be concrete");

            var ctor = ConstructorInstance.DetermineConstructor(ServiceGraph, objectType, out var message);
            if (ctor == null) throw new InvalidOperationException(message);

            var dependencies = ctor.GetParameters().Select(x =>
            {
                if (x.HasAttribute<NamedAttribute>())
                {
                    return GetInstance(x.ParameterType, x.GetAttribute<NamedAttribute>().Name);
                }
                
                
                return GetInstance(x.ParameterType);
            }).ToArray();

            return Activator.CreateInstance(objectType, dependencies);
        }

        public IContainer GetNestedContainer()
        {
            return new Scope(ServiceGraph);
        }

        public IReadOnlyList<T> GetAllInstances<T>()
        {
            return ServiceGraph.FindAll(typeof(T)).Select(x => x.Resolver.Resolve(this)).OfType<T>().ToList();
        }

        public IEnumerable GetAllInstances(Type serviceType)
        {
            return ServiceGraph.FindAll(serviceType).Select(x => x.Resolver.Resolve(this)).ToArray();
        }


        object ISupportRequiredService.GetRequiredService(Type serviceType)
        {
            return GetInstance(serviceType);
        }

        IServiceScope IServiceScopeFactory.CreateScope()
        {
            return new Scope(ServiceGraph);
        }
        

        public string WhatDoIHave(Type serviceType = null, Assembly assembly = null, string @namespace = null,
            string typeName = null)
        {
            //assertNotDisposed();

            var writer = new WhatDoIHaveWriter(ServiceGraph);
            return writer.GetText(new ModelQuery
            {
                Assembly = assembly,
                Namespace = @namespace,
                ServiceType = serviceType,
                TypeName = typeName
            });
        }
    }
}