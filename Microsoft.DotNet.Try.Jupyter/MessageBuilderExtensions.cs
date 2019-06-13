// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public static class MessageBuilderExtensions
    {
        public static Message CreateResponseMessage(this IMessageBuilder messageBuilder, JupyterMessageContent content,
            Message request)
        {
            if (messageBuilder == null)
            {
                throw new ArgumentNullException(nameof(messageBuilder));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var replyMessage = messageBuilder.CreateMessage(content, request.Header, request.Identifiers);
            if (request.Signature != null)
            {
                replyMessage.Signature = request.Signature;
            }

            return replyMessage;
        }
    }
}