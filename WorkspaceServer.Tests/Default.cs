// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.Tests
{
    public class Default
    {
        public static AsyncLazy<PackageRegistry> PackageFinder { get; } = new AsyncLazy<PackageRegistry>(async () =>
        {
            var _ = await _lazyConsolePackage.ValueAsync();
            return PackageRegistry.CreateForHostedMode();
        });


        public static AsyncLazy<Package> _lazyConsolePackage = new AsyncLazy<Package>(async () => 
        {
            var packageBuilder = new PackageBuilder("console");
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        });

        public static Task<Package> ConsoleWorkspace() =>  _lazyConsolePackage.ValueAsync();

        public static async Task<Package> WebApiWorkspace()
        {
            var finder = await PackageFinder.ValueAsync();
            return await finder.Get<Package>("aspnet.webapi");
        }

        public static async Task<Package> XunitWorkspace()
        {
            var finder = await PackageFinder.ValueAsync();
            return await finder.Get<Package>("xunit");
        }

        public static async Task<Package> NetstandardWorkspace()
        {
            var finder = await PackageFinder.ValueAsync();
            return await finder.Get<Package>("blazor-console");
        }
    }
}