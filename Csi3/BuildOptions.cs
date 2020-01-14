using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Csi3
{
    public class BuildOptions
    {
        public bool EnableDebug { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;
        public IEnumerable<string> IncludePaths { get; set; } = Array.Empty<string>();
        public IEnumerable<string> LibraryPaths { get; set; } = Array.Empty<string>();

        internal SourceCodeKind GetKind()
            => SourceCodeKind.Regular;

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
