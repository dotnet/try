// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class KernelCommandBase : IKernelCommand
    {
        [JsonIgnore]
        public KernelCommandInvocation Handler { get; set; }

        public async Task InvokeAsync(KernelInvocationContext context)
        {
            await Handler(context);
        }
    }
}