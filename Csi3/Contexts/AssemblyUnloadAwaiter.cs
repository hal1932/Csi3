using System;
using System.Threading;
using System.Threading.Tasks;

namespace Csi3.Contexts
{
    public class AssemblyUnloadAwaiter
    {
        public bool IsUnloaded => !_loadContextRef.IsAlive;

        internal AssemblyUnloadAwaiter(WeakReference loadContextRef)
        {
            _loadContextRef = loadContextRef;
        }

        public bool WaitForUnload()
            => WaitForUnload(TimeSpan.MaxValue);

        public bool WaitForUnload(TimeSpan timeout)
        {
            var start = DateTime.Now;
            while (_loadContextRef.IsAlive && DateTime.Now - start < timeout)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(50);
            }
            return !_loadContextRef.IsAlive;
        }

        public async Task<bool> WaitForUnloadAsync(CancellationToken token)
            => await WaitForUnloadAsync(TimeSpan.MaxValue, token).ConfigureAwait(false);

        public async Task<bool> WaitForUnloadAsync(TimeSpan timeout, CancellationToken token)
            => await Task.Factory.StartNew(() =>
            {
                var start = DateTime.Now;
                while (_loadContextRef.IsAlive && DateTime.Now - start < timeout && !token.IsCancellationRequested)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(50);
                }
                return !_loadContextRef.IsAlive;
            })
            .ConfigureAwait(false);

        private WeakReference _loadContextRef;
    }
}
