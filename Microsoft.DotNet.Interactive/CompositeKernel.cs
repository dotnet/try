// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class CompositeKernel : KernelBase, IEnumerable<IKernel>
    {
        private readonly List<IKernel> _kernels = new List<IKernel>();
        private readonly Argument<string> _kernelNameArgument;

        public CompositeKernel()
        {
            _kernelNameArgument = new Argument<string>("kernelName");

            var chooseKernelCommand = new Command("#kernel")
            {
                _kernelNameArgument
            };

            chooseKernelCommand.Handler =
                CommandHandler.Create<string, KernelPipelineContext>((kernelName, context) =>
                {
                    DefaultKernel = this.Single(k => k.Name == kernelName);
                });

            AddDirective(chooseKernelCommand);
        }

        public IKernel DefaultKernel { get; set; }

        public override string Name => nameof(CompositeKernel);

        public void Add(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            _kernels.Add(kernel);

            _kernelNameArgument.FromAmong(kernel.Name);

            AddDisposable(kernel.KernelEvents.Subscribe(PublishEvent));
        }

        protected override void SetKernel(
            IKernelCommand command,
            KernelPipelineContext context)
        {
            if (context.Kernel == null)
            {
                if (DefaultKernel != null)
                {
                    context.Kernel = DefaultKernel;
                }
                else if (_kernels.Count == 1)
                {
                    context.Kernel = _kernels[0];
                }
            }
        }

        protected internal override async Task HandleAsync(
            IKernelCommand command,
            KernelPipelineContext context)
        {
            var kernel = context.Kernel;

            if (kernel is KernelBase kernelBase)
            {
                await kernelBase.Pipeline.SendAsync(command, context);
                return;
            }

            throw new NoSuitableKernelException();
        }

        public IEnumerator<IKernel> GetEnumerator() => _kernels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}