﻿using Csi3.Build;
using Csi3.Contexts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Csi3
{
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

        public async Task<ScriptExecutionContext> ExecuteAsync(string[] args)
            => await Task.Factory.StartNew(() =>
            {
                using (_peStream)
                using (_pdbStream)
                {
                    var loadContext = new ScriptAssemblyLoadContext();
                    var assembly = loadContext.LoadFromStream(_peStream, _pdbStream);

                    assembly.EntryPoint.Invoke(null, new[] { args });

                    loadContext.Unload();

                    return new ScriptExecutionContext(new WeakReference(loadContext));
                }
            })
            .ConfigureAwait(false);

        private bool _disposed;
        private Stream _peStream;
        private Stream _pdbStream;

    }
}
