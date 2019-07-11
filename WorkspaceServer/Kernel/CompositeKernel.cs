// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class CompositeKernel : KernelBase, IEnumerable<IKernel>
    {
        private readonly List<IKernel> _kernels = new List<IKernel>();

        public CompositeKernel()
        {
            Pipeline.AddMiddleware(ChooseKernel);
        }

        public void Add(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            _kernels.Add(kernel);

            AddDisposable(kernel.KernelEvents.Subscribe(PublishEvent));
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

        public IEnumerator<IKernel> GetEnumerator() => _kernels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}