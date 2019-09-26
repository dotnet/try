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

        internal JupyterRequestContext(IReplyChannel serverChannel, IPubSubChannel ioPubChannel, JupyterMessage request, string kernelIdentity) : 
            this(new JupyterMessageContentDispatcher(ioPubChannel, serverChannel, kernelIdentity),request,kernelIdentity)
        {
        }

        public JupyterRequestContext(IJupyterMessageContentDispatcher jupyterMessageContentDispatcher, JupyterMessage request, string kernelIdentity)
        {


            JupyterMessageContentDispatcher = jupyterMessageContentDispatcher ?? throw new ArgumentNullException(nameof(jupyterMessageContentDispatcher));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            KernelIdentity = kernelIdentity;
        }

        public IJupyterMessageContentDispatcher JupyterMessageContentDispatcher { get; }

        public JupyterMessage Request { get; }
        public string KernelIdentity { get; }

        public T GetRequestContent<T>() where T : JupyterRequestMessageContent
        {
            return Request?.Content as T;
        }

        public void Complete() => _done.SetResult(Unit.Default);

        public Task Done() => _done.Task;
    }
}