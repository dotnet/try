// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Utility;
using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.Tests
{
    public class Default
    {
        public static AsyncLazy<PackageRegistry> PackageRegistry { get; } = new AsyncLazy<PackageRegistry>(async () =>
        {
            await _lazyConsole.ValueAsync();
            await _lazyBlazorConsole.ValueAsync();
            await _lazyAspnetWebapi.ValueAsync();
            await _lazyBlazorNodatimeApi.ValueAsync();
            await _lazyFSharpConsole.ValueAsync();
            await _lazyXunit.ValueAsync();
            await _lazyNodaTimeApi.ValueAsync();

            return WorkspaceServer.PackageRegistry.CreateForHostedMode();
        });


        private static AsyncLazy<Package> _lazyConsole = new AsyncLazy<Package>(async () => 
        {
            var packageBuilder = new PackageBuilder("console");
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        });

        private static AsyncLazy<Package> _lazyNodaTimeApi = new AsyncLazy<Package>(async () =>
        {
            var packageBuilder = new PackageBuilder("nodatime.api");
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("NodaTime", "2.3.0");
            packageBuilder.AddPackageReference("NodaTime.Testing", "2.3.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        });

        private static AsyncLazy<Package> _lazyAspnetWebapi = new AsyncLazy<Package>(async () =>
        {
            var packageBuilder = new PackageBuilder("aspnet.webapi");
            packageBuilder.CreateUsingDotnet("webapi");
            packageBuilder.WriteFile("Controllers/CustomController.cs", @"using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.webapi.Controllers
{
    [ApiController]
    [Route(""[controller]"")]
    public class CustomController : ControllerBase
    {
        public CustomController() { }

        [HttpGet(""values"")]
        public IEnumerable<string> Values()
        {
            return new[] { ""value1"", ""value2"" };
        }
    }
}");
            packageBuilder.TrySetLanguageVersion("8.0");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        });

        private static AsyncLazy<Package> _lazyXunit = new AsyncLazy<Package>(async () =>
        {
            var packageBuilder = new PackageBuilder("xunit");
            packageBuilder.CreateUsingDotnet("xunit", "tests");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            packageBuilder.DeleteFile("UnitTest1.cs");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        });

        public static AsyncLazy<Package> _lazyBlazorConsole = new AsyncLazy<Package>(async () =>
        {
            var packageBuilder = new PackageBuilder("blazor-console");
            packageBuilder.CreateUsingDotnet("classlib");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        });

        public static AsyncLazy<Package2> _lazyBlazorNodatimeApi = new AsyncLazy<Package2>(async () =>
        {
            var workingDirectory = Package.DefaultPackagesDirectory;
            var dotnet = new Dotnet(workingDirectory);
            var tools = await dotnet.ToolList(workingDirectory);
            if (tools.Contains("blazor-nodatime.api"))
            {
                return await new WebAssemblyAssetFinder(new FileSystemDirectoryAccessor(workingDirectory))
                    .Find<Package2>("blazor-nodatime.api");
            }

            using (var dir = DisposableDirectory.Create())
            {
                var subDir = dir.Directory.CreateSubdirectory("blazor-nodatime.api");
                dotnet = new Dotnet(subDir);
                await dotnet.New("classlib");
                await dotnet.AddPackage("NodaTime", "2.4.4");
                await dotnet.AddPackage("NodaTime.Testing", "2.4.4");
                await dotnet.AddPackage("Newtonsoft.Json");

                var console = new TestConsole();
                var name = await PackCommand.Do(new PackOptions(subDir, enableWasm: true, packageName: "blazor-nodatime.api"), console);
                return await new PackageInstallingWebAssemblyAssetFinder(new FileSystemDirectoryAccessor(workingDirectory), new PackageSource(subDir.FullName))
                    .Find<Package2>("blazor-nodatime.api");
            }
        });

        public static AsyncLazy<PackageBase> _lazyFSharpConsole = new AsyncLazy<PackageBase>(async () =>
        {
            var packageBuilder = new PackageBuilder("fsharp-console");
            packageBuilder.CreateUsingDotnet("console", language: "F#");
            var package = packageBuilder.GetPackage() as PackageBase;
            await package.EnsureReady(new Budget());
            return package;
        });

        public static Task<Package> ConsoleWorkspace() =>  _lazyConsole.ValueAsync();

        public static Task<Package> WebApiWorkspace() => _lazyAspnetWebapi.ValueAsync();

        public static Task<Package> XunitWorkspace() => _lazyXunit.ValueAsync();

        public static Task<Package> NetstandardWorkspace() => _lazyBlazorConsole.ValueAsync();
    }
}