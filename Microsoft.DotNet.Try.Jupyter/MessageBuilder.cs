// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class MessageBuilder : IMessageBuilder
    {
        private Header CreateHeader(string messageType, string session)
        {
            var newHeader = new Header
            {
                Username = Constants.USERNAME,
                Session = session,
                MessageId = Guid.NewGuid().ToString(),
                MessageType = messageType,
                Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Version = "5.3"
            };

            return newHeader;
        }

        public Message CreateMessage(JupyterMessageContent content, Header parentHeader, List<byte[]> identifiers = null)
        {
            var messageType = GetMessageType(content);
            var session = parentHeader.Session;

            var message = new Message
            {
                ParentHeader = parentHeader,
                Header = CreateHeader(messageType, session),
                Content = content,
                Identifiers = identifiers
            };

            return message;
        }

        private string GetMessageType(JupyterMessageContent source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var attribute = source.GetType().GetCustomAttribute<JupyterMessageTypeAttribute>() ?? throw new InvalidOperationException("source is not annotated with JupyterMessageTypeAttribute");

            return attribute.Type;
        }
    }
}