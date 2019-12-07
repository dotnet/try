// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    public static class StandardIOKernelServerExtensions
    {
        public static Task WriteAsync(
            this StandardIOKernelServer server,
            IKernelCommand kernelCommand,
            int correlationId = -1)
        {
            return server.WriteAsync(
                new StreamKernelCommand
                {
                    Id = correlationId,
                    CommandType = kernelCommand.GetType().Name,
                    Command = kernelCommand
                }.Serialize());
        }
    }
}