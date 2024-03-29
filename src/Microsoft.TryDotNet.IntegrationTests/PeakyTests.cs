// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Peaky.Client;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.TryDotNet.IntegrationTests;

public class PeakyTests
{
    private readonly ITestOutputHelper _output;

    private readonly PeakyClient _peakyClient = new(new HttpClient());

    public PeakyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory(Skip = "Work in progress")]
    [ClassData(typeof(PeakyTestDiscovery))]
    public async Task The_peaky_test_passes(Uri url)
    {
        var result = await _peakyClient.GetTestResultAsync(url);

        _output.WriteLine(result.Content);

        result.Passed.Should().BeTrue();
    }
}