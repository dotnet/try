// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public interface IMessageBuilder
    {
        Message CreateMessage(JupyterMessageContent content, Header parentHeader, List<byte[]> identifiers = null);
    }

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

            var message = messageBuilder.CreateMessage(content, request.Header);
            message.Identifiers = request.Identifiers;
            message.Signature = request.Signature;
            return message;
        }
    }
}