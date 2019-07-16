using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MLS.Agent;
using MLS.Agent.Tools;
using WorkspaceServer;

namespace NotIntegrationTests
{
    public class DotnetTryFixture : IDisposable
    {
        private DisposableDirectory _disposableDirectory;
        private Process _process;
        private HttpClient _client;
        private AsyncLazy<bool> _lazyReady;

        public DotnetTryFixture()
        {
            _disposableDirectory = DisposableDirectory.Create();
            _lazyReady = new AsyncLazy<bool>(ReadyAsync);
        }

        public void Dispose()
        {
            _process?.Kill();
            _disposableDirectory.Dispose();
        }

        private async Task<bool> ReadyAsync()
        {
            var disposable = DisposableDirectory.Create();
            var dotnet = new Dotnet();
            var installResult = await dotnet.ToolInstall("dotnet try", disposable.Directory, addSource: GetPackageSource());
            Console.WriteLine(string.Join("\n", installResult.Output));

            await Start();
            return true;
        }

        private async Task Start()
        {
            var tcs = new TaskCompletionSource<bool>();
            var dotnet = new Dotnet(_disposableDirectory.Directory);
            _process = dotnet.StartProcess("try --port 7891", 
                output =>
                {
                    if (output.Contains("Now listening on"))
                    {
                        tcs.SetResult(true);
                    }
                },
                error =>
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        tcs.TrySetException(new Exception(error));
                        Console.Write(error);
                    }
                });

            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://localhost:7891");
            await tcs.Task;
        }

        private PackageSource GetPackageSource([CallerFilePath] string callerFile = "")
        {
            var env = Environment.GetEnvironmentVariables();
            foreach (var key in env.Keys)
            {
                Console.WriteLine($"{key}:{env[key]}");
            }
            var dotnetTryPackageSource = Environment.GetEnvironmentVariable("PACKAGESOURCE");

            var directory = !string.IsNullOrWhiteSpace(dotnetTryPackageSource)
                ? new DirectoryInfo(dotnetTryPackageSource)
                : new DirectoryInfo(Path.Combine(Path.GetDirectoryName(callerFile), "../artifacts/packages/Debug/Shipping"));

            if (!directory.Exists)
            {
                throw new Exception($"Expected packages directory {directory.FullName} to exist but it does not");
            }

            return new PackageSource(directory.FullName);
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            await _lazyReady.ValueAsync();
            return await _client.GetAsync(requestUri);
        }
    }
}
