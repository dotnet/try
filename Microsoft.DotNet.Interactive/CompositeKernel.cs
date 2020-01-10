// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class CompositeKernel : KernelBase, IEnumerable<IKernel>, ICompositeKernel, IExtensibleKernel
    {
        private readonly List<IKernel> _childKernels = new List<IKernel>();

        public CompositeKernel()
        {
            Name = nameof(CompositeKernel);
            RegisterForDisposal(Disposable.Create(() => { KernelHierarchy.DeleteNode(this); }));
            RegisterForDisposal(KernelEvents.Subscribe(async e =>
            {
                if (e is PackageAdded packageAdded)
                {
                    await this.SendAsync(new LoadKernelExtensionsInDirectory(packageAdded.PackageReference.PackageRoot));
                }
            }));
        }

        public string DefaultKernelName { get; set; }

        public void Add(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            _childKernels.Add(kernel);

            KernelHierarchy.AddChildKernel(this, kernel);

            var chooseKernelCommand = new Command($"%%{kernel.Name}");

            chooseKernelCommand.Handler =
                CommandHandler.Create<KernelInvocationContext>(context =>
                {
                    context.HandlingKernel = kernel;
                });

            AddDirective(chooseKernelCommand);

            RegisterForDisposal(kernel.KernelEvents.Subscribe(PublishEvent));

            RegisterForDisposal(kernel);
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

                    default:
                        if (command is SubmitCode submitCode &&
                            ChildKernels.SingleOrDefault(k => k.Name == submitCode.TargetKernelName) is {} targetKernel)
                        {
                            context.HandlingKernel = targetKernel;
                        }
                        else if (DefaultKernelName != null)
                        {
                            context.HandlingKernel = ChildKernels.SingleOrDefault(k => k.Name == DefaultKernelName);
                        }

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
                await kernelBase.RunDeferredCommandsAsync();

                await kernelBase.Pipeline.SendAsync(command, context);

                return;
            }

            throw new NoSuitableKernelException();
        }

        internal override Task HandleInternalAsync(IKernelCommand command, KernelInvocationContext context)
        {
            return HandleAsync(command, context);
        }

        public IReadOnlyCollection<IKernel> ChildKernels => _childKernels;

        public IEnumerator<IKernel> GetEnumerator() => _childKernels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Task LoadExtensionsFromDirectory(DirectoryInfo directory, KernelInvocationContext invocationContext,
            IReadOnlyList<FileInfo> additionalDependencies = null)
        {
           // TODO: add kernel logic
           return Task.CompletedTask;
        }
    }
}