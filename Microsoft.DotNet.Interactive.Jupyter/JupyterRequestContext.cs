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

        public JupyterRequestContext(IMessageSender serverChannel, IMessageSender ioPubChannel, Message request, string kernelIdent)
        {
            ServerChannel = serverChannel ?? throw new ArgumentNullException(nameof(serverChannel));
            IoPubChannel = ioPubChannel ?? throw new ArgumentNullException(nameof(ioPubChannel));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            KernelIdent = kernelIdent;
        }

        public IMessageSender ServerChannel { get; }

        public IMessageSender IoPubChannel { get; }

        public Message Request { get; }
        public string KernelIdent { get; }

        public T GetRequestContent<T>() where T : JupyterMessageContent
        {
            return Request?.Content as T;
        }

        public void Complete() => _done.SetResult(Unit.Default);

        public Task Done() => _done.Task;
    }
}