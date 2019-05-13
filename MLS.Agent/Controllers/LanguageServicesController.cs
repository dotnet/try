// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Middleware;
using Pocket;
using WorkspaceServer;
using WorkspaceServer.Servers.Roslyn;
using static Pocket.Logger<MLS.Agent.Controllers.LanguageServicesController>;

namespace MLS.Agent.Controllers
{
    public class LanguageServicesController : Controller
    {
        private const string CompletionRoute = "/workspace/completion";
        public static RequestDescriptor CompletionApi => new RequestDescriptor(
            CompletionRoute,
            timeoutMs: 60000,
            properties: new[]
            {
                new RequestDescriptorProperty("completionProvider"),
            });

        private const string DiagnosticsRoute = "/workspace/diagnostics";
        public static RequestDescriptor DiagnosticsApi => new RequestDescriptor(DiagnosticsRoute, timeoutMs: 60000);

        private const string SignatureHelpRoute = "/workspace/signatureHelp";
        public static RequestDescriptor SignatureHelpApi => new RequestDescriptor(SignatureHelpRoute, timeoutMs: 60000);

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly RoslynWorkspaceServer _workspaceServer;

        public LanguageServicesController(RoslynWorkspaceServer workspaceServer)
        {
            _workspaceServer = workspaceServer ?? throw new ArgumentNullException(nameof(workspaceServer));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposables.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpPost]
        [Route(CompletionRoute)]
        [DebugEnableFilter]
        public async Task<IActionResult> Completion(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "15000")
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Info("Processing workspaceType {workspaceType}", request.Workspace.WorkspaceType);
                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);
                var server = GetServerForWorkspace(request.Workspace);
                var result = await server.GetCompletionList(request, budget);
                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }
    
        [HttpPost]
        [Route("/workspace/signaturehelp")]
        public async Task<IActionResult> SignatureHelp(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "15000")
        {
            if (Debugger.IsAttached && !(Clock.Current is VirtualClock))
            {
                _disposables.Add(VirtualClock.Start());
            }

            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Info("Processing workspaceType {workspaceType}", request.Workspace.WorkspaceType);
                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);
                var server = GetServerForWorkspace(request.Workspace);
                var result = await server.GetSignatureHelp(request, budget);
                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }

        [HttpPost]
        [Route(DiagnosticsRoute)]
        public async Task<IActionResult> Diagnostics(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "15000")
        {
            if (Debugger.IsAttached && !(Clock.Current is VirtualClock))
            {
                _disposables.Add(VirtualClock.Start());
            }

            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Info("Processing workspaceType {workspaceType}", request.Workspace.WorkspaceType);
                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);
                var server = GetServerForWorkspace(request.Workspace);
                var result = await server.GetDiagnostics(request, budget);
                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }

        private ILanguageService GetServerForWorkspace(Workspace workspace)
        {
            return _workspaceServer;
        }
    }
}
