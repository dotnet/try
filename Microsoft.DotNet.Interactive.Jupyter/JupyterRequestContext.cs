// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterRequestContext
    {
        private readonly TaskCompletionSource<Unit> _done = new TaskCompletionSource<Unit>();

        internal JupyterRequestContext(IReplyChannel serverChannel, IPubSubChannel ioPubChannel, Message request, string kernelIdentity) : 
            this(new MessageDispatcher(ioPubChannel, serverChannel, kernelIdentity),request,kernelIdentity)
        {
        }

        public JupyterRequestContext(IMessageDispatcher messageDispatcher, Message request, string kernelIdentity)
        {


            MessageDispatcher = messageDispatcher ?? throw new ArgumentNullException(nameof(messageDispatcher));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            KernelIdentity = kernelIdentity;
        }

        public IMessageDispatcher MessageDispatcher { get; }

        public Message Request { get; }
        public string KernelIdentity { get; }

        public T GetRequestContent<T>() where T : JupyterRequestMessageContent
        {
            return Request?.Content as T;
        }

        public void Complete() => _done.SetResult(Unit.Default);

        public Task Done() => _done.Task;
    }
}