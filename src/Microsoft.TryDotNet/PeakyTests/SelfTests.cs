// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.Events;
using Peaky;

namespace Microsoft.TryDotNet.PeakyTests;

public class SelfTests : IPeakyTest, IHaveTags, IApplyToApplication
{
    public string[] Tags => ["self"];

    public async Task<Prebuild> Console_prebuild_is_ready()
    {
        var prebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(enableBuild: false);

        prebuild.Directory.Exists.Should().BeTrue();

        var subdirectories = prebuild.Directory.GetDirectories();

        subdirectories.Should().Contain(d => d.Name == "bin");
        subdirectories.Should().Contain(d => d.Name == "obj");
        prebuild.Directory.GetFiles().Should().Contain(f => f.Name == "console.csproj.interactive.workspaceData.cache");

        return prebuild;
    }

    public async Task<object> Can_get_completions()
    {
         using var kernel = Program.CreateKernel();

        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"// content will be replaced") })));
        await kernel.SendAsync(new OpenDocument("Program.cs"));

        var code =  """
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text;
                    using System.Globalization;
                    using System.Text.RegularExpressions;
                    
                    C|
                    """;

        var result = await kernel.SendAsync(new RequestCompletions(code.Replace("|", ""), new LinePosition(7, 1)));

        result.Events.Should().ContainSingle(e => e is CompletionsProduced);
        
        var completionsProduced = result.Events.OfType<CompletionsProduced>().Single();

        completionsProduced.Completions.Should().NotBeEmpty();

        return new
        {
            Count = completionsProduced.Completions.Count(),
            CompletionsProduced = completionsProduced
        };
    }

    public async Task Can_get_signature_help()
    {
        using var kernel = Program.CreateKernel();

        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"// content will be replaced") })));
        await kernel.SendAsync(new OpenDocument("Program.cs"));

        var code = """
            
            public class Program
            {
                public static void Main(string[] args)
                {
                    var fileInfo = new System.IO.FileInfo("test.file");
                    fileInfo.CopyTo(
                }
            }

            """;

        var result = await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(6, 24)));
        using var _ = new AssertionScope();

        var sigHelpProduced = result.Events
                                    .Should()
                                    .ContainSingle(e => e is SignatureHelpProduced)
                                    .Which
                                    .As<SignatureHelpProduced>();

        sigHelpProduced
            .ActiveSignatureIndex
            .Should()
            .Be(0);
        sigHelpProduced
            .ActiveParameterIndex
            .Should()
            .Be(0);
        sigHelpProduced
            .Signatures
            .Should()
            .BeEquivalentTo(new[]
            {
                new SignatureInformation(
                    "FileInfo FileInfo.CopyTo(string destFileName)",
                    new FormattedValue("text/markdown", "Copies an existing file to a new file, disallowing the overwriting of an existing file."),
                    new[]
                    {
                        new ParameterInformation(
                            "string destFileName",
                            new FormattedValue("text/markdown", "**destFileName**: The name of the new file to copy to."))
                    }),
                new SignatureInformation(
                    "FileInfo FileInfo.CopyTo(string destFileName, bool overwrite)",
                    new FormattedValue("text/markdown", "Copies an existing file to a new file, allowing the overwriting of an existing file."),
                    new[]
                    {
                        new ParameterInformation(
                            "string destFileName",
                            new FormattedValue("text/markdown", "**destFileName**: The name of the new file to copy to.")),
                        new ParameterInformation(
                            "bool overwrite",
                            new FormattedValue("text/markdown", "**overwrite**: true to allow an existing file to be overwritten; otherwise, false."))
                    })
            });
    }

    public bool AppliesToApplication(string application) => application == "trydotnet";
}