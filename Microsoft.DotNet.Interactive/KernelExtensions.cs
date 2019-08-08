﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        public static Task<IKernelCommandResult> SendAsync(
           this IKernel kernel,
           IKernelCommand command)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(command, CancellationToken.None);
        }
    }
}