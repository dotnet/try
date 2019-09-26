// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal class MessageDispatcher : IMessageDispatcher
    {
        private readonly IPubSubChannel _pubSubChannel;
        private readonly IReplyChannel _replyChannel;
        private readonly string _kernelIdentity;

        public MessageDispatcher(IPubSubChannel pubSubChannel, IReplyChannel replyChannel, string kernelIdentity)
        {
            if (string.IsNullOrWhiteSpace(kernelIdentity))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(kernelIdentity));
            }

            _pubSubChannel = pubSubChannel ?? throw new ArgumentNullException(nameof(pubSubChannel));
            _replyChannel = replyChannel ?? throw new ArgumentNullException(nameof(replyChannel));
            _kernelIdentity = kernelIdentity;
        }

        public void Dispatch(JupyterPubSubMessageContent messageContent, Message request)
        {
            _pubSubChannel.Publish(messageContent, request, _kernelIdentity);}

        public void Dispatch(JupyterReplyMessageContent messageContent, Message request)
        {
            _replyChannel.Reply(messageContent, request);
        }
    }
}