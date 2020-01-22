using ConsoleAppFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Csi3.Build
{
    class SourceCode
    {
        public string FilePath { get; }
        public string Text { get; }
        public Encoding Encoding { get; }

        internal SourceCode(string filePath, string text, Encoding encoding)
        {
            FilePath = filePath;
            Text = text;
            Encoding = encoding;
        }

        public byte[] GetBytes()
            => Encoding.GetBytes(Text);

        public EmbeddedText CreateEmbeddedText(bool canBeEmbedded)
        {
            var bytes = GetBytes();
            var sourceText = SourceText.From(bytes, bytes.Length, Encoding, canBeEmbedded: canBeEmbedded);
            return EmbeddedText.FromSource(FilePath, sourceText);
        }
    }

    class SourceCodeWalker
    {
        public IEnumerable<SourceCode> SourceCodes => _sourceCodes;
        public IEnumerable<string> ReferencePaths => _referencePaths;

        public SourceCodeWalker(BuildOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public bool Walk(string rootSourceCodePath)
        {
            _sourceCodes.Clear();
            _referencePaths.Clear();

            var visitedFiles = new HashSet<string>();

            var sourceCodePaths = new Stack<string>();
            sourceCodePaths.Push(rootSourceCodePath);

            var sourceCodeRoot = Path.GetDirectoryName(rootSourceCodePath);

            while (sourceCodePaths.Any())
            {
                var filePath = sourceCodePaths.Pop();

                var sourceCodePath = FindSourceCodeFilePath(filePath, sourceCodeRoot);
                if (string.IsNullOrEmpty(sourceCodePath))
                {
                    _logger.LogError($"ファイルが見つかりません: {filePath}");
                    return false;
                }

                if (!visitedFiles.Add(sourceCodePath))
                {
                    continue;
                }

                var pp = new Preprocessor(sourceCodePath, _options.Encoding);
                pp.Preprocess(waitForUnlock: _options.SourceFilesMayBeLocked);
                foreach (var loadedPath in pp.Loads)
                {
                    sourceCodePaths.Push(loadedPath);
                }

                foreach (var referencePath in pp.References)
                {
                    _referencePaths.Add(referencePath);
                }

                _sourceCodes.Add(new SourceCode(sourceCodePath, pp.SourceCode.ToString(), _options.Encoding));
            }

            return true;
        }

        private string FindSourceCodeFilePath(string filePath, string rootDirectoryPath)
        {
            if (File.Exists(filePath))
            {
                return Path.GetFullPath(filePath);
            }

            var sourceCodePathCandidate = Path.Combine(rootDirectoryPath, filePath);
            if (File.Exists(sourceCodePathCandidate))
            {
                return sourceCodePathCandidate;
            }

            foreach (var includePath in _options.LoadPaths)
            {
                sourceCodePathCandidate = Path.Combine(includePath, filePath);
                if (File.Exists(sourceCodePathCandidate))
                {
                    return sourceCodePathCandidate;
                }
            }

            return null;
        }

        private BuildOptions _options;
        private ILogger _logger;
        private List<SourceCode> _sourceCodes = new List<SourceCode>();
        private HashSet<string> _referencePaths = new HashSet<string>();
    }
}
