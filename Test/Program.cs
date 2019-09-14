using System;
using System.IO;
using System.Reflection.Emit;
using DelegatedPropertySharp.Fody;
using Fody;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var weavingTask = new ModuleWeaver();
            var result = weavingTask.ExecuteTestRun("Test.AssemblyToInject.dll",true);
            var resultAssemblyPath = result.AssemblyPath;
            Console.WriteLine(resultAssemblyPath);
        }
    }
}
