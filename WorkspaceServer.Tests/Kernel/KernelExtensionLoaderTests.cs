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
            var extensionOutputDirectory = directory.CreateSubdirectory("extensionOutput");
            var extensionDll = await CreateExtensionDll(directory, extensionOutputDirectory);

            var kernel = CreateKernel();
            await new KernelExtensionLoader().LoadFromAssembly(extensionDll, kernel);

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));

        }

        [Fact]
        public async Task Can_load_from_nuget_package()
        {
            var baseDirectory = Create.EmptyWorkspace().Directory;
            var extensionOutputDirectory = baseDirectory.CreateSubdirectory("interactive-extensions");

            var extensionDll = await CreateExtensionDll(baseDirectory, extensionOutputDirectory);

            var loadExtensionCommand = new LoadCSharpExtension(baseDirectory);
            var kernel = CreateKernel();
            await new KernelExtensionLoader().LoadCSharpExtension(loadExtensionCommand, kernel);

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));
        }

        private static async Task<FileInfo> CreateExtensionDll(DirectoryInfo baseDirectory, DirectoryInfo extensionOutputDirectory)
        {
            var extensionCodeDirectory = baseDirectory.CreateSubdirectory("extensionCode");
            var microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;
            var extensionName = baseDirectory.Name;

            new InMemoryDirectoryAccessor(extensionCodeDirectory)
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

            var buildResult = await new Dotnet(extensionCodeDirectory).Build();
            buildResult.ThrowOnFailure();

            var extensionDll = extensionCodeDirectory
                            .GetDirectories("bin", SearchOption.AllDirectories)
                            .Single()
                            .GetFiles($"{extensionName}.dll", SearchOption.AllDirectories)
                            .Single();

            File.Copy(extensionDll.FullName, Path.Combine(extensionOutputDirectory.FullName, extensionDll.Name));
            return extensionOutputDirectory
                    .GetFiles($"{extensionName}.dll", SearchOption.AllDirectories)
                    .Single(); ;
        }
    }
}