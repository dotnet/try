// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Pocket;
using Recipes;
using WorkspaceServer.Features;
using WorkspaceServer.Tests.Packaging;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests
{
    public class WebServerTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public WebServerTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose() => _disposables.Dispose();

        [Fact]
        public async Task Multiple_WebServer_instances_can_be_run_concurrently_in_the_same_folder()
        {
            var workspace = await PackageUtilities.Copy(await Default.WebApiWorkspace());
            using (var webServer1 = new WebServer(workspace))
            using (var webServer2 = new WebServer(workspace))
            {
                var response1 = await webServer1.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/custom/values"));
                var response2 = await webServer2.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/custom/values"));

                response1.EnsureSuccess();
                response2.EnsureSuccess();
            }
        }

        [Fact]
        public async Task EnsureStarted_returns_the_web_server_base_uri()
        {
            var workspace = await PackageUtilities.Copy(await Default.WebApiWorkspace());

            using (var webServer = new WebServer(workspace))
            {
                var uri = await webServer.EnsureStarted();

                uri.Should().NotBeNull();

                uri.ToString().Should().Match("http://127.0.0.1:*");
            }
        }

        [Fact]
        public async Task WebServer_lifecycle_events_can_be_viewed_via_StandardOutput()
        {
            var workspace = await PackageUtilities.Copy(await Default.WebApiWorkspace());
            var log = new StringBuilder();

            using (var webServer = new WebServer(workspace))
            using (webServer.StandardOutput.Subscribe(s => log.Append(s)))
            {
                await webServer.EnsureStarted(); 
                await Task.Delay(100);
            }

            log.ToString().Should().Match(
                "*Now listening on: http://127.0.0.1:*");
            log.ToString().Should().Match(
                "*Hosting environment: Production*");
        }
    }
}
