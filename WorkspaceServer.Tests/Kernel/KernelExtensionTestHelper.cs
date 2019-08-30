// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Tests;

namespace WorkspaceServer.Tests.Kernel
{
    internal static class KernelExtensionTestHelper
    {
        internal static async Task<FileInfo> CreateExtensionInDirectory(DirectoryInfo extensionDir, DirectoryInfo outputDir)
        {
            var extensionDll = await CreateExtension(extensionDir);
            var finalExtensionDll = new FileInfo(Path.Combine(outputDir.FullName, Path.GetRandomFileName()+".dll"));
            File.Copy(extensionDll.FullName, finalExtensionDll.FullName);

            return finalExtensionDll;
        }

        internal static async Task<FileInfo> CreateExtension(DirectoryInfo extensionDir)
        {
            var microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

            new InMemoryDirectoryAccessor(extensionDir)
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

            var buildResult = await new Dotnet(extensionDir).Build();
            buildResult.ThrowOnFailure();

            return extensionDir
                               .GetDirectories("bin", SearchOption.AllDirectories)
                               .Single()
                               .GetFiles("TestExtension.dll", SearchOption.AllDirectories)
                               .Single();
        }
    }
}