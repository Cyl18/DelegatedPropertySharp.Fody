using System;
using System.Reflection;
using DelegatedPropertySharp;

namespace Test.AssemblyToInject
{
    public class Class1
    {
        static Class1()
        {
            _a = new Handler<Class1, int>();
            _a.PropertyInfo = null;
        }
        static Handler<Class1, int> _a;
        [Attrib]
        public int Fork { get; set; }
    }

    public class Attrib : DelegatedPropertyAttributeBase
    {
        static Handler<object, object> a;
        static Attrib()
        {
            a = new Handler<object, object>();

        }
    }

    
    public class Handler<TThis, TProperty> : IDelegatedPropertyHandler<TThis, TProperty, Attrib>
    {
        public PropertyInfo PropertyInfo { get; set; }

        public TProperty Get(TThis @this)
        {
            throw new NotImplementedException();
        }

        public void Set(TThis @this, TProperty value)
        {
            throw new NotImplementedException();
        }
    }
}
