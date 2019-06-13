// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.CommandLine;
using Pocket;
using Xunit.Abstractions;

namespace MLS.Agent.Tests
{
    public abstract class ApiViaHttpTestsBase : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected ApiViaHttpTestsBase(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(VirtualClock.Start());
        }

        public void Dispose() => _disposables.Dispose();

        protected static Task<HttpResponseMessage> CallCompletion(
            string requestBody,
            int? timeoutMs = null) =>
            Call("/workspace/completion",
                 requestBody,
                 timeoutMs);

        protected static Task<HttpResponseMessage> CallRun(
            string requestBody,
            int? timeoutMs = null,
            StartupOptions options = null) =>
            Call("/workspace/run",
                 requestBody,
                 timeoutMs,
                 options);

        protected static Task<HttpResponseMessage> CallCompile(
            string requestBody,
            int? timeoutMs = null,
            StartupOptions options = null) =>
            Call("/workspace/compile",
                 requestBody,
                 timeoutMs,
                 options);

        protected static Task<HttpResponseMessage> CallSignatureHelp(
            string requestBody,
            int? timeoutMs = null) =>
            Call("/workspace/signaturehelp",
                 requestBody,
                 timeoutMs);

        private static async Task<HttpResponseMessage> Call(
            string relativeUrl,
            string requestBody,
            int? timeoutMs = null,
            StartupOptions options = null)
        {
            HttpResponseMessage response;
            using (var agent = new AgentService(options ?? StartupOptionsFactory.CreateFromCommandLine("hosted")))
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    relativeUrl)
                {
                    Content = new StringContent(
                        requestBody,
                        Encoding.UTF8,
                        "application/json")
                };

                if (timeoutMs != null)
                {
                    request.Headers.Add("Timeout", timeoutMs.Value.ToString("F0"));
                }

                response = await agent.SendAsync(request);
            }

            return response;
        }
    }
}
