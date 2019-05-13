// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Builder;
using WorkspaceServer;
using WorkspaceServer.Packaging;

namespace MLS.Agent.Blazor
{
    internal sealed class BlazorPackageConfiguration
    {
        public static void Configure(
            IApplicationBuilder app, 
            IServiceProvider serviceProvider, 
            PackageRegistry registry, 
            Budget budget,
            bool prepareIfNeeded)
        {
            List<Task> prepareTasks = new List<Task>();

            foreach (var builderFactory in registry)
            {
                var builder = builderFactory.Result;
                if (builder.BlazorSupported)
                {
                    var package = (BlazorPackage) builder.GetPackage();
                    if (PackageHasBeenBuiltAndHasBlazorStuff(package))
                    {
                        SetupMappingsForBlazorContentsOfPackage(package, app);
                    }
                    else if(prepareIfNeeded)
                    {
                        prepareTasks.Add(Task.Run(() => package.EnsureReady(budget)).ContinueWith(t => {
                            if (t.IsCompletedSuccessfully)
                            {
                                SetupMappingsForBlazorContentsOfPackage(package, app);
                            }
                        }));
                    }
                }
            }

            Task.WaitAll(prepareTasks.ToArray());
        }

        private static void SetupMappingsForBlazorContentsOfPackage(BlazorPackage package, IApplicationBuilder builder)
        {
            builder.Map(package.CodeRunnerPath, appBuilder =>
            {
                var blazorEntryPoint = package.BlazorEntryPointAssemblyPath;
                appBuilder.UsePathBase(package.CodeRunnerPathBase);
                appBuilder.UseBlazor(new BlazorOptions { ClientAssemblyPath = blazorEntryPoint.FullName });
            });
        }

        private static bool PackageHasBeenBuiltAndHasBlazorStuff(BlazorPackage package)
        {
            return package.BlazorEntryPointAssemblyPath.Exists;
        }
    }
}
