// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public interface IMessageBuilder
    {
        Message CreateMessage(JupyterMessageContent content, Header parentHeader, IReadOnlyList<IReadOnlyList <byte>> identifiers = null, string signature = null);
    }
}