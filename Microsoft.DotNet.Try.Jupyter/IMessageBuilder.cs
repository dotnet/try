// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public interface IMessageBuilder
    {
        Header CreateHeader(string messageType, string session);

        Message CreateMessage( JupyterMessageContent content, Header parentHeader);

        Message CreateMessage(string messageType, JupyterMessageContent content, Header parentHeader);
    }
}