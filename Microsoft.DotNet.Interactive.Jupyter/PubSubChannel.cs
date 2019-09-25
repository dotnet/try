// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class PubSubChannel : IPubSubChannel
    {
        private readonly IMessageSender _sender;

        public PubSubChannel(IMessageSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }
        public bool Publish(JupyterPubSubMessageContent messageContent, Message request, string ident = null)
        {
            var reply = Message.CreatePubSub(messageContent, request, ident);
            return _sender.Send(reply);
        }
    }
}