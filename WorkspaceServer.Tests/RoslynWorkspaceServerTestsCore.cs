// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WorkspaceServer.Packaging;
using WorkspaceServer.Servers.Roslyn;
using Xunit.Abstractions;
using System.Threading.Tasks;

namespace WorkspaceServer.Tests
{
    public abstract class RoslynWorkspaceServerTestsCore : WorkspaceServerTestsCore
    {
        protected RoslynWorkspaceServerTestsCore(ITestOutputHelper output) : base(output)
        {
        }

        protected override async Task<ILanguageService> GetLanguageServiceAsync() => new RoslynWorkspaceServer(await Default.PackageRegistry.ValueAsync());

        protected override async Task<ICodeCompiler> GetCodeCompilerAsync() => new RoslynWorkspaceServer(await Default.PackageRegistry.ValueAsync());

        protected override async Task<ICodeRunner> GetCodeRunnerAsync() => new RoslynWorkspaceServer(await Default.PackageRegistry.ValueAsync());
    }
}