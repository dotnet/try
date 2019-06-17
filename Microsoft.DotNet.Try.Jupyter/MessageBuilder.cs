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
            var newHeader = new Header(messageType: messageType, messageId: Guid.NewGuid().ToString(), version:"5.3", username: Constants.USERNAME, session:session, date: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            return newHeader;
        }

        public Message CreateMessage(JupyterMessageContent content, Header parentHeader, IReadOnlyList<IReadOnlyList<byte>> identifiers = null, string signature = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var messageType = GetMessageType(content);
            var session = parentHeader.Session;

            var message = new Message(CreateHeader(messageType, session), parentHeader: parentHeader, content: content, identifiers: identifiers, signature:signature);


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