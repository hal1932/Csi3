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

        public async Task<Executer> BuildAsync(string scriptPath)
            => await Task.Factory.StartNew(() =>
            {
                scriptPath = Path.GetFullPath(scriptPath);

                var sourceCodes = new Dictionary<string, string>();
                var referencePaths = new HashSet<string>();
                {
                    var paths = new Stack<string>();
                    paths.Push(scriptPath);

                    var sourceCodeRoot = Path.GetDirectoryName(scriptPath);

                    while (paths.Any())
                    {
                        var path = paths.Pop();

                        string filePath = null;

                        var filePathCandidate = Path.Combine(sourceCodeRoot, path);
                        if (File.Exists(filePathCandidate))
                        {
                            filePath = filePathCandidate;
                        }
                        else
                        {

                            foreach (var includePath in _options.LoadPaths)
                            {
                                filePathCandidate = Path.Combine(includePath, path);
                                if (File.Exists(filePathCandidate))
                                {
                                    filePath = filePathCandidate;
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(filePath))
                        {
                            Console.Error.WriteLine($"ファイルが見つかりません: {path}");
                            return null;
                        }

                        if (sourceCodes.ContainsKey(filePath))
                        {
                            continue;
                        }

                        var pp = new Preprocessor(filePath, _options.Encoding);
                        pp.Preprocess();
                        foreach (var loadedPath in pp.Loads)
                        {
                            paths.Push(loadedPath);
                        }

                        foreach (var referencePath in pp.References)
                        {
                            referencePaths.Add(referencePath);
                        }

                        sourceCodes[filePath] = pp.SourceCode.ToString();
                    }
                }

                var parseOptions = new CSharpParseOptions(
                    kind: _options.GetKind(),
                    preprocessorSymbols: _options.GetPreprocessorSymbols()
                    );

                var syntaxTrees = new List<SyntaxTree>();
                foreach (var item in sourceCodes)
                {
                    var filePath = item.Key;
                    var sourceCode = item.Value;

                    var tree = CSharpSyntaxTree.ParseText(
                        sourceCode,
                        parseOptions,
                        filePath
                        );

                    if (_options.EnableDebug)
                    {
                        tree = CSharpSyntaxTree.Create(tree.GetRoot() as CSharpSyntaxNode, path: filePath, encoding: _options.Encoding);
                    }

                    syntaxTrees.Add(tree);
                }

                IEnumerable<MetadataReference> references;
                using (var metadataLoadContext = new MetadataLoadContext(new ScriptAssemblyResolver(searchPaths: _options.ReferencePaths)))
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
                        .WithBaseDirectory(Path.GetDirectoryName(scriptPath))
                        .WithSearchPaths(_options.LoadPaths.Concat(new[] { _options.WorkingDirectory })),
                    metadataReferenceResolver: ScriptReferenceResolver.Default
                        .WithSearchPaths(_options.ReferencePaths.Concat(new[] { _options.WorkingDirectory, RuntimeEnvironment.GetRuntimeDirectory() }))
                    ); ;

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(scriptPath),
                    syntaxTrees: syntaxTrees,
                    references: references,
                    options: compileOptions
                    );

                Stream peStream = new MemoryStream();
                Stream pdbStream = null;

                EmitOptions emitOptions = null;
                List<EmbeddedText> embeddedTexts = null;

                if (_options.EnableDebug)
                {
                    pdbStream = new MemoryStream();
                    emitOptions = new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: Path.ChangeExtension(scriptPath, ".pdb")
                        );

                    embeddedTexts = new List<EmbeddedText>();
                    foreach (var item in sourceCodes)
                    {
                        var filePath = item.Key;
                        var sourceCode = item.Value;

                        var sourceCodeBuffer = _options.Encoding.GetBytes(sourceCode);
                        var sourceText = SourceText.From(sourceCodeBuffer, sourceCodeBuffer.Length, encoding: _options.Encoding, canBeEmbedded: true);

                        embeddedTexts.Add(EmbeddedText.FromSource(filePath, sourceText));
                    }
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
