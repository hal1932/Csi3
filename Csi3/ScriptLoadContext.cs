﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Csi3
{
    class ScriptLoadContext : AssemblyLoadContext
    {
        public ScriptLoadContext()
            : base(isCollectible: true)
        { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }

        private AssemblyDependencyResolver _resolver;
    }
}
