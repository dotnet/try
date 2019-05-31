// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Servers.Scripting;
using WorkspaceServer.Packaging;
using Xunit;
using Xunit.Abstractions;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public class RoslynWorkspaceServerScriptDiagnosticsTests : WorkspaceServerTestsCore
    {
        public RoslynWorkspaceServerScriptDiagnosticsTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Get_diagnostics()
        {
            var code = @"addd";
            var (processed, markLocation) = CodeManipulation.ProcessMarkup(code);

            var ws = new Workspace(buffers: new[] { new Buffer("file.csx", processed, markLocation) });
            var request = new WorkspaceRequest(ws, activeBufferId: "file.csx");
            var server = GetLanguageService();
            var result = await server.GetDiagnostics(request);
            result.Diagnostics.Should().NotBeEmpty();
            result.Diagnostics.Should().Contain(diagnostics => diagnostics.Message == "file.csx(1,1): error CS0103: The name \'addd\' does not exist in the current context");
        }

        protected override ILanguageService GetLanguageService() => new RoslynWorkspaceServer(Default.PackageFinder);

        protected override ICodeCompiler GetCodeCompiler() => new RoslynWorkspaceServer(Default.PackageFinder);

        protected override ICodeRunner GetCodeRunner() => new RoslynWorkspaceServer(Default.PackageFinder);
    }
}