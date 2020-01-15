// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
            Pipeline.AddMiddleware(async (command, context, next) =>
            {
                if (command is AddPackage _)
                {
                    var packageAddedEvents = new List<PackageAdded>();
                    using var _ = context.KernelEvents.OfType<PackageAdded>().Subscribe(packageAddedEvents.Add);

                    await next(command, context);

                    foreach (var packageRoot in packageAddedEvents.Select(p => p.PackageReference.PackageRoot)
                        .Distinct())
                    {
                        var loadExtensionsInDirectory = new LoadExtensionsInDirectory(packageRoot, Name);
                        await this.SendAsync(loadExtensionsInDirectory);
                    }
                }
                else
                {
                    await next(command, context);
                }
            });
        }

        public string DefaultKernelName { get; set; }

        public void Add(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (ChildKernels.Any(k => k.Name == kernel.Name))
            {
                throw new ArgumentException($"Kernel \"{kernel.Name}\" already registered", nameof(kernel));
            }

            _childKernels.Add(kernel);

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
            var targetKernelName = (command as KernelCommandBase)?.TargetKernelName
                                   ?? DefaultKernelName;
            if (context.HandlingKernel == null || context.HandlingKernel.Name != targetKernelName)
            {
                if (targetKernelName != null)
                {
                    context.HandlingKernel = targetKernelName == Name
                        ? this
                        : ChildKernels.FirstOrDefault(k => k.Name == targetKernelName)
                          ?? throw new NoSuitableKernelException();
                }
                else
                {
                    context.HandlingKernel = _childKernels.Count switch
                    {
                        0 => this,
                        1 => _childKernels[0],
                        _ => context.HandlingKernel
                    };
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

                if (kernelBase != this)
                {
                    await kernelBase.Pipeline.SendAsync(command, context);
                }
                else
                {
                    await command.InvokeAsync(context);
                }

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

        public async Task LoadExtensionsFromDirectory(
            DirectoryInfo directory,
            KernelInvocationContext context)
        {
            var extensionsDirectory =
                new DirectoryInfo(
                    Path.Combine(
                        directory.FullName,
                        "interactive-extensions",
                        "dotnet",
                        "composite"));

            if (extensionsDirectory.Exists)
            {
                await new KernelExtensionLoader().LoadFromAssembliesInDirectory(
                    extensionsDirectory,
                    this,
                    context);
            }
        }
    }
}