// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public interface IMessageBuilder
    {
        Header CreateHeader(string messageType, string session);

        Message CreateResponseMessage(JupyterMessageContent content, Message request);

        Message CreateMessage(JupyterMessageContent content, Header parentHeader);

        Message CreateMessage(JupyterMessageContent content, Header parentHeader, List<byte[]> identifiers);

        Message CreateMessage(string messageType, JupyterMessageContent content, Header parentHeader);
    }
}