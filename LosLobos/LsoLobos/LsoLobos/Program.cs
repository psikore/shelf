using System;
using System.IO;
using System.Reflection;
using System.Web.UI;

namespace LsoLobos
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string payload = File.ReadAllText("payload.txt");

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;


            try
            {
                var los = new LosFormatter();
                object obj = los.Deserialize(payload);
                Console.WriteLine("type: " + obj.GetType().FullName);

                DumpObject(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Redirect binding to a stub assembly if needed
            if (args.Name.StartsWith("MyAssembly"))
            {
                // Load a stub assembly you've built with placeholder classes
                return Assembly.LoadFrom("StubAssembly.dll");
            }
            return null;
        }

        static void DumpObject(object obj, int indent = 0)
        {
            if (obj == null) { return; }
            string pad = new string(' ', indent);
            Console.WriteLine($"{pad} {obj.GetType().FullName}");
            foreach (var prop in obj.GetType().GetProperties())
            {
                try 
                {
                    var val = prop.GetValue(obj, null);
                    Console.WriteLine($"{pad} {prop.Name} = {val}");
                }
                catch { }
            }
        }
    }
}

//Stub class
namespace MyNamespace
{
    public class PayloadClassFromFile
    {
        // empty - just enough for LosFormatter to bind
    }
}
