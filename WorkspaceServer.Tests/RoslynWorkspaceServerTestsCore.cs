// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WorkspaceServer.Packaging;
using WorkspaceServer.Servers.Roslyn;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests
{
    public abstract class RoslynWorkspaceServerTestsCore : WorkspaceServerTestsCore
    {
        private IPackageFinder _packageFinder;

        protected RoslynWorkspaceServerTestsCore(ITestOutputHelper output) : base(output)
        {
        }

        protected override ILanguageService GetLanguageService() => new RoslynWorkspaceServer(GetPackageFinder());

        protected override ICodeCompiler GetCodeCompiler() => new RoslynWorkspaceServer(GetPackageFinder());

        protected override ICodeRunner GetCodeRunner() => new RoslynWorkspaceServer(GetPackageFinder());

        private IPackageFinder GetPackageFinder() => _packageFinder ?? (_packageFinder = PackageRegistry.CreateForHostedMode());
    }
}