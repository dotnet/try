// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    internal class JupyterMessageContentDispatcher : IJupyterMessageContentDispatcher
    {
        private readonly IPubSubChannel _pubSubChannel;
        private readonly IReplyChannel _replyChannel;
        private readonly string _kernelIdentity;
        private readonly JupyterMessage _request;

        public JupyterMessageContentDispatcher(IPubSubChannel pubSubChannel, IReplyChannel replyChannel, string kernelIdentity, JupyterMessage request)
        {
            if (string.IsNullOrWhiteSpace(kernelIdentity))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(kernelIdentity));
            }

            _pubSubChannel = pubSubChannel ?? throw new ArgumentNullException(nameof(pubSubChannel));
            _replyChannel = replyChannel ?? throw new ArgumentNullException(nameof(replyChannel));
            _kernelIdentity = kernelIdentity;
            _request = request;
        }

        public void Dispatch(JupyterPubSubMessageContent messageContent)
        {
            _pubSubChannel.Publish(messageContent, _request, _kernelIdentity);}

        public void Dispatch(JupyterReplyMessageContent messageContent)
        {
            _replyChannel.Reply(messageContent, _request);
        }
    }
}