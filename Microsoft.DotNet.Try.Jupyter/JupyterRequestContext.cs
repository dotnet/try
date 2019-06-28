// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class JupyterRequestContext
    {
        public IMessageSender ServerChannel { get; }
        public IMessageSender IoPubChannel { get; }
        public Message Request { get; }

        public T GetRequestContent<T>() where T : JupyterMessageContent
        {
            return Request?.Content as T;
        }

        public IRequestHandlerStatus RequestHandlerStatus { get; }

        public JupyterRequestContext(IMessageSender serverChannel, IMessageSender ioPubChannel, Message request,
            IRequestHandlerStatus requestHandlerStatus)
        {
            ServerChannel = serverChannel ?? throw new ArgumentNullException(nameof(serverChannel));
            IoPubChannel = ioPubChannel ?? throw new ArgumentNullException(nameof(ioPubChannel));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            RequestHandlerStatus = requestHandlerStatus;
        }
    }
}