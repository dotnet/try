// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Envelope = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterRequestContext
    {
        private readonly TaskCompletionSource<Unit> _done = new TaskCompletionSource<Unit>();

        internal JupyterRequestContext(ReplyChannel serverChannel, PubSubChannel ioPubChannel, Envelope
 request, string kernelIdentity) : 
            this(new JupyterMessageSender(ioPubChannel, serverChannel, kernelIdentity, request),request,kernelIdentity)
        {
        }

        public JupyterRequestContext(IJupyterMessageSender jupyterMessageSender, Envelope
 request, string kernelIdentity)
        {
            JupyterMessageSender = jupyterMessageSender ?? throw new ArgumentNullException(nameof(jupyterMessageSender));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            KernelIdentity = kernelIdentity;
        }

        public IJupyterMessageSender JupyterMessageSender { get; }

        public Envelope
 Request { get; }
        public string KernelIdentity { get; }

        public T GetRequestContent<T>() where T : RequestMessage
        {
            return Request?.Content as T;
        }

        public void Complete() => _done.SetResult(Unit.Default);

        public Task Done() => _done.Task;
    }
}