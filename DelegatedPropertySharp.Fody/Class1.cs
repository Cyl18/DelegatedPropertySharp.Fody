using System;
using System.Reflection;

namespace DelegatedPropertySharp.Fody
{
    public class Class1
    {
        public static string b { get; }

        public string A()
        {
            return "";
            //return (string)((DelegatedPropertyAttributeBase)(typeof(Class1).GetProperty("b").GetCustomAttribute(typeof(DelegatedPropertyAttributeBase)))).Getter();
        }
    }
}
