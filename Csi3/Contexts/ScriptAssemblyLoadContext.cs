using System.Reflection;
using System.Runtime.Loader;

namespace Csi3.Contexts
{
    class ScriptAssemblyLoadContext : AssemblyLoadContext
    {
        public ScriptAssemblyLoadContext()
            : base(isCollectible: true)
        { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }

        //private AssemblyDependencyResolver _resolver;
    }
}
