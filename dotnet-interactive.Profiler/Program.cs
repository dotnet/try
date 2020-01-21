using System;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;

namespace dotnet_interactive.Profiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MemoryProfiler.CollectAllocations(true);
            MemoryProfiler.ForceGc();

            const int iterationCount = 20;

            for (int i = 0; i < iterationCount; i++)
            {
                MemoryProfiler.GetSnapshot($"Before Kernel creation at Iteration {i}");
                MemoryProfiler.ForceGc();
                var kernel = CreateKernel();
                MemoryProfiler.ForceGc();
                MemoryProfiler.GetSnapshot($"Before Iteration {i}");
                
                var submitCode = new SubmitCode(@"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");"
                    , targetKernelName: "csharp");

                await kernel.SendAsync(submitCode);
                MemoryProfiler.GetSnapshot($"After Iteration {i}");
                kernel.Dispose();
                kernel = null;
                MemoryProfiler.ForceGc();
            }
        }

        private static IKernel CreateKernel()
        {
            var kernel = new CompositeKernel
                {
                    new CSharpKernel()
                        .UseDefaultFormatting()
                        .UseNugetDirective()
                        .UseKernelHelpers()
                        .UseWho()
                        .UseXplot()
                        .UseMathAndLaTeX(),
                    new FSharpKernel()
                        .UseDefaultFormatting()
                        .UseKernelHelpers()
                        .UseWho()
                        .UseDefaultNamespaces()
                        .UseXplot()
                        .UseMathAndLaTeX()
                }
                .UseDefaultMagicCommands();

            kernel.DefaultKernelName = "csharp";
            kernel.Name = ".NET";
            return kernel;
        }
    }
}
