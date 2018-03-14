using System;
using System.IO;
using System.Reflection;
using System.Text;
using TypeLite;
using TypeLite.Net4;
using TypeLite.TsModels;

namespace TypeScriptComandline
{
    class Program
    {
        private static string applicationDirectory;
        static int Main(string[] args)
        {

            var executingAssembly = Assembly.GetExecutingAssembly();
            var version = executingAssembly.GetName().Version;
            var executableName = executingAssembly.GetName().Name;


            Console.WriteLine("TypeScript definition file generator.");
            Console.WriteLine($"Version { version }, runtime version { executingAssembly.ImageRuntimeVersion}");
            if (args.Length != 2)
            {
                Console.WriteLine();
                Console.WriteLine("This utility extract the models from assemblies marked with attribute [TsClass]");
                Console.WriteLine("and converts them to TypeScript definition format.");
                Console.WriteLine();
                Console.WriteLine($"Usage: {executableName} [inputfile] [outputfile]");
                Console.WriteLine();
                Console.WriteLine($"Example: {executableName} MyFile.exe MyFile.ts.d");
                return -1;
            }
            else
            {
                try
                {
                    if (!File.Exists(args[0]))
                        throw new FileNotFoundException($"File {args[0]} not found.");

                    // We want to load the provided assembly and also the referenced once from the same folder.
                    // So a little hook will do the trick.
                    applicationDirectory = Path.GetDirectoryName(args[0]);
                    AppDomain currentDomain = AppDomain.CurrentDomain;
                    currentDomain.AssemblyResolve += currentDomain_AssemblyResolve;

                    Console.WriteLine($"Loading assembly {args[0]}");
                    Assembly.LoadFile(args[0]);

                    var ts = TypeScript.Definitions()
                        .ForLoadedAssemblies()
                        .WithIndentation("    ")
                        .WithVisibility(VisibilityFormatter) // All interfaces should be exported
                        .WithMemberFormatter((identifier) => Char.ToLower(identifier.Name[0]) + identifier.Name.Substring(1)) // Convert to camelCase
                        .WithTypeFormatter(TypeFormatter) //Prefix classname with I and convert GUID to string
                        .AsConstEnums(false)
                        .WithConvertor<Guid>(c => "string")
                        .WithModuleNameFormatter(module => module.Name = module.Name.Replace(".", "_")); // Convert Guids to string

                    Console.WriteLine("Generate typescript.");
                    var generatedTypeScript = ts.Generate();

                    // According to ES6 we need to export the module, its not a nice way but TypeLite is not able to change this behaviour (line 246 in TsGenerator.cs)
                    generatedTypeScript = generatedTypeScript.Replace("declare namespace ", "export module ");

                    Console.WriteLine("Writing to file.");
                    using (StreamWriter sw = new StreamWriter(File.Open(args[1], FileMode.Create), Encoding.UTF8))
                    {
                        sw.WriteLine("// Do not modify this file!");
                        sw.Write(generatedTypeScript);
                    }
                    Console.WriteLine("Done.");

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred : {ex.Message}");
                }
                return -2;
            }
        }

        private static bool VisibilityFormatter(TsClass tsClass, string typeName)
        {
            return true;
        }

        private static string TypeFormatter(TsType tsType, ITsTypeFormatter f)
        {
            TsClass tsClass = (TsClass)tsType;

            //Prefix classname with I
            return "I" + tsClass.Name;
        }

        static Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.
            string[] fields = args.Name.Split(',');
            string assemblyName = fields[0];
            string assemblyCulture;
            if (fields.Length < 2)
                assemblyCulture = null;
            else
                assemblyCulture = fields[2].Substring(fields[2].IndexOf('=') + 1);

            string assemblyFileName = assemblyName + ".dll";
            string assemblyPath;

            if (assemblyName.EndsWith(".resources"))
            {
                // Specific resources are located in app subdirectories
                string resourceDirectory = Path.Combine(applicationDirectory, assemblyCulture);
                assemblyPath = Path.Combine(resourceDirectory, assemblyFileName);
            }
            else
            {
                assemblyPath = Path.Combine(applicationDirectory, assemblyFileName);
            }

            if (File.Exists(assemblyPath))
            {
                //Load the assembly from the specified path.
                Console.WriteLine($"Loading referenced assembly {assemblyPath}");
                Assembly loadingAssembly = Assembly.LoadFrom(assemblyPath);

                //Return the loaded assembly.
                return loadingAssembly;
            }
            else
            {
                return null;
            }

        }
    }
}