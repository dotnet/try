// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class EventTracker<T>  : IDisposable
        where T: class ,IKernelEvent
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ConcurrentDictionary<IKernelCommand, List<T>> _mailBoxes = new ConcurrentDictionary<IKernelCommand, List<T>>();
        private readonly Func<List<T>, Task> _onCommandHandled;
        private readonly Func<List<T>, Task> _onCommandFailed;

        public EventTracker(IObservable<IKernelEvent> eventStream, Func<List<T>, Task> onCommandHandled,
            Func<List<T>, Task> onCommandFailed = null)
        {
            if (eventStream == null)
            {
                throw new ArgumentNullException(nameof(eventStream));
            }

            _onCommandHandled = onCommandHandled ?? throw new ArgumentNullException(nameof(onCommandHandled));
            _onCommandFailed = onCommandFailed;

            _disposables.Add(eventStream.Where(e => e.Command != null).Subscribe(async e =>
            {
                await TrackEvent(e);
            }));
          
        }


        private async Task TrackEvent(IKernelEvent @event)
        {
            switch (@event)
            {
                case CommandFailed cf:
                {
                    var events = GetQueue(cf.Command);
                    if (_onCommandFailed != null)
                    {
                        await _onCommandFailed(events);
                    }
                }
                    break;
                case CommandHandled ch:
                {
                    var events = GetQueue(ch.Command);
                    await _onCommandHandled(events);
                }
                    break;
                case T trackedEvent:
                    _mailBoxes.AddOrUpdate(trackedEvent.Command,
                            _ =>
                            {
                                var queue = new List<T> {trackedEvent};
                                return queue;
                            },
                            (_, queue) =>
                            {
                                queue.Add(trackedEvent);
                                return queue;
                            });
                    
                    break;
            }
        }

        private List<T> GetQueue(IKernelCommand kernelCommand)
        {
            return _mailBoxes.TryRemove(kernelCommand, out var queue) ? queue : null;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
    public class CompositeKernel : KernelBase, IEnumerable<IKernel>, ICompositeKernel, IExtensibleKernel
    {
        private readonly List<IKernel> _childKernels = new List<IKernel>();
        private readonly CompositeKernelExtensionLoader _extensionLoader;

        public CompositeKernel()
        {
            Name = nameof(CompositeKernel);
            _extensionLoader = new CompositeKernelExtensionLoader();
            InterceptPackageAddedEvent();
        }

        private void InterceptPackageAddedEvent()
        {
            var tracker = new EventTracker<PackageAdded>(KernelEvents,
                async events =>
                {
                    foreach (var packageAdded in events)
                    {
                        var loadExtensionsInDirectory =
                            new LoadExtensionsInDirectory(packageAdded.PackageReference.PackageRoot, Name);
                        await this.SendAsync(loadExtensionsInDirectory);
                    }
                });

            RegisterForDisposal(tracker);
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

            var chooseKernelCommand = new Command($"%%{kernel.Name}")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    context => { context.HandlingKernel = kernel; })
            };

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
            await _extensionLoader.LoadFromDirectoryAsync(
                directory,
                this,
                context);
        }
    }
}