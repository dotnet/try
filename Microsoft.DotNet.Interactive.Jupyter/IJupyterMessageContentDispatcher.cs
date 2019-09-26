// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public interface IJupyterMessageContentDispatcher     
    {
        void Dispatch(JupyterPubSubMessageContent messageContent, JupyterMessage request);
        void Dispatch(JupyterReplyMessageContent messageContent, JupyterMessage request);
    }
}