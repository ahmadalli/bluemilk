﻿using System.Diagnostics;
using BlueMilk.IoC;
using Xunit;

namespace BlueMilk.Testing.IoC.Acceptance
{
    public class do_not_blow_up_with_bi_directional_dependencies
    {
        public do_not_blow_up_with_bi_directional_dependencies()
        {
            container = new Container(x =>
            {
                x.For<IBiView>().Use<BiView>();
                x.For<IBiPresenter>().Use<BiPresenter>();

                x.For<IBiGrandparent>().Use<BiGrandparent>();
                x.For<IBiHolder>().Use<BiHolder>();
                x.For<IBiLeaf>().Use<BiLeaf>();
            });
        }

        private readonly Container container;

        [Fact]
        public void do_not_blow_up_with_a_stack_overflow_problem()
        {
            var ex =
                Exception<BlueMilkException>.ShouldBeThrownBy(() =>
                {
                    container.GetInstance<IBiPresenter>();
                });

            ex.Message.ShouldContain("Bi-directional dependencies detected");
        }

        [Fact]
        public void do_not_blow_up_with_a_stack_overflow_problem_2()
        {
            var ex =
                Exception<BlueMilkException>.ShouldBeThrownBy(() => { container.GetInstance<IBiHolder>(); });

            ex.Message.ShouldContain("Bi-directional dependencies detected");
        }
    }

    public interface IBiHolder
    {
    }

    public interface IBiGrandparent
    {
    }

    public interface IBiLeaf
    {
    }

    public class BiHolder : IBiHolder
    {
        public BiHolder(IBiGrandparent grandparent)
        {
        }
    }

    public class BiGrandparent : IBiGrandparent
    {
        public BiGrandparent(IBiLeaf leaf)
        {
        }
    }

    public class BiLeaf : IBiLeaf
    {
        public BiLeaf(IBiHolder holder)
        {
        }
    }

    public interface IBiView
    {
    }

    public interface IBiPresenter
    {
    }

    public class BiView : IBiView
    {
        private readonly IBiPresenter _presenter;

        public BiView(IBiPresenter presenter)
        {
            _presenter = presenter;
        }

        public IBiPresenter Presenter
        {
            get { return _presenter; }
        }
    }

    public class BiPresenter : IBiPresenter
    {
        private readonly IBiView _view;

        public BiPresenter(IBiView view)
        {
            _view = view;
        }

        public IBiView View
        {
            get { return _view; }
        }
    }
}