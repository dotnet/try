﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.Events;
using Xunit.Abstractions;

namespace Pocket;

internal partial class LogEvents
{
    public static IDisposable SubscribeToPocketLogger(this ITestOutputHelper output) =>
        Subscribe(
            e => output.WriteLine(e.ToLogString()),
            new[]
            {
                typeof(LogEvents).Assembly,
                typeof(KernelEvent).Assembly,
                typeof(CSharpProjectKernel).Assembly
            });
}