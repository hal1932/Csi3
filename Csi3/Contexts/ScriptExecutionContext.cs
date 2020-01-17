using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Csi3.Contexts
{
    public class ScriptExecutionContext
    {
        internal ScriptExecutionContext(WeakReference loadContextRef)
        {
            _loadContextRef = loadContextRef;
        }

        public void WaitForExit()
            => WaitForExit(TimeSpan.MaxValue);

        public void WaitForExit(TimeSpan timeout)
        {
            var start = DateTime.Now;
            while (_loadContextRef.IsAlive && DateTime.Now - start < timeout)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(50);
            }
        }

        private WeakReference _loadContextRef;
    }
}
