// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class CompositeKernel : KernelBase
    {
        private readonly IReadOnlyList<IKernel> _kernels;
        

        public CompositeKernel(IReadOnlyList<IKernel> kernels)
        {
            _kernels = kernels ?? throw new ArgumentNullException(nameof(kernels));
            AddDisposable( kernels.Select(k => k.KernelEvents).Merge().Subscribe(PublishEvent));
        }

        protected internal override async Task HandleAsync(KernelCommandContext context)
        {
            foreach (var kernel in _kernels.OfType<KernelBase>())
            {
                await kernel.Pipeline.InvokeAsync(context);
                if (context.Result != null)
                {
                    return;
                }
            }
        }
    }
}