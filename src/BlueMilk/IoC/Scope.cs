﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Baseline;
using Baseline.Reflection;
using BlueMilk.Compilation;
using BlueMilk.IoC.Diagnostics;
using BlueMilk.IoC.Instances;
using BlueMilk.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace BlueMilk.IoC
{
    public class Scope : IServiceScope, IServiceProvider, ISupportRequiredService, IServiceScopeFactory
    {
        protected bool _hasDisposed;
        
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
        
        /// <summary>
        /// Asserts that this container is not disposed yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the container is disposed.</exception>
        protected void assertNotDisposed()
        {
            if (!_hasDisposed) return;

            throw new ObjectDisposedException("This Container has been disposed");
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
            if (DisposalLock == DisposalLock.Ignore) return;

            if (DisposalLock == DisposalLock.ThrowOnDispose) throw new InvalidOperationException("This Container has DisposalLock = DisposalLock.ThrowOnDispose and cannot be disposed until the lock is cleared");

            if (_hasDisposed) return;
            _hasDisposed = true;

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
            return (T) GetInstance(typeof(T));
        }

        public T GetInstance<T>(string name)
        {
            return (T) GetInstance(typeof(T), name);
        }

        public object GetInstance(Type serviceType)
        {
            assertNotDisposed();
            var resolver = ServiceGraph.FindResolver(serviceType);
            
            if (resolver == null)
            {
                throw new BlueMilkMissingRegistrationException(serviceType);
            }
            
            return resolver.Resolve(this);
        }

        public object GetInstance(Type serviceType, string name)
        {
            assertNotDisposed();

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
            assertNotDisposed();
            var resolver = ServiceGraph.FindResolver(serviceType);
            return resolver?.Resolve(this) ?? null;
        }

        public object TryGetInstance(Type serviceType, string name)
        {
            assertNotDisposed();
            var resolver = ServiceGraph.FindResolver(serviceType, name);
            return resolver?.Resolve(this) ?? null;
        }

        public T QuickBuild<T>()
        {
            return (T) QuickBuild(typeof(T));

        }

        public object QuickBuild(Type objectType)
        {
            assertNotDisposed();
            
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



        public IReadOnlyList<T> GetAllInstances<T>()
        {
            assertNotDisposed();
            return ServiceGraph.FindAll(typeof(T)).Select(x => x.Resolver.Resolve(this)).OfType<T>().ToList();
        }

        public IEnumerable GetAllInstances(Type serviceType)
        {
            assertNotDisposed();
            return ServiceGraph.FindAll(serviceType).Select(x => x.Resolver.Resolve(this)).ToArray();
        }


        object ISupportRequiredService.GetRequiredService(Type serviceType)
        {
            return GetInstance(serviceType);
        }

        IServiceScope IServiceScopeFactory.CreateScope()
        {
            assertNotDisposed();
            return new Scope(ServiceGraph);
        }
        

        public string WhatDoIHave(Type serviceType = null, Assembly assembly = null, string @namespace = null,
            string typeName = null)
        {
            assertNotDisposed();

            var writer = new WhatDoIHaveWriter(ServiceGraph);
            return writer.GetText(new ModelQuery
            {
                Assembly = assembly,
                Namespace = @namespace,
                ServiceType = serviceType,
                TypeName = typeName
            });
        }
        
        /// <summary>
        /// Returns a textual report of all the assembly scanners used to build up this Container
        /// </summary>
        /// <returns></returns>
        public string WhatDidIScan()
        {
            assertNotDisposed();
            
            var scanners = Model.Scanners;

            if (!scanners.Any()) return "No type scanning in this Container";

            var writer = new StringWriter();
            writer.WriteLine("All Scanners");
            writer.WriteLine("================================================================");

            scanners.Each(scanner =>
            {
                scanner.Describe(writer);

                writer.WriteLine();
                writer.WriteLine();
            });

            var failed = TypeRepository.FailedAssemblies();
            if (failed.Any())
            {
                writer.WriteLine();
                writer.WriteLine("Assemblies that failed in the call to Assembly.GetExportedTypes()");
                failed.Each(assem =>
                {
                    writer.WriteLine("* " + assem.Record.Name);
                });
            }
            else
            {
                writer.WriteLine("No problems were encountered in exporting types from Assemblies");
            }

            return writer.ToString();
        }

        public void CompileWithInlineServices(GeneratedAssembly assembly)
        {
            assembly.CompileAll(ServiceGraph);
        }

        public string GenerateCodeWithInlineServices(GeneratedAssembly assembly)
        {
            return assembly.GenerateCode(ServiceGraph);
        }


    }
}