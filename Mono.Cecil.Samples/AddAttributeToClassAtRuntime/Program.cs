using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Newtonsoft.Json;

namespace AddAttributeToClassAtRuntime
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string outputFileName = string.Empty;
            var directoryInfo = Directory.GetParent(Directory.GetCurrentDirectory()).Parent;
            if (directoryInfo != null)
            {
                if (directoryInfo.Parent != null)
                {
                    outputFileName = Path.Combine(directoryInfo.Parent.FullName, @"ClassLibrary1\bin\Debug\ClassLibrary1.dll");
                }
            }

            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            if (directoryName != null)
            {

                // get the assembly given the path name, and get the main module 
                var assembly = AssemblyDefinition.ReadAssembly(outputFileName);
                var module = assembly.MainModule;

                // Foo is the class that will get the attribute 
                var foo = module.Types.First(x => x.Name == "Person");

                if (foo.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == "System.SerializableAttribute").ToList().Count == 0)
                {
                    if (File.Exists(outputFileName))
                    {
                        File.Delete(outputFileName);
                    }

                    var attCtor = module.Import(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes));
                    var custatt = new CustomAttribute(attCtor);
                    foo.CustomAttributes.Add(custatt);

                    assembly.Write(outputFileName);
                }

                UseDynamics(outputFileName);
                TypeLoad(outputFileName);
            }

            Console.Read();
        }

        private static void TypeLoad(string outputFileName)
        {
            Assembly loadAssembly = LoadAssembly(outputFileName);
            object instance = loadAssembly.CreateInstance("ClassLibrary1.Person");

            if (instance != null)
            {
                Type type = instance.GetType();
                PropertyInfo prop = type.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);

                if (null != prop && prop.CanWrite)
                {
                    prop.SetValue(instance, "TypeLoad", null);
                }

                Console.WriteLine(JsonConvert.SerializeObject(instance));

                Console.WriteLine("Attributes");
                IEnumerable<SerializableAttribute> customAttributes = type.GetCustomAttributes(typeof(SerializableAttribute), true) as IEnumerable<SerializableAttribute>;
                if (customAttributes != null)
                    Console.WriteLine("Attribute Count: {0}", customAttributes.Count());
            }
        }
        private static void UseDynamics(string outputFileName)
        {
            Assembly loadAssembly = LoadAssembly(outputFileName);
            dynamic instance = loadAssembly.CreateInstance("ClassLibrary1.Person");

            if (instance != null)
            {
                instance.Name = "UseDynamics";
                Console.WriteLine(JsonConvert.SerializeObject(instance));
            }
        }

        private static string _asmBase;

        public static Assembly LoadAssembly(string assemblyName)
        {
            _asmBase = Path.GetDirectoryName(assemblyName);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return Assembly.Load(File.ReadAllBytes(assemblyName));
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            string strTempAssmbPath = "";
            var objExecutingAssemblies = args.RequestingAssembly;
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.                
                    strTempAssmbPath = _asmBase + "\\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }

            }
            //Load the assembly from the specified path.                    
            var assembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return assembly;
        }
    }
}
