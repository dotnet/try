// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Pocket;
using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class IntegratedServicesFixture : IAsyncLifetime
{
    private static readonly CompositeDisposable _disposables = new();

    private static readonly AsyncLazy<TryDotNetServer> _lazyTryDotNetServer;
    private static readonly AsyncLazy<LearnServer> _lazyLearnServer;
    private static readonly AsyncLazy<PlaywrightSession> _lazyPlaywright;
    private static int _instanceCount;

    static IntegratedServicesFixture()
    {
        _lazyTryDotNetServer = new AsyncLazy<TryDotNetServer>(StartTryDotNetServer);
        _lazyLearnServer = new AsyncLazy<LearnServer>(StartLearnServer);
        _lazyPlaywright = new AsyncLazy<PlaywrightSession>(StartPlaywright);
    }

    public async Task InitializeAsync()
    {
        var consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(true);
        await consolePrebuild.EnsureReadyAsync();
    }

    public IntegratedServicesFixture()
    {
        Interlocked.Increment(ref _instanceCount);
    }

    private static async Task<LearnServer> StartLearnServer()
    {
        var server = await LearnServer.StartAsync();
        _disposables.Add(server);
        return server;
    }

    private static async Task<TryDotNetServer> StartTryDotNetServer()
    {
        var server = await TryDotNetServer.StartAsync();
        _disposables.Add(server);
        return server;
    }

    private static async Task<PlaywrightSession> StartPlaywright()
    {
        var server = await PlaywrightSession.StartAsync();
        _disposables.Add(server);
        return server;
    }

    public Task<TryDotNetServer> GetTryDotNetServerAsync() => _lazyTryDotNetServer.ValueAsync();

    public Task<LearnServer> GetLearnServerAsync() => _lazyLearnServer.ValueAsync();

    public Task<PlaywrightSession> GetPlaywrightAsync() => _lazyPlaywright.ValueAsync();

    public Task DisposeAsync()
    {
        _disposables.Dispose();
        return Task.CompletedTask;
    }
}