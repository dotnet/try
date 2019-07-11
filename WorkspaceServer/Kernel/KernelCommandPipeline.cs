// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class KernelCommandPipeline
    {
        private readonly KernelBase _kernel;

        private readonly List<KernelCommandPipelineMiddleware> _invocations = new List<KernelCommandPipelineMiddleware>();
        private KernelCommandPipelineMiddleware _invocationChain;

        public KernelCommandPipeline(KernelBase kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        private void EnsureMiddlewarePipelineIsInitialized()
        {
            if (_invocationChain == null)
            {
                _invocationChain = BuildInvocationChain();
            }
        }

        public async Task InvokeAsync(
            IKernelCommand command,
            KernelCommandContext context)
        {
            EnsureMiddlewarePipelineIsInitialized();

            await _invocationChain(command, context, (_, __) => Task.CompletedTask);
        }

        private KernelCommandPipelineMiddleware BuildInvocationChain()
        {
            var invocations = new List<KernelCommandPipelineMiddleware>(_invocations);

            invocations.Add(async (command, context, _) =>
            {
                await _kernel.HandleAsync(command, context);
            });

            return invocations.Aggregate(
                (function, continuation) =>
                    (cmd1, ctx1, next) =>
                        function(cmd1, ctx1, (cmd2, ctx2) =>
                                     continuation(cmd2, ctx2, next)));
        }

        public void AddMiddleware(KernelCommandPipelineMiddleware middleware)
        {
            _invocations.Add(middleware);
            _invocationChain = null;
        }
    }
}