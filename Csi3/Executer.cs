using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Csi3
{
    public class ExecutionContext
    {
        internal ExecutionContext(WeakReference loadContextRef)
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
                Thread.Sleep(100);
            }
        }

        private WeakReference _loadContextRef;
    }

    public class Executer : IDisposable
    {
        internal Executer(Stream peStream, Stream pdbStream)
        {
            _peStream = peStream;
            _pdbStream = pdbStream;
        }

        ~Executer()
            => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _peStream?.Dispose();
                _peStream = null;

                _pdbStream?.Dispose();
                _pdbStream = null;

                _disposed = true;
            }
        }

        public async Task<ExecutionContext> ExecuteAsync(params string[] args)
            => await Task.Factory.StartNew(() =>
            {
                using (_peStream)
                using (_pdbStream)
                {
                    var loadContext = new ScriptLoadContext();
                    var assembly = loadContext.LoadFromStream(_peStream, _pdbStream);

                    assembly.EntryPoint.Invoke(null, new[] { args });

                    loadContext.Unload();

                    return new ExecutionContext(new WeakReference(loadContext));
                }
            })
            .ConfigureAwait(false);

        private bool _disposed;
        private Stream _peStream;
        private Stream _pdbStream;
    }
}
