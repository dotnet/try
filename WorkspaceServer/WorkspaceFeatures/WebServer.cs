// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Packaging;
using static Pocket.Logger;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.WorkspaceFeatures
{
    public class WebServer : IRunResultFeature, IDisposable
    {
        private readonly Package package;
        private readonly AsyncLazy<HttpClient> _getHttpClient;
        private readonly AsyncLazy<Uri> _listeningAtUri;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public WebServer(IPackage package)
        {
            this.package = (Package)package ?? throw new ArgumentNullException(nameof(package));

            _listeningAtUri = new AsyncLazy<Uri>(RunKestrel);

            _getHttpClient = new AsyncLazy<HttpClient>(async () =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = await EnsureStarted()
                };

                return httpClient;
            });
        }

        private async Task<Uri> RunKestrel()
        {
            await package.EnsurePublished();

            var operation = Log.OnEnterAndExit();

            var process = CommandLine.StartProcess(
                Dotnet.Path.FullName,
                package.EntryPointAssemblyPath.FullName,
                package.Directory,
                StandardOutput.OnNext,
                StandardError.OnNext,
                ("ASPNETCORE_DETAILEDERRORS", "1"),
                ("ASPNETCORE_URLS", "http://127.0.0.1:0"),
                ("ASPNETCORE_PORT", null));

            _disposables.Add(() =>
            {
                operation.Dispose();
                process.Kill();
            });

            _disposables.Add(StandardOutput.Subscribe(s => operation.Trace(s)));
            _disposables.Add(StandardError.Subscribe(s => operation.Error(s)));

            var kestrelListeningMessagePrefix = "Now listening on:";

            var uriString = await StandardOutput
                                  .Where(line => line.Contains(kestrelListeningMessagePrefix))
                                  .Select(line => line.Replace(kestrelListeningMessagePrefix, ""))
                                  .FirstAsync();

            operation.Trace("Starting Kestrel at {uri}.", uriString);

            return new Uri(uriString);
        }

        public StandardOutput StandardOutput { get; } = new StandardOutput();

        public StandardError StandardError { get; } = new StandardError();

        public string Name => nameof(WebServer);

        public Task<Uri> EnsureStarted() => _listeningAtUri.ValueAsync();

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var httpClient = await _getHttpClient.ValueAsync();

            var response = await httpClient.SendAsync(request);

            return response;
        }

        public void Dispose() => _disposables.Dispose();

        public void Apply(FeatureContainer runResult)
        {
        }
    }
}
