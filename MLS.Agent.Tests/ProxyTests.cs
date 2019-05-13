// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MLS.Agent.Tests
{
    public class ProxyTests
    {
        [Fact]
        public async Task XForwardedPathBase_is_prepended_to_request_url()
        {
            using (var service = new AgentService())
            {
                var message = new HttpRequestMessage(HttpMethod.Get, "/");
                message.Headers.Add("X-Forwarded-PathBase", "/LocalCodeRunner/blazor-console");

                var expected = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width"">
    <base href=""/LocalCodeRunner/blazor-console/"" />
</head>
<body>
    <app>Loading...</app>

    <script src=""interop.js""></script>
    <script src=""_framework/blazor.webassembly.js""></script>
</body>
</html>
";

                var result = await service.SendAsync(message);
                var content = await result.Content.ReadAsStringAsync();
                content.Should().Be(expected);

            }
        }
    }
}
