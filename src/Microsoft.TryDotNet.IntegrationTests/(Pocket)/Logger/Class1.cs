// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.TryDotNet.IntegrationTests;
using Xunit.Abstractions;

namespace Pocket;

internal partial class LogEvents
{
    public static IDisposable SubscribeToPocketLogger(this ITestOutputHelper output) =>
        Subscribe(
            e => output.WriteLine(e.ToLogString()),
            AssembliesPublishingPocketLoggerEvents);

    public static Assembly[] AssembliesPublishingPocketLoggerEvents =>
    [
        typeof(Microsoft.TryDotNet.Program).Assembly, // Microsoft.TryDotNet.dll
        typeof(Kernel).Assembly, // Microsoft.DotNet.Interactive.dll
        typeof(CSharpProjectKernel).Assembly, // Microsoft.DotNet.Interactive.CSharpProject.dll
        typeof(InteractiveDocument).Assembly, // Microsoft.DotNet.Interactive.Documents.dll,
        typeof(IntegratedServicesFixture).Assembly // Microsoft.TryDotNet.IntegrationTests.dll
    ];
}