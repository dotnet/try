// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class JupyterRequestContext
    {
        public IMessageBuilder Builder { get; }
        public IMessageSender ServerChannel { get; }
        public IMessageSender IoPubChannel { get; }
        public Message Request { get; }
        public IRequestHandlerStatus RequestHandlerStatus { get; }

        public JupyterRequestContext(IMessageBuilder messageBuilder, IMessageSender serverChannel, IMessageSender ioPubChannel, Message request,
            IRequestHandlerStatus requestHandlerStatus)
        {
            Builder = messageBuilder ?? throw new ArgumentNullException(nameof(messageBuilder));
            ServerChannel = serverChannel ?? throw new ArgumentNullException(nameof(serverChannel));
            IoPubChannel = ioPubChannel ?? throw new ArgumentNullException(nameof(ioPubChannel));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            RequestHandlerStatus = requestHandlerStatus;
        }
    }
}