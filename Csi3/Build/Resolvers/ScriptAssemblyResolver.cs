using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Csi3.Build.Resolvers
{
    static class StringExtensions
    {
        public static bool AssemblyNameEquals(this string name, string other)
            => name.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    class ScriptAssemblyResolver : MetadataAssemblyResolver
    {
        public static ScriptAssemblyResolver Default { get; } = new ScriptAssemblyResolver();

        public ScriptAssemblyResolver(IEnumerable<string> searchPaths = null)
        {
            _searchPaths = searchPaths?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }

        public ScriptAssemblyResolver WithSearchPaths(params string[] paths)
            => new ScriptAssemblyResolver(searchPaths: paths);

        public ScriptAssemblyResolver WithSearchPaths(IEnumerable<string> paths)
            => new ScriptAssemblyResolver(searchPaths: paths);

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            var name = assemblyName.Name;

            // https://github.com/search?q=CoreMetadataAssemblyResolver&type=Code
            if (name.AssemblyNameEquals("mscorlib") ||
                name.AssemblyNameEquals("System.Private.CoreLib") ||
                name.AssemblyNameEquals("System.Runtime") ||
                name.AssemblyNameEquals("netstandard") ||
                name.AssemblyNameEquals("System.Runtime.InteropServices"))
            {
                if (_coreAssembly == default)
                {
                    _coreAssembly = context.LoadFromAssemblyPath(typeof(object).Assembly.Location);
                }
                Console.WriteLine($"  -> [CoreLib] {_coreAssembly.Location}");
                return _coreAssembly;
            }

            var dllPath = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), name + ".dll");
            if (File.Exists(dllPath))
            {
                //Console.WriteLine($"  -> [Runtime] {dllPath}");
                return context.LoadFromAssemblyPath(dllPath);
            }

            foreach (var searchPath in _searchPaths)
            {
                dllPath = Path.Combine(searchPath, name + ".dll");
                if (File.Exists(dllPath))
                {
                    //Console.WriteLine($"  -> [Path] {dllPath}");
                    return context.LoadFromAssemblyPath(dllPath);
                }
            }

            var resolver = new AssemblyDependencyResolver(Assembly.GetExecutingAssembly().Location);
            dllPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrEmpty(dllPath))
            {
                //Console.WriteLine($"  -> [AsmDep] {dllPath}");
                return context.LoadFromAssemblyPath(dllPath);
            }

            //Console.WriteLine($"[ERROR] assembly not found: {assemblyName.FullName}");
            return null;
        }

        private Assembly _coreAssembly;
        private ImmutableArray<string> _searchPaths = ImmutableArray<string>.Empty;
    }
}
