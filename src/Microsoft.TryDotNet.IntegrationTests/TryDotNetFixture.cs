// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class TryDotNetFixture : IDisposable, IAsyncLifetime
{
    private AspNetProcess? _process;
    public Uri? Url { get; private set; }

    public async Task InitializeAsync()
    {

        _process = new AspNetProcess();
        Url = await _process.Start();

    }


    public Task DisposeAsync()
    {             
        _process!.Dispose();
        return Task.CompletedTask;
    }


    public void Dispose()
    {
        _process?.Dispose();
    }
}