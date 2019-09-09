﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class KernelCommandBase : IKernelCommand
    {
        [JsonIgnore]
        public KernelCommandInvocation Handler { get; set; }

        [JsonIgnore]
        public IKernelCommand Parent { get; }

        protected KernelCommandBase()
        {
            Parent = KernelInvocationContext.Current?.Command;
        }

        public async Task InvokeAsync(KernelInvocationContext context)
        {
            if (Handler == null)
            {
                throw new InvalidOperationException($"{GetType().Name}.{nameof(Handler)} was not set.");
            }

            await Handler(context);
        }
    }
}