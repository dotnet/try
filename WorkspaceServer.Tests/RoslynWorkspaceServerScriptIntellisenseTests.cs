// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.Tests;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Servers.Scripting;
using WorkspaceServer.Packaging;
using Xunit;
using Xunit.Abstractions;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public class RoslynWorkspaceServerScriptIntellisenseTests : WorkspaceServerTestsCore
    {
        public RoslynWorkspaceServerScriptIntellisenseTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override  Task<(ICodeRunner runner, Package workspace)> GetRunnerAndWorkspaceBuild(string testName = null)
        {
            return Task.FromResult(((ICodeRunner)new ScriptingWorkspaceServer(),(Package) new NonrebuildablePackage("script")));
        }
        protected override ILanguageService GetLanguageService(
            [CallerMemberName] string testName = null) => new RoslynWorkspaceServer(
            PackageRegistry.CreateForHostedMode());

        [Fact]
        public async Task Get_signature_help_for_invalid_location_return_empty()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
  public static void Main()
  {
    foreach (var i in Fibonacci().Take())$$
    {
      Console.WriteLine(i);
    }
  }

  private static IEnumerable<int> Fibonacci()
  {
    int current = 1, next = 1;

    while (true)
    {
      yield return current;
      next = current + (current = next);
    }
  }
}";
            var (processed, markLocation) = CodeManipulation.ProcessMarkup(code);

            var ws = new Workspace(buffers: new[] { new Buffer("", processed, markLocation) });
            var request = new WorkspaceRequest(ws, activeBufferId: "");
            var server = GetLanguageService();
            var result = await server.GetSignatureHelp(request);
            result.Should().NotBeNull();
            result.Signatures.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task Can_show_signature_help_for_extensions()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
  public static void Main()
  {
    foreach (var i in Fibonacci().Take($$))
    {
      Console.WriteLine(i);
    }
  }

  private static IEnumerable<int> Fibonacci()
  {
    int current = 1, next = 1;

    while (true)
    {
      yield return current;
      next = current + (current = next);
    }
  }
}";
            var (processed, markLocation) = CodeManipulation.ProcessMarkup(code);
            var ws = new Workspace( buffers: new[] { new Buffer("", processed, markLocation) });
            var request = new WorkspaceRequest(ws, activeBufferId: "");
            var server = GetLanguageService();
            var result = await server.GetSignatureHelp(request);
            result.Signatures.Should().NotBeEmpty();
            result.Signatures.First().Label.Should().Be("IEnumerable<TSource> Enumerable.Take<TSource>(IEnumerable<TSource> source, int count)");
            result.Signatures.First().Documentation.Value.Should().Be("Returns a specified number of contiguous elements from the start of a sequence.");
        }

        [Fact]
        public async Task Can_show_KeyValuePair_because_it_uses_the_right_reference_assemblies()
        {
            var (processed, markLocation) = CodeManipulation.ProcessMarkup("System.Collections.Generic.$$");

            var ws = new Workspace(buffers: new[] { new Buffer("default.cs", processed, markLocation) });
            var request = new WorkspaceRequest(ws, activeBufferId: "default.cs");
            var server = GetLanguageService();
            var result = await server.GetCompletionList(request);

            result.Items.Should().NotBeNullOrEmpty();
            result.Items.Should().Contain(i => i.DisplayText == "KeyValuePair");
        }

        [Fact]
        public async Task Can_show_completions()
        {
            var (processed, markLocation) = CodeManipulation.ProcessMarkup("var xa = 3;\n$$a");
            var ws = new Workspace(buffers: new[] { new Buffer("default.cs", processed, markLocation) });
            var request = new WorkspaceRequest(ws, activeBufferId: "default.cs");
            var server = GetLanguageService();
            var result = await server.GetCompletionList(request);

            result.Items.Should().NotBeNullOrEmpty();
            result.Items.Should().Contain(i => i.DisplayText == "xa");
        }

        [Fact]
        public async Task Can_get_signatureHelp_for_workspace_with_buffers()
        {
            var container = @"class A
{
    #region nesting
    #endregion
    void Operation()
    {
        var instance = new C();
    }
}";
            var markup = @"class C
{
    public void Foo() { Foo($$ }
}";

            var (processed, markLocation) = CodeManipulation.ProcessMarkup(markup);

            var ws = new Workspace(
                files: new[] { new File("program.cs", container.EnforceLF()) },
                buffers: new[] { new Buffer("program.cs@nesting", processed, markLocation) });


            var request = new WorkspaceRequest(ws, activeBufferId: "program.cs@nesting");
            var server = GetLanguageService();
            var result = await server.GetSignatureHelp(request);

            result.Signatures.Should().NotBeEmpty();
            result.Signatures.First().Label.Should().Be("void C.Foo()");
        }

        [Fact]
        public async Task Can_show_signatureHelp_for_workspace()
        {
            var markup = @"class C
{
    void Foo() { Foo($$ }
}";

            var (processed, markLocation) = CodeManipulation.ProcessMarkup(markup);
            var ws = new Workspace(buffers: new[] { new Buffer("program.cs", processed, markLocation) });

            var request = new WorkspaceRequest(ws, activeBufferId: "program.cs");
            var server = GetLanguageService();
            var result = await server.GetSignatureHelp(request);
            result.Signatures.Should().NotBeEmpty();
            result.Signatures.First().Label.Should().Be("void C.Foo()");
        }

        [Fact]
        public async Task Can_show_all_completion_properties_for_Class_Task()
        {
            var ws = new Workspace(buffers: new[] { new Buffer("default.cs", "System.Threading.Tasks.", 23) });
            var request = new WorkspaceRequest(ws, activeBufferId: "default.cs");
            var server = GetLanguageService();
            var result = await server.GetCompletionList(request);
            var taskCompletionItem = result.Items.First(i => i.DisplayText == "Task");

            taskCompletionItem.DisplayText.Should().Be("Task");
            taskCompletionItem.FilterText.Should().Be("Task");
            taskCompletionItem.Kind.Should().Be("Class");
            taskCompletionItem.SortText.Should().Be("Task");
        }

        [Fact]
        public async Task Get_completion_for_console()
        {
            var ws = new Workspace(workspaceType: "script", buffers: new[] { new Buffer("program.cs", "Console.", 8) });

            var request = new WorkspaceRequest(ws, activeBufferId: "program.cs");

            var server = GetLanguageService();

            var result = await server.GetCompletionList(request);

            result.Items.Should().ContainSingle(item => item.DisplayText == "WriteLine");
        }

        [Fact]
        public async Task Get_signature_help_for_console_writeline()
        {
            var ws = new Workspace(workspaceType: "script", buffers: new[] { new Buffer("program.cs", "Console.WriteLine()", 18) });

            var request = new WorkspaceRequest(ws, activeBufferId: "program.cs");

            var server = GetLanguageService();

            var result = await server.GetSignatureHelp(request);

            result.Signatures.Should().NotBeNullOrEmpty();
            result.Signatures.Should().Contain(signature => signature.Label == "void Console.WriteLine(string format, params object[] arg)");
        }
    }
}
