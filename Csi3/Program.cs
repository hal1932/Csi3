using ConsoleAppFramework;
using Csi3.Build;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Csi3
{
    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ReplaceToSimpleConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .RunConsoleAppFrameworkAsync<Program>(args);
        }

        [Command("program")]
        public async Task<int> ExecuteProgramAsync(
            [Option("d", "enable debug")] bool debug = false,
            [Option("e", "source code encoding")] string encoding = "utf-8",
            [Option("i", "source code include paths")] string[] includes = null,
            [Option("l", "library assembly paths")] string[] libraries = null,
            [Option("w", "watch file updates")] bool watch = false
            )
            => await ExecuteImplAsync(SourceCodeKind.Regular, debug, encoding, includes, libraries, watch);


        [Command("script")]
        public async Task<int> ExecuteScriptAsync(
            [Option("d", "enable debug")] bool debug = false,
            [Option("e", "source code encoding")] string encoding = "utf-8",
            [Option("i", "source code include paths")] string[] includes = null,
            [Option("l", "library assembly paths")] string[] libraries = null,
            [Option("w", "watch file updates")] bool watch = false
            )
            => await ExecuteImplAsync(SourceCodeKind.Script, debug, encoding, includes, libraries, watch);

        private async Task<int> ExecuteImplAsync(SourceCodeKind kind, bool debug, string encoding, string[] includes, string[] libraries, bool watch)
        {
            var logger = Context.Logger;

            var argsSplitterIndex = Array.IndexOf(Context.Arguments ?? Array.Empty<string>(), "--");

            var scriptPath = default(string);
            var args = Array.Empty<string>();
            if (argsSplitterIndex < 0)
            {
                scriptPath = Context.Arguments.LastOrDefault();
            }
            else if (argsSplitterIndex > 0)
            {
                scriptPath = Context.Arguments[argsSplitterIndex - 1];
                args = Context.Arguments.Skip(argsSplitterIndex + 1).ToArray();
            }

            if (string.IsNullOrEmpty(scriptPath))
            {
                logger.LogError("no script file is specified.");
                return 1;
            }

            if (!File.Exists(scriptPath))
            {
                logger.LogError($"{scriptPath} is not found");
                return 1;
            }

            var options = new BuildOptions()
            {
                SourceCodeKind = kind,
                EnableDebug = debug,
                Encoding = Encoding.GetEncoding(encoding),
                LoadPaths = includes ?? Array.Empty<string>(),
                ReferencePaths = libraries ?? Array.Empty<string>(),
                WorkingDirectory = Environment.CurrentDirectory,
                SourceFilesMayBeLocked = watch,
            };

            var builder = new Builder(options, logger);

            if (watch)
            {
                using (var watcher = new FileWather())
                {
                    while (!Context.CancellationToken.IsCancellationRequested)
                    {
                        watcher.Stop();
                        watcher.Clear();

                        using (var executer = await builder.BuildAsync(scriptPath))
                        {
                            if (executer != null)
                            {
                                var result = await executer.ExecuteAsync(args);
                                result.WaitForExit();
                            }
                        }

                        watcher.AddFiles(builder.SourceCodePaths);
                        watcher.Start();

                        Console.WriteLine();
                        Console.WriteLine("waiting for source codes edit...");
                        Console.WriteLine();

                        watcher.WaitForChanged(Context.CancellationToken);
                    }
                }
            }
            else
            {
                var executer = await builder.BuildAsync(scriptPath);
                var result = await executer.ExecuteAsync(args);
                result.WaitForExit();
            }

            return 0;
        }
    }
}
