using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Csi3
{
    public class Builder
    {
        public Builder(BuildOptions options)
        {
            _options = options;
        }

        public async Task<Executer> BuildAsync(string sourceCodePath)
            => await Task.Factory.StartNew(() =>
            {
                sourceCodePath = Path.GetFullPath(sourceCodePath);

                var sourceCodeText = File.ReadAllText(sourceCodePath);
                var sourceCodeBuffer = _options.Encoding.GetBytes(sourceCodeText);
                var sourceText = SourceText.From(sourceCodeBuffer, sourceCodeBuffer.Length, encoding: _options.Encoding, canBeEmbedded: _options.EnableDebug);

                var parseOptions = new CSharpParseOptions(
                    kind: _options.GetKind(),
                    preprocessorSymbols: _options.GetPreprocessorSymbols()
                    );

                var tree = CSharpSyntaxTree.ParseText(
                    sourceText,
                    parseOptions,
                    sourceCodePath
                    );

                if (_options.EnableDebug)
                {
                    tree = CSharpSyntaxTree.Create(tree.GetRoot() as CSharpSyntaxNode, path: sourceCodePath, encoding: _options.Encoding);
                }

                IEnumerable<MetadataReference> references;
                using (var metadataLoadContext = new MetadataLoadContext(new ScriptAssemblyResolver()))
                {
                    references = Assembly.GetExecutingAssembly()
                        .GetReferencedAssemblies()
                        .Select(name => metadataLoadContext.LoadFromAssemblyName(name).Location)
                        .Select(path => MetadataReference.CreateFromFile(path))
                        .ToArray();
                }

                var compileOptions = new CSharpCompilationOptions(
                    OutputKind.ConsoleApplication,
                    optimizationLevel: _options.GetOptimizationLevel(),
                    sourceReferenceResolver: ScriptSourceResolver.Default
                        .WithBaseDirectory(Path.GetDirectoryName(sourceCodePath))
                        .WithSearchPaths(_options.IncludePaths.Concat(new[] { _options.WorkingDirectory })),
                    metadataReferenceResolver: ScriptReferenceResolver.Default
                        .WithSearchPaths(_options.LibraryPaths.Concat(new[] { _options.WorkingDirectory, RuntimeEnvironment.GetRuntimeDirectory() }))
                    ); ;

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(sourceCodePath),
                    syntaxTrees: new[] { tree },
                    references: references,
                    options: compileOptions
                    );

                Stream peStream = new MemoryStream();
                Stream pdbStream = null;

                EmitOptions emitOptions = null;
                IEnumerable<EmbeddedText> embeddedTexts = null;

                if (_options.EnableDebug)
                {
                    pdbStream = new MemoryStream();
                    emitOptions = new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: Path.ChangeExtension(sourceCodePath, ".pdb")
                        );
                    embeddedTexts = new[] { EmbeddedText.FromSource(sourceCodePath, sourceText) };
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
                        Console.WriteLine(diag.ToString());
                    }
                    return null;
                }

                peStream.Seek(0, SeekOrigin.Begin);
                pdbStream?.Seek(0, SeekOrigin.Begin);

                return new Executer(peStream, pdbStream);
            })
            .ConfigureAwait(false);

        private BuildOptions _options;
    }
}
