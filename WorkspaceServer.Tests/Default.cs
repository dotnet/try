// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.Tests
{
    public static class Default
    {
        private static readonly PackageRegistry DefaultPackages = PackageRegistry.CreateForHostedMode();

        public static async Task<Package> ConsoleWorkspace() =>  await DefaultPackages.Get<Package>("console");

        public static async Task<Package> WebApiWorkspace() =>  await DefaultPackages.Get<Package>("aspnet.webapi");

        public static async Task<Package> XunitWorkspace() =>  await DefaultPackages.Get<Package>("xunit");

        public static async Task<Package> NetstandardWorkspace() =>  await DefaultPackages.Get<Package>("blazor-console");
    }
}