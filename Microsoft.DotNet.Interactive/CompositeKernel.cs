// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class CompositeKernel : KernelBase, IEnumerable<IKernel>, ICompositeKernel
    {
        private readonly List<IKernel> _childKernels = new List<IKernel>();

        public CompositeKernel()
        {
            Name = nameof(CompositeKernel);
        }

        public void Add(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            _childKernels.Add(kernel);

            var chooseKernelCommand = new Command($"%%{kernel.Name}");

            chooseKernelCommand.Handler =
                CommandHandler.Create<KernelInvocationContext>(context =>
                {
                    context.HandlingKernel = kernel;
                });

            AddDirective(chooseKernelCommand);

            AddDisposable(kernel.KernelEvents.Subscribe(PublishEvent));
        }

        protected override void SetHandlingKernel(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            if (context.HandlingKernel == null)
            {
                switch (_childKernels.Count)
                {
                    case 0:
                        context.HandlingKernel = this;
                        break;
                    case 1:
                        context.HandlingKernel = _childKernels[0];
                        break;
                }
            }
        }

        protected internal override async Task HandleAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            var kernel = context.HandlingKernel;

            if (kernel is KernelBase kernelBase)
            {
                await kernelBase.Pipeline.SendAsync(command, context);
                return;
            }

            throw new NoSuitableKernelException();
        }

        public IReadOnlyCollection<IKernel> ChildKernels => _childKernels;

        public IEnumerator<IKernel> GetEnumerator() => _childKernels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}