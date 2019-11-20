// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Middleware;
using Pocket;
using WorkspaceServer.Models.Execution;
using WorkspaceServer.Servers.Scripting;
using static Pocket.Logger<MLS.Agent.Controllers.RunController>;
using MLS.Agent.CommandLine;
using WorkspaceServer.Servers;
using WorkspaceServer.WorkspaceFeatures;

namespace MLS.Agent.Controllers
{
    public class RunController : Controller
    {
        private const string RunRoute = "/workspace/run";
        public static RequestDescriptor RunApi => new RequestDescriptor(RunRoute, timeoutMs:600000);

        private readonly StartupOptions _options;
        private readonly IWorkspaceServer _workspaceServer;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public RunController(
            StartupOptions options,
            IWorkspaceServer workspaceServer)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _workspaceServer = workspaceServer;
        }

        [HttpPost]
        [Route(RunRoute)]
        [DebugEnableFilter]
        public async Task<IActionResult> Run(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "45000")
        {
            if (_options.IsLanguageService)
            {
                return NotFound();
            }

            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                var workspaceType = request.Workspace.WorkspaceType;

                operation.Info("Processing workspaceType {workspaceType}", workspaceType);

                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                RunResult result;
                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);

                var budget = new TimeBudget(runTimeout);

                if (string.Equals(workspaceType, "script", StringComparison.OrdinalIgnoreCase))
                {
                    var server = new ScriptingWorkspaceServer();

                    result = await server.Run(
                                 request,
                                 budget);
                }
                else
                {
                    using (result = await _workspaceServer.Run(request, budget))
                    {
                        _disposables.Add(result);

                        if (result.Succeeded &&
                            request.HttpRequest != null)
                        {
                            var webServer = result.GetFeature<WebServer>();

                            if (webServer != null)
                            {
                                var response = await webServer.SendAsync(
                                                                  request.HttpRequest.ToHttpRequestMessage())
                                                              .CancelIfExceeds(budget);

                                result = new RunResult(
                                    true,
                                    await response.ToDisplayString());
                            }
                        }
                    }
                }

                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }
    }
}
