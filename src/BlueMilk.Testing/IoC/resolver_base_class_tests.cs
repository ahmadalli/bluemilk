﻿using BlueMilk.IoC;
using BlueMilk.IoC.Resolvers;
using Shouldly;
using Xunit;

namespace BlueMilk.Testing.IoC
{
    public class resolver_base_class_tests
    {
        public readonly Scope theScope = new Scope();
        
        [Fact]
        public void non_disposable_singleton()
        {
            var singleton1 = new Singleton1(theScope);

            var clock = singleton1.Resolve(new Scope());
            clock
                .ShouldBeSameAs(singleton1.Resolve(new Scope()));
            
            theScope.Disposables.ShouldNotContain(clock);
            theScope.TryFind(out IClock cachedClock).ShouldBeTrue();
            
            cachedClock.ShouldBeSameAs(clock);
                
        }
        
        [Fact]
        public void disposable_singleton()
        {
            var singleton1 = new DisposableSingleton(theScope);

            var clock = singleton1.Resolve(new Scope());
            clock
                .ShouldBeSameAs(singleton1.Resolve(new Scope()));
            
            theScope.Disposables.ShouldContain(clock);
            theScope.TryFind(out IClock cachedClock).ShouldBeTrue();
            
            cachedClock.ShouldBeSameAs(clock);
                
        }
        
        [Fact]
        public void scoped_resolver()
        {
            var resolver = new ScopedClock();

            var clock = resolver.Resolve(theScope);
            clock.ShouldBeSameAs(resolver.Resolve(theScope));

            clock.ShouldNotBeTheSameAs(resolver.Resolve(new Scope()));
            
            theScope.TryFind(out IClock cachedClock).ShouldBeTrue();
            cachedClock.ShouldBeSameAs(clock);
        }
        
        [Fact]
        public void scoped_resolver_will_track_disposables()
        {
            var resolver = new DisposableScopedClock();
            var clock = resolver.Resolve(theScope);
            
            theScope.Disposables.ShouldContain(clock);
        }
        
        [Fact]
        public void transient_resolver_behavior()
        {
            var resolver = new TransientClock();

            resolver.Resolve(theScope)
                .ShouldNotBeTheSameAs(resolver.Resolve(theScope));
        }
        
        [Fact]
        public void transient_disposables()
        {
            var resolver = new DisposableTransientClock();

            var clock = resolver.Resolve(theScope);
            
            theScope.Disposables.ShouldContain(clock);

        }
    }

    public class TransientClock : TransientResolver<IClock>
    {
        public override object Build(Scope scope)
        {
            return new Clock();
        }
    }

    public class DisposableTransientClock : TransientResolver<IClock>
    {
        public override object Build(Scope scope)
        {
            return new DisposableClock();
        }
    }

    public class ScopedClock : ScopedResolver<IClock>
    {
        public override object Build(Scope scope)
        {
            return new Clock();
        }
       
    }

    public class DisposableScopedClock : ScopedResolver<IClock>
    {
        public override object Build(Scope scope)
        {
            return new DisposableClock();
        }
    }

    public class Singleton1 : SingletonResolver<IClock>
    {
        public Singleton1(Scope topLevelScope) : base(topLevelScope)
        {
        }

        public override IClock Build(Scope scope)
        {
            return new Clock();
        }
    }
    
    public class DisposableSingleton : SingletonResolver<IClock>
    {
        public DisposableSingleton(Scope topLevelScope) : base(topLevelScope)
        {
        }

        public override IClock Build(Scope scope)
        {
            return new DisposableClock();
        }
    }
}