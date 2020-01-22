using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Csi3.Build
{
    public class BuildOptions
    {
        public SourceCodeKind SourceCodeKind { get; set; }
        public bool EnableDebug { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;
        public IEnumerable<string> LoadPaths { get; set; } = Array.Empty<string>();
        public IEnumerable<string> ReferencePaths { get; set; } = Array.Empty<string>();
        public bool SourceFilesMayBeLocked { get; set; } = false;

        internal IEnumerable<string> GetPreprocessorSymbols()
        {
            var symbols = new List<string>();
            if (EnableDebug)
            {
                symbols.Add("DEBUG");
            }
            return symbols;
        }

        internal OptimizationLevel GetOptimizationLevel()
            => EnableDebug ? OptimizationLevel.Debug : OptimizationLevel.Release;
    }
}
