// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ReplyChannel : IReplyChannel
    {
        private readonly IMessageSender _sender;

        public ReplyChannel(IMessageSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }
        public bool Send(JupyterReplyContent content, Message request)
        {
            var reply = Message.CreateReply(content, request);
            return _sender.Send(reply);
        }
    }

    public class PubSubChannel : IPubSubChannel
    {
        private readonly IMessageSender _sender;

        public PubSubChannel(IMessageSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }
        public bool Send(JupyterPubSubContent content, Message request, string ident = null)
        {
            var reply = Message.CreatePubSub(content, request, ident);
            return _sender.Send(reply);
        }
    }
}