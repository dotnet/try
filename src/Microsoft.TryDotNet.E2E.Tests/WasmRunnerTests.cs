using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.TryDotNet.E2E.Tests;

public class WasmRunnerTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _playwright;

    public WasmRunnerTests(PlaywrightFixture playwright)
    {
        _playwright = playwright;
    }
    
    [Fact]
    public void Test1()
    {
        throw new NotImplementedException();    
    }
}