// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class RecordingJupyterMessageContentDispatcher : IJupyterMessageContentDispatcher
    {
        private readonly List<JupyterMessageContent> _messages;
        private readonly List<JupyterPubSubMessageContent> _pubSubMessages;
        private readonly List<JupyterReplyMessageContent> _replyMessages;
        public IReadOnlyList<JupyterMessageContent> Messages => _messages;

        public IEnumerable<JupyterReplyMessageContent> ReplyMessages => _replyMessages;
        public IEnumerable<JupyterPubSubMessageContent> PubSubMessages => _pubSubMessages;

        public RecordingJupyterMessageContentDispatcher()
        {
            _messages = new List<JupyterMessageContent>();
            _pubSubMessages = new List<JupyterPubSubMessageContent>();
            _replyMessages = new List<JupyterReplyMessageContent>();
        }

        public void Dispatch(JupyterPubSubMessageContent messageContent)
        {
           _messages.Add(messageContent);
           _pubSubMessages.Add(messageContent);
        }

        public void Dispatch(JupyterReplyMessageContent messageContent)
        {
            _messages.Add(messageContent);
            _replyMessages.Add(messageContent);
        }
    }
}
