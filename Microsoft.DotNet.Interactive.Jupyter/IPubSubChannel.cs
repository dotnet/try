// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal interface IPubSubChannel
    {
        void Publish(JupyterPubSubMessageContent messageContent, JupyterMessage request, string kernelIdentity);
    }
}