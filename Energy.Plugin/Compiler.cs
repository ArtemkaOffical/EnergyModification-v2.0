using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Text;
using Energy.Plugins;

namespace Energy
{
    public static class Compiler
    {
        private static List<PortableExecutableReference> _references = new List<PortableExecutableReference>();

        private static bool AddAssembly(string assemblyDll)
        {
            if (string.IsNullOrEmpty(assemblyDll)) return false;

            var file = Path.GetFullPath(assemblyDll);

            if (!File.Exists(file))
            {
                var path = Path.GetDirectoryName(typeof(object).Assembly.Location);
                file = Path.Combine(path, assemblyDll);
                if (!File.Exists(file))
                    return false;
            }

            if (_references.Any(r => r.FilePath == file)) return true;

            try
            {
                var reference = MetadataReference.CreateFromFile(file);
                _references.Add(reference);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool AddAssembly(Type type)
        {
            try
            {
                if (_references.Any(r => r.FilePath == type.Assembly.Location))
                    return true;

                var systemReference = MetadataReference.CreateFromFile(type.Assembly.Location);
                _references.Add(systemReference);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static void AddNetCoreDefaultReferences()
        {
            var rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                         Path.DirectorySeparatorChar;

            AddAssembly(rtPath + "System.Private.CoreLib.dll");
            AddAssembly(rtPath + "System.Runtime.dll");
            AddAssembly(rtPath + "System.Console.dll");
            AddAssembly(rtPath + "netstandard.dll");
            AddAssembly(rtPath + "System.dll");


            AddAssembly(rtPath + "System.Text.RegularExpressions.dll"); // IMPORTANT!
            AddAssembly(rtPath + "System.Linq.dll");
            AddAssembly(rtPath + "System.Linq.Expressions.dll"); // IMPORTANT!

            AddAssembly(rtPath + "System.IO.dll");
            AddAssembly(rtPath + "System.Net.Primitives.dll");
            AddAssembly(rtPath + "System.Net.Http.dll");
            AddAssembly(rtPath + "System.Private.Uri.dll");
            AddAssembly(rtPath + "System.Reflection.dll");
            AddAssembly(rtPath + "System.ComponentModel.Primitives.dll");
            AddAssembly(rtPath + "System.Globalization.dll");
            AddAssembly(rtPath + "System.Collections.Concurrent.dll");
            AddAssembly(rtPath + "System.Collections.NonGeneric.dll");
        }

        //compile cs from file to plugin at runtime
        public static Plugin? Compile(string filePath)
        {
            AddNetCoreDefaultReferences();

            AddAssembly("Energy.dll");
            AddAssembly("Energy.Plugin.dll");
            AddAssembly("Energy.Attributes.dll");
            AddAssembly("Newtonsoft.Json.dll");
            
            var files = Directory.GetFiles("Libraries");

            foreach (var file in files)
            {
                AddAssembly(file);
            }

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(File.ReadAllText(filePath));

            var compilation = CSharpCompilation.Create(Path.GetFileName(filePath))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release))
                .WithReferences(_references)
                .AddSyntaxTrees(tree);

            string errorMessage = null;
            Assembly assembly = null;
            Stream codeStream = null;

            using (codeStream = new MemoryStream())
            {
                EmitResult compilationResult = null;
                compilationResult = compilation.Emit(codeStream);

                if (!compilationResult.Success)
                {
                    var sb = new StringBuilder();

                    foreach (var diag in compilationResult.Diagnostics)
                        sb.AppendLine(diag.ToString());

                    errorMessage = sb.ToString();
                    Console.WriteLine(errorMessage);
                    return null;
                }

                _references.Clear();

                assembly = Assembly.Load(((MemoryStream)codeStream).ToArray());

                var types = assembly.GetExportedTypes();

                if (types.Length == 0 || types[0]?.BaseType?.BaseType != typeof(Plugin))
                    return null;

                if (types[0].Name != Path.GetFileNameWithoutExtension(filePath))
                {
                    Console.WriteLine($"{assembly.GetName().Name} | File name should be {types[0].Name}");
                    return null;                  
                }

                Type FileClass = assembly.GetType($"{types[0].FullName}");

                if(FileClass == null) 
                    return null;

               // object obj = FileClass.GetConstructor(new Type[0]).Invoke(new object[0]);
                Plugin plugin = (Plugin)Activator.CreateInstance(FileClass);
                return plugin;

            }
        }

    }
}
