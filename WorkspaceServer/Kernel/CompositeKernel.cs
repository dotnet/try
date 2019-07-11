// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class CompositeKernel : KernelBase
    {
        private readonly IReadOnlyList<IKernel> _kernels;

        public CompositeKernel(IReadOnlyList<IKernel> kernels)
        {
            _kernels = kernels ?? throw new ArgumentNullException(nameof(kernels));

            Pipeline.AddMiddleware(ChooseKernel);

            AddDisposable(
                kernels.Select(k => k.KernelEvents)
                       .Merge()
                       .Subscribe(PublishEvent));
        }

        private Task ChooseKernel(
            IKernelCommand command, 
            KernelPipelineContext context, 
            KernelPipelineContinuation next)
        {
            if (context.Kernel == null)
            {
                if (_kernels.Count == 1)
                {
                    context.Kernel = _kernels[0];
                }
            }

            return next(command, context);
        }

        protected internal override async Task HandleAsync(
            IKernelCommand command,
            KernelPipelineContext context)
        {
            var kernel = context.Kernel;

            if (kernel is KernelBase kernelBase)
            {
                await kernelBase.SendOnContextAsync(command, context);
                return;
            }

            throw new NoSuitableKernelException();
        }
    }
}