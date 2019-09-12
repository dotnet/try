// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MLS.Agent;
using MLS.Agent.Tools;
using WorkspaceServer;

namespace NotIntegration.Tests
{
    public class DotnetTryFixture : IDisposable
    {
        private readonly DisposableDirectory _disposableDirectory;
        private Process _process;
        private HttpClient _client;
        private readonly AsyncLazy<bool> _lazyReady;

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
            var dotnet = new Dotnet();
            var installResult = await dotnet.ToolInstall("dotnet-try", _disposableDirectory.Directory, version: "1.0.44142.42", addSource: GetPackageSource());
            installResult.ThrowOnFailure();

            await Start();
            return true;
        }

        private async Task Start()
        {
            var tcs = new TaskCompletionSource<bool>();
            var dotnetTry = _disposableDirectory.Directory.GetFiles("dotnet-try*").First().FullName;
            _process = CommandLine.StartProcess(dotnetTry, "--port 7891 hosted", _disposableDirectory.Directory,
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
            _client.BaseAddress = new Uri("http://localhost:7891");
            await tcs.Task;
        }

        private PackageSource GetPackageSource([CallerFilePath] string callerFile = "")
        {
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
