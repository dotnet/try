// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using Newtonsoft.Json.Linq;

namespace WorkspaceServer.Tests.Kernel
{
    internal static class StreamKernelEventExtensions
    {
        public static JObject ToJObject(this StreamKernelEvent streamKernelEvent)
        {
            return JObject.Parse(streamKernelEvent.Serialize());
        }
    }
}