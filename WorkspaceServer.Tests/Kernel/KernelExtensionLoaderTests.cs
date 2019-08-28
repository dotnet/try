using System.Threading.Tasks;
using Xunit;
using System.IO;
using WorkspaceServer.Tests.Kernel;
using Xunit.Abstractions;
using MLS.Agent.Tools;
using Microsoft.DotNet.Interactive;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using System.Linq;
using MLS.Agent.Tools.Tests;
using Microsoft.DotNet.Interactive.Commands;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WorkspaceServer.Tests
{
    public class KernelExtensionLoaderTests : CSharpKernelTestBase
    {
        public KernelExtensionLoaderTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Can_load_from_assembly()
        {
            var directory = Create.EmptyWorkspace().Directory;
            var extensionDll = await CreateExtensionInDirectory(directory);

            var kernel = CreateKernel();
            await new KernelExtensionLoader().LoadFromAssembly(extensionDll, kernel);

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));

        }

        [Fact]
        public async Task Can_load_from_nuget_package()
        {
            var nugetPackageDirectory = new InMemoryDirectoryAccessor(Create.EmptyWorkspace().Directory.Subdirectory("myNugetPackage"))
            {
                ($"2.0.0/lib/netstandard2.0/myNugetPackage.dll", ""),
            }.CreateFiles();

            var extensionDll = await CreateExtensionInDirectory((DirectoryInfo)nugetPackageDirectory.GetFullyQualifiedPath(new RelativeDirectoryPath("2.0.0/interactive-extensions")));

            var loadExtensionCommand = new LoadCSharpExtension(new NugetPackageReference("myNugetPackage"), new List<FileInfo>() { nugetPackageDirectory.GetFullyQualifiedFilePath($"2.0.0/lib/netstandard2.0/myNugetPackage.dll") });
            var kernel = CreateKernel();
            await new KernelExtensionLoader().LoadFromNuGetPackage(loadExtensionCommand, kernel);

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));
        }

        private static async Task<FileInfo> CreateExtensionInDirectory(DirectoryInfo extensionDirectory, [CallerMemberName] string extensionName = null)
        {
            var directory = Create.EmptyWorkspace(extensionName).Directory;
            var microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

            new InMemoryDirectoryAccessor(directory)
                {
                    ( "Extension.cs", $@"
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
public class TestKernelExtension : IKernelExtension
{{
    public async Task OnLoadAsync(IKernel kernel)
    {{
        await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));
    }}
}}
" ),
                    ($"{extensionName}.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
    <ItemGroup>
    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>
</Project>
")
                }
                 .CreateFiles();

            var buildResult = await new Dotnet(directory).Build($"/p:OutDir={extensionDirectory.FullName}");
            buildResult.ThrowOnFailure();

            var extensionDll = extensionDirectory
                            .GetFiles("TestExtension.dll", SearchOption.AllDirectories)
                            .Single();

            return extensionDll;
        }

    }
}