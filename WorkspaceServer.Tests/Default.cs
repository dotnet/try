// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.Tests
{
    public static class Default
    {
        public static PackageRegistry PackageFinder { get; } = PackageRegistry.CreateForHostedMode();

        public static async Task<Package> ConsoleWorkspace() =>  await PackageFinder.Get<Package>("console");

        public static async Task<Package> WebApiWorkspace() =>  await PackageFinder.Get<Package>("aspnet.webapi");

        public static async Task<Package> XunitWorkspace() =>  await PackageFinder.Get<Package>("xunit");

        public static async Task<Package> NetstandardWorkspace() =>  await PackageFinder.Get<Package>("blazor-console");
    }
}