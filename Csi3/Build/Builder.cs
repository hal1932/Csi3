using ConsoleAppFramework;
using Csi3.Build.Resolvers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Csi3.Build
{
    class WriteLockList<T> : List<T>
    {
        public new void Add(T item)
        {
            lock (_lock)
            {
                base.Add(item);
            }
        }

        public new void AddRange(IEnumerable<T> items)
        {
            lock (_lock)
            {
                base.AddRange(items);
            }
        }

        private object _lock = new object();
    }

    public class Builder
    {
        public IEnumerable<string> SourceCodePaths => _sourceCodePaths;

        public Builder(BuildOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<Executer> BuildAsync(string scriptPath)
            => await Task.Factory.StartNew(() =>
            {
                scriptPath = Path.GetFullPath(scriptPath);

                var reader = new SourceCodeWalker(_options, _logger);
                if (!reader.Walk(scriptPath))
                {
                    return null;
                }

                _sourceCodePaths = reader.SourceCodes.Select(code => code.FilePath).ToArray();

                var syntaxTrees = new WriteLockList<SyntaxTree>();
                {
                    var parseOptions = new CSharpParseOptions(
                        kind: _options.SourceCodeKind,
                        preprocessorSymbols: _options.GetPreprocessorSymbols()
                        );

                    Parallel.ForEach(reader.SourceCodes, sourceCode =>
                    {
                        var tree = CSharpSyntaxTree.ParseText(
                            sourceCode.Text,
                            parseOptions,
                            sourceCode.FilePath
                            );

                        if (_options.EnableDebug)
                        {
                            tree = CSharpSyntaxTree.Create(tree.GetRoot() as CSharpSyntaxNode, path: sourceCode.FilePath, encoding: _options.Encoding);
                        }

                        syntaxTrees.Add(tree);
                    });
                }

                IEnumerable<MetadataReference> references;
                {
                    using (var metadataLoadContext = new MetadataLoadContext(new ScriptAssemblyResolver(searchPaths: _options.ReferencePaths)))
                    {
                        references = Assembly.GetExecutingAssembly()
                            .GetReferencedAssemblies()
                            .Select(name => metadataLoadContext.LoadFromAssemblyName(name).Location)
                            .Select(path => MetadataReference.CreateFromFile(path))
                            .ToArray();
                    }
                }

                CSharpCompilation compilation;
                {
                    var compileOptions = new CSharpCompilationOptions(
                        OutputKind.ConsoleApplication,
                        optimizationLevel: _options.GetOptimizationLevel(),
                        sourceReferenceResolver: ScriptSourceResolver.Default
                            .WithBaseDirectory(Path.GetDirectoryName(scriptPath))
                            .WithSearchPaths(_options.LoadPaths.Concat(new[] { _options.WorkingDirectory })),
                        metadataReferenceResolver: ScriptReferenceResolver.Default
                            .WithSearchPaths(_options.ReferencePaths.Concat(new[] { _options.WorkingDirectory, RuntimeEnvironment.GetRuntimeDirectory() }))
                        ); ;

                    compilation = CSharpCompilation.Create(
                        Path.GetFileNameWithoutExtension(scriptPath),
                        syntaxTrees: syntaxTrees,
                        references: references,
                        options: compileOptions
                        );
                }

                Stream peStream = new MemoryStream();
                Stream pdbStream = null;
                {
                    EmitOptions emitOptions = null;
                    IEnumerable<EmbeddedText> embeddedTexts = null;

                    if (_options.EnableDebug)
                    {
                        pdbStream = new MemoryStream();
                        emitOptions = new EmitOptions(
                            debugInformationFormat: DebugInformationFormat.PortablePdb,
                            pdbFilePath: Path.ChangeExtension(scriptPath, ".pdb")
                            );

                        embeddedTexts = reader.SourceCodes.Select(code => code.CreateEmbeddedText(true));
                    }

                    var emitResult = compilation.Emit(
                        peStream,
                        pdbStream: pdbStream,
                        options: emitOptions,
                        embeddedTexts: embeddedTexts
                        );

                    if (!emitResult.Success)
                    {
                        foreach (var diag in emitResult.Diagnostics)
                        {
                            _logger.LogError(diag.ToString());
                        }
                        return null;
                    }

                    peStream.Seek(0, SeekOrigin.Begin);
                    pdbStream?.Seek(0, SeekOrigin.Begin);
                }

                return new Executer(peStream, pdbStream);
            })
            .ConfigureAwait(false);

        private BuildOptions _options;
        private ILogger _logger;
        private string[] _sourceCodePaths = Array.Empty<string>();
    }
}
