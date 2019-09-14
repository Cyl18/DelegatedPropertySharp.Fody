using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DelegatedPropertySharp
{

    /// <summary>
    /// 万物之宗。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class DelegatedPropertyAttributeBase : Attribute
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public interface IDelegatedPropertyHandler<TThis, TProperty, TAttribute>
    {
        PropertyInfo PropertyInfo { get; set; }
        TProperty Get(TThis @this);
        void Set(TThis @this, TProperty value);
    }

}
