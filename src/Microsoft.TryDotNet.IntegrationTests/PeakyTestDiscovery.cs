// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Peaky.Client;
using Peaky.XUnit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class PeakyTestDiscovery : PeakyXunitTestBase
{
    private static readonly Uri _testDiscoveryUri = new("https://mls-monitoring.azurewebsites.net/tests/staging/orchestrator?deployment=true");

    private readonly PeakyClient _peakyClient = new(_testDiscoveryUri);

    public override PeakyClient PeakyClient => _peakyClient;
}