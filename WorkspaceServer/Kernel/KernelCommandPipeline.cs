// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class KernelCommandPipeline {
        private readonly KernelBase _kernel;

        private readonly List<KernelCommandPipelineMiddleware> _invocations = new List<KernelCommandPipelineMiddleware>();

        public KernelCommandPipeline(KernelBase kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public async Task InvokeAsync(KernelCommandContext context)
        {
            var invocationChain = BuildInvocationChain();

            await invocationChain(context, invocationContext => Task.CompletedTask);
        }

        private KernelCommandPipelineMiddleware BuildInvocationChain()
        {
            var invocations = new List<KernelCommandPipelineMiddleware>(_invocations);

            invocations.Add(async (invocationContext, _) =>
            {
                await _kernel.HandleAsync(invocationContext);
            });

            return invocations.Aggregate(
                (function, continuation) =>
                    (ctx, next) =>
                        function(ctx, c => continuation(c, next)));
        }

        public void AddMiddleware(KernelCommandPipelineMiddleware middleware)
        {
            _invocations.Add(middleware);
        }
    }
}