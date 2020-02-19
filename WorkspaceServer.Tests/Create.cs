// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Cors;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.Tests;
using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Tests;
using Recipes;
using WorkspaceServer.Packaging;
using WorkspaceServer.Tests.Packaging;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public static class Create
    {
        public static Action<PackageBuilder> ConsoleConfiguration { get; } = packageBuilder =>
        {
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
        };

        public static Task<IPackage> NewPackage(string name, Action<PackageBuilder> configure = null, bool createRebuildablePackage = false)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var package = EmptyWorkspace(name);
            return NewPackage(package.Name, package.Directory, configure, createRebuildablePackage);
        }

        public static async Task<IPackage> NewPackage(string name, DirectoryInfo directory, Action<PackageBuilder> configure = null, bool createRebuildablePackage = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            var packageBuilder = new PackageBuilder(name)
            {
                Directory = directory,
                CreateRebuildablePackage = createRebuildablePackage
            };


            configure?.Invoke(packageBuilder);
            var package = packageBuilder.GetPackage();

            await package.EnsureReady(new Budget());

            return package;
        }

        public static async Task<Package> ConsoleWorkspaceCopy([CallerMemberName] string testName = null, bool isRebuildable = false, IScheduler buildThrottleScheduler = null) =>
            await PackageUtilities.Copy(
                await Default.ConsoleWorkspace(),
                testName,
                isRebuildable,
                buildThrottleScheduler);

        public static async Task<Package> WebApiWorkspaceCopy([CallerMemberName] string testName = null) =>
            await PackageUtilities.Copy(
                await Default.WebApiWorkspace(),
                testName);

        public static async Task<Package> XunitWorkspaceCopy([CallerMemberName] string testName = null) =>
            await PackageUtilities.Copy(
                await Default.XunitWorkspace(),
                testName);

        public static async Task<Package> NetstandardWorkspaceCopy(
            [CallerMemberName] string testName = null,
            DirectoryInfo parentDirectory = null) =>
            await PackageUtilities.Copy(
                await Default.NetstandardWorkspace(),
                testName,
                parentDirectory: parentDirectory);

        public static Package EmptyWorkspace([CallerMemberName] string testName = null, IPackageInitializer initializer = null, bool isRebuildablePackage = false)
        {
            if (!isRebuildablePackage)
            {
                return new NonrebuildablePackage(directory: PackageUtilities.CreateDirectory(testName), initializer: initializer);
            }

            return new RebuildablePackage(directory: PackageUtilities.CreateDirectory(testName), initializer: initializer);
        }

        public static async Task<(string packageName, DirectoryInfo addSource)> NupkgWithBlazorEnabled([CallerMemberName] string testName = null)
        {
            DirectoryInfo destination = new DirectoryInfo(
                Path.Combine(Package.DefaultPackagesDirectory.FullName, "nupkgs"));
            var asset = await NetstandardWorkspaceCopy(testName, destination);
            var packageName = asset.Directory.Name;
            var console = new TestConsole();
            await PackCommand.Do(new PackOptions(asset.Directory, enableWasm: true, packageName: packageName), console);
            var nupkg = asset.Directory.GetFiles("*.nupkg").Single();

            return (packageName, nupkg.Directory);
        }

        public static async Task<IPackage> InstalledPackageWithBlazorEnabled([CallerMemberName] string testName = null)
        {
            var (packageName, addSource) = await NupkgWithBlazorEnabled(testName);
            var destination = Package.DefaultPackagesDirectory;
            await InstallCommand.Do(new InstallOptions(packageName, new PackageSource(addSource.FullName), destination), new TestConsole());

            var strategy = new WebAssemblyAssetFinder(new FileSystemDirectoryAccessor(destination));
            return await strategy.Find<IPackage>(packageName);
        }

        public static string SimpleWorkspaceRequestAsJson(
            string consoleOutput = "Hello!",
            string workspaceType = null,
            string workspaceLanguage = "csharp")
        {
            var workspace = Workspace.FromSource(
                SimpleConsoleAppCodeWithoutNamespaces(consoleOutput),
                workspaceType,
                "Program.cs"
            );

            return new WorkspaceRequest(workspace, requestId: "TestRun").ToJson();
        }

        public static string SimpleConsoleAppCodeWithoutNamespaces(string consoleOutput)
        {
            var code = $@"
using System;

public static class Hello
{{
    public static void Main()
    {{
        Console.WriteLine(""{consoleOutput}"");
    }}
}}";
            return code.EnforceLF();
        }
    }
}
