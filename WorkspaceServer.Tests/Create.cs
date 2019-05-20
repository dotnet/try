// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.Tests;
using MLS.Agent.CommandLine;
using Recipes;
using WorkspaceServer.Packaging;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public static class Create
    {
        public static async Task<Package> ConsoleWorkspaceCopy([CallerMemberName] string testName = null, bool isRebuildable = false, IScheduler buildThrottleScheduler = null) =>
            await Package.Copy(
                await Default.ConsoleWorkspace(),
                testName,
                isRebuildable,
                buildThrottleScheduler);

        public static async Task<Package> WebApiWorkspaceCopy([CallerMemberName] string testName = null) =>
            await Package.Copy(
                await Default.WebApiWorkspace(),
                testName);

        public static async Task<Package> XunitWorkspaceCopy([CallerMemberName] string testName = null) =>
            await Package.Copy(
                await Default.XunitWorkspace(),
                testName);

        public static async Task<Package> NetstandardWorkspaceCopy(
            [CallerMemberName] string testName = null,
            DirectoryInfo parentDirectory = null) =>
            await Package.Copy(
                await Default.NetstandardWorkspace(),
                testName,
                parentDirectory: parentDirectory);

        public static Package EmptyWorkspace([CallerMemberName] string testName = null, IPackageInitializer initializer = null, bool isRebuildablePackage = false)
        {
            if (!isRebuildablePackage)
            {
                return new NonrebuildablePackage(directory: Package.CreateDirectory(testName), initializer: initializer);
            }

            return new RebuildablePackage(directory: Package.CreateDirectory(testName), initializer: initializer);
        }

        public static async Task<(string packageName, DirectoryInfo addSource)> NupkgWithBlazorEnabled([CallerMemberName] string testName = null)
        {
            DirectoryInfo destination = new DirectoryInfo(
                Path.Combine(Package.DefaultPackagesDirectory.FullName, "nupkgs"));
            var asset = await NetstandardWorkspaceCopy(testName, destination);
            var packageName = asset.Directory.Name;
            var console = new TestConsole();
            await PackCommand.Do(new PackOptions(asset.Directory, enableBlazor: true, packageName: packageName), console);
            var nupkg = asset.Directory.GetFiles("*.nupkg").Single();

            return (packageName, nupkg.Directory);
        }

        public static async Task<IPackage> InstalledPackageWithBlazorEnabled([CallerMemberName] string testName = null)
        {
            var (packageName, addSource) = await NupkgWithBlazorEnabled(testName);
            var destination = Package.DefaultPackagesDirectory;
            await InstallCommand.Do(new InstallOptions(addSource, packageName, destination), new TestConsole());

            var strategy = new WebAssemblyAssetFinder(destination);
            return await strategy.Find<IPackage>(packageName);
        }

        public static string SimpleWorkspaceRequestAsJson(
            string consoleOutput = "Hello!",
            string workspaceType = null)
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
