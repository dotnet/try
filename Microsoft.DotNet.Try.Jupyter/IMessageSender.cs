// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Jupyter.Protocol;

namespace Microsoft.DotNet.Try.Jupyter
{
    public interface IMessageSender
    {
        bool Send(Message message);
    }
}