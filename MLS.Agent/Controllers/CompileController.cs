// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Middleware;
using Pocket;
using WorkspaceServer.Servers.Roslyn;
using static Pocket.Logger<MLS.Agent.Controllers.CompileController>;

namespace MLS.Agent.Controllers
{
    public class CompileController : Controller
    {
        private const string CompileRoute = "/workspace/compile";
        public static RequestDescriptor CompileApi => new RequestDescriptor(CompileRoute, timeoutMs: 600000);


        private readonly RoslynWorkspaceServer _workspaceServer;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public CompileController(
            RoslynWorkspaceServer workspaceServer)
        {
            _workspaceServer = workspaceServer;
        }

        [HttpPost]
        [Route(CompileRoute)]
        [DebugEnableFilter]
        public async Task<IActionResult> Compile(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "45000")
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                var workspaceType = request.Workspace.WorkspaceType;

                operation.Info("Compiling workspaceType {workspaceType}", workspaceType);

                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                if (string.Equals(workspaceType, "script", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);

                var result = await _workspaceServer.Compile(request, budget);
                budget.RecordEntry();
                operation.Succeed();
                return Ok(result);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposables.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
