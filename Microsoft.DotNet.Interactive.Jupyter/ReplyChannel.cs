// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal class ReplyChannel : IReplyChannel
    {
        private readonly IMessageSender _sender;

        public ReplyChannel(IMessageSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }
        public void Reply(JupyterReplyMessageContent messageContent, Message request)
        {
            var reply = Message.CreateReply(messageContent, request);
            _sender.Send(reply);
        }
    }
}