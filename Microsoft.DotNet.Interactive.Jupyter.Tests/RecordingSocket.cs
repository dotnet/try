// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class RecordingDispatcher : IMessageDispatcher
    {
        private readonly List<JupyterMessageContent> _messages;
        public IReadOnlyList<JupyterMessageContent> Messages => _messages;

        public IEnumerable<JupyterReplyMessageContent> ReplyMessages => _messages.OfType<JupyterReplyMessageContent>();
        public IEnumerable<JupyterPubSubMessageContent> PubSubMessages => _messages.OfType<JupyterPubSubMessageContent>();

        public RecordingDispatcher()
        {
            _messages = new List<JupyterMessageContent>();
        }
        public void Dispatch(JupyterPubSubMessageContent messageContent, Message request)
        {
           _messages.Add(messageContent);
        }

        public void Dispatch(JupyterReplyMessageContent messageContent, Message request)
        {
            _messages.Add(messageContent);
        }
    }
}