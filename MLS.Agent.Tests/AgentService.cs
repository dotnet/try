// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;
using Pocket;
using Recipes;
using WorkspaceServer;

namespace MLS.Agent.Tests
{
    public class AgentService : IDisposable
    {
        private readonly IDirectoryAccessor _directoryAccessor;
        private readonly StartupOptions _options;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private readonly HttpClient _client;

        public AgentService(StartupOptions options = null, IDirectoryAccessor directoryAccessor = null)
        {
            _directoryAccessor = directoryAccessor;
            _options = options ?? new StartupOptions(
                           production: false,
                           languageService: false);

            var testServer = CreateTestServer();

            _client = testServer.CreateClient();

            _disposables.Add(testServer);
            _disposables.Add(_client);
        }

        public FakeBrowserLauncher BrowserLauncher { get; private set; }

        public void Dispose() => _disposables.Dispose();

        private TestServer CreateTestServer()
        {
            //We need to set the feature collection to be not null,
            //so that the ServerFeatures(like the urls to use) can be configured and used
            var featureCollection = new FeatureCollection();
            featureCollection.Set<IServerAddressesFeature>(new ServerAddressesFeature());
            return new TestServer(CreateWebHostBuilder(), featureCollection);
        }

        private IWebHostBuilder CreateWebHostBuilder()
        {
            var builder = new WebHostBuilder()
                          .ConfigureServices(c =>
                          {
                              if (_directoryAccessor != null)
                              {
                                  c.AddSingleton(_directoryAccessor);
                              }
                              c.AddSingleton(_options);
                              c.AddSingleton<IBrowserLauncher>(sp =>
                              {
                                  BrowserLauncher = new FakeBrowserLauncher();
                                  return BrowserLauncher;
                              });
                          })
                          .UseTestEnvironment()
                          .UseStartup<Startup>()
                          .ConfigureUrl(_options.Mode, _options.Port);

            return builder;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
            _client.SendAsync(request);
    }
}