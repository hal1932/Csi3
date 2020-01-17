using ConsoleAppFramework;
using Csi3.Build;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Csi3
{
    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
            Console.WriteLine("===");
            Console.ReadKey();
        }

        public async Task<int> ExecuteAsync()
        {
            var scriptPath = Context.Arguments.FirstOrDefault();
            if (scriptPath == default)
            {
                WriteError("no script file is specified.");
                return 1;
            }

            if (!File.Exists(scriptPath))
            {
                WriteError($"{scriptPath} is not found");
                return 1;
            }

            var options = new BuildOptions()
            {
                EnableDebug = true,
            };

            var builder = new Builder(options);

            Console.WriteLine("build");
            var executer = await builder.BuildAsync(scriptPath);
            Console.WriteLine("exec");
            var task = executer.ExecuteAsync("a", "b");
            Console.WriteLine("wait exec");
            task.Wait();
            Console.WriteLine("wait exit");
            task.Result.WaitForExit();
            Console.WriteLine("done");

            return 0;
        }

        private void WriteError(string message)
            => Console.Error.WriteLine($"[ERROR] {message}");
    }
}
