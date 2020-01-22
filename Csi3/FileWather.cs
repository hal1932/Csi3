using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Csi3
{
    class FileWather : IDisposable
    {
        ~FileWather()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop();
                Clear();
            }
        }

        public void AddFile(string filePath)
        {
            var watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(filePath),
                Filter = Path.GetFileName(filePath),
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.LastWrite,
            };
            watcher.Changed += OnChangedFile;
            _watchers.Add(watcher);
        }

        public void AddFiles(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                AddFile(filePath);
            }
        }

        public void Clear()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Changed -= OnChangedFile;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        public void Start()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        public void Stop()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
            }
        }

        public void WaitForChanged(CancellationToken token)
        {
            _waitEvent.Wait(token);
            _waitEvent.Reset();
        }

        private void OnChangedFile(object sender, FileSystemEventArgs e)
        {
            var file = new FileInfo(e.FullPath);
            if (_lastChangedTimes.TryGetValue(file.FullName, out var lastWriteTime))
            {
                if (file.LastWriteTime <= lastWriteTime)
                {
                    return;
                }
            }
            _lastChangedTimes[file.FullName] = file.LastWriteTime;

            while (file.IsReadLocked())
            {
                Thread.Sleep(50);
            }

            _waitEvent.Set();
        }

        private bool _disposed;
        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private Dictionary<string, DateTime> _lastChangedTimes = new Dictionary<string, DateTime>();
        private ManualResetEventSlim _waitEvent = new ManualResetEventSlim(false);
    }
}
