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
            var extensionDll = await CreateAndGetExtensionDll(directory);

            var kernel = CreateKernel();
            await new KernelExtensionLoader().LoadFromAssembly(extensionDll, kernel);

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));

        }

        //[Fact]
        //public async Task Can_load_from_nuget_package()
        //{
        //    var directory = Create.EmptyWorkspace().Directory;

        //    var extensionDll = await CreateAndGetExtensionDll(directory);

        //    var nugetPackageDirectory = new InMemoryDirectoryAccessor(directory.Subdirectory("myNugetPackage"))
        //    {
        //        ($"2.0.0/interactive-extensions/{extensionDll.Name}", extensionDll.Read()),
        //        ($"2.0.0/lib/netstandard2.0/myNugetPackage.dll", "")
        //    }.CreateFiles();

        //    File.Copy(extensionDll.FullName, "");

        //    var nugetPackageAdded = new NuGetPackageAdded(new NugetPackageReference("myNugetPackage"), new List<FileInfo>() { nugetPackageDirectory.GetFullyQualifiedFilePath($"2.0.0/interactive-extensions/{extensionDll.Name}") });
        //    var kernel = CreateKernel();
        //    await new KernelExtensionLoader().LoadFromNuGetPackage(nugetPackageAdded, kernel);

        //    KernelEvents.Should()
        //              .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
        //                                  e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));

        //    throw new NotImplementedException();
        //}

        private static async Task<FileInfo> CreateAndGetExtensionDll(DirectoryInfo directory)
        {
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
                    ("TestExtension.csproj", $@"
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

            var buildResult = await new Dotnet(directory).Build();
            buildResult.ThrowOnFailure();

            var extensionDll = directory
                            .GetDirectories("bin", SearchOption.AllDirectories)
                            .Single()
                            .GetFiles("TestExtension.dll", SearchOption.AllDirectories)
                            .Single();
            return extensionDll;
        }

    }
}