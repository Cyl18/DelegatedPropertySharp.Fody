using System;
using System.Reflection;
using DelegatedPropertySharp;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace ExecutionTest
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
    public class Class1
    {
        [Lazy] public int LazyObject1 { get; }
        [Lazy] public object LazyObject2 { get; set; }
    }

    public class Lazy : DelegatedPropertyAttributeBase
    {
    }

    public class Handler<TThis, TProperty> : IDelegatedPropertyHandler<TThis, TProperty, Lazy>
    {
        public PropertyInfo PropertyInfo { get; set; }
        readonly Lazy<TProperty> _lazy = new Lazy<TProperty>();

        public TProperty Get(TThis @this)
        {
            return _lazy.Value;
        }

        public void Set(TThis @this, TProperty value)
        {
            throw new InvalidOperationException();
        }
    }
}
