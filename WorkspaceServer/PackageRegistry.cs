// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis.Operations;
using WorkspaceServer.Packaging;

namespace WorkspaceServer
{
    public class PackageRegistry : 
        IPackageFinder,
        IEnumerable<Task<PackageBuilder>>
    {
        private readonly List<IPackageFinder> _packageFinders;

        private readonly ConcurrentDictionary<string, Task<PackageBuilder>> _packageBuilders = new ConcurrentDictionary<string, Task<PackageBuilder>>();
        private readonly ConcurrentDictionary<string, Task<IPackage>> _packages = new ConcurrentDictionary<string, Task<IPackage>>();
        private readonly List<IPackageDiscoveryStrategy> _strategies = new List<IPackageDiscoveryStrategy>();

        public PackageRegistry(
            bool createRebuildablePackages = false,
            PackageSource addSource = null,
            IEnumerable<IPackageFinder> packageFinders = null,
            params IPackageDiscoveryStrategy[] additionalStrategies)
            : this(addSource,
                  new IPackageDiscoveryStrategy[]
                   {
                       new ProjectFilePackageDiscoveryStrategy(createRebuildablePackages),
                       new DirectoryPackageDiscoveryStrategy(createRebuildablePackages)
                   }.Concat(additionalStrategies),
                  packageFinders)
        {
        }

        private PackageRegistry(
            PackageSource addSource,
            IEnumerable<IPackageDiscoveryStrategy> strategies,
            IEnumerable<IPackageFinder> packageFinders = null)
        {
            foreach (var strategy in strategies)
            {
                if (strategy == null)
                {
                    throw new ArgumentException("Strategy cannot be null.");
                }

                _strategies.Add(strategy);
            }
            
            _packageFinders = packageFinders?.ToList() ?? GetDefaultPackageFinders().ToList();
        }

        public void Add(string name, Action<PackageBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            var packageBuilder = new PackageBuilder(name);
            configure(packageBuilder);
            _packageBuilders.TryAdd(name, Task.FromResult(packageBuilder));
        }

        public async Task<T> Get<T>(string packageName, Budget budget = null)
            where T : class, IPackage
        {
            if (packageName == "script")
            {
                packageName = "console";
            }

            var descriptor = new PackageDescriptor(packageName);

            // FIX: (Get) move this into the cache
            var package = await GetPackage2<T>(descriptor);

            if (package == null)
            {
                package = await GetPackageFromPackageBuilder<T>(packageName, budget, descriptor);
            }

            return (T) package;
        }

        private async Task<IPackage> GetPackage2<T>(PackageDescriptor descriptor)
            where T : class, IPackage
        {
            foreach (var packgeFinder in _packageFinders)
            {
                if (await packgeFinder.Find<T>(descriptor) is T pkg)
                {
                   return pkg;
                }
            }

            return default;
        }

        private Task<IPackage> GetPackageFromPackageBuilder<T>(string packageName, Budget budget, PackageDescriptor descriptor)
            where T : IPackage
        {
            return _packages.GetOrAdd(packageName, async name =>
            {
                var packageBuilder = await _packageBuilders.GetOrAdd(
                                         name,
                                         async name2 =>
                                         {
                                             foreach (var strategy in _strategies)
                                             {
                                                 var builder = await strategy.Locate(descriptor, budget);

                                                 if (builder != null)
                                                 {
                                                     return builder;
                                                 }
                                             }

                                             throw new PackageNotFoundException($"Package named \"{name2}\" not found.");
                                         });

                return packageBuilder.GetPackage(budget);
            });
        }

        public static PackageRegistry CreateForTryMode(DirectoryInfo project, PackageSource addSource = null)
        {
            var finders = GetDefaultPackageFinders().Append(new WebAssemblyAssetFinder(Package.DefaultPackagesDirectory, addSource));
            var registry = new PackageRegistry(
                true, 
                addSource,
                finders,
                additionalStrategies: new LocalToolInstallingPackageDiscoveryStrategy(Package.DefaultPackagesDirectory, addSource));

            registry.Add(project.Name, builder =>
            {
                builder.CreateRebuildablePackage = true;
                builder.Directory = project;
            });

            return registry;
        }

        public static PackageRegistry CreateForHostedMode()
        {
            var registry = new PackageRegistry(
                false);

            registry.Add("console",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("console");
                             packageBuilder.TrySetLanguageVersion("8.0");
                             packageBuilder.AddPackageReference("Newtonsoft.Json");
                         });

            registry.Add("nodatime.api",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("console");
                             packageBuilder.TrySetLanguageVersion("8.0");
                             packageBuilder.AddPackageReference("NodaTime", "2.3.0");
                             packageBuilder.AddPackageReference("NodaTime.Testing", "2.3.0");
                             packageBuilder.AddPackageReference("Newtonsoft.Json");
                         });

            registry.Add("aspnet.webapi",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("webapi");
                             packageBuilder.TrySetLanguageVersion("8.0");
                         });

            registry.Add("xunit",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("xunit", "tests");
                             packageBuilder.TrySetLanguageVersion("8.0");
                             packageBuilder.AddPackageReference("Newtonsoft.Json");
                             packageBuilder.DeleteFile("UnitTest1.cs");
                         });

            registry.Add("blazor-console",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("classlib");
                             packageBuilder.AddPackageReference("Newtonsoft.Json");
                         });

            registry.Add("humanizer.api",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("classlib");
                             packageBuilder.AddPackageReference("Newtonsoft.Json");
                             packageBuilder.AddPackageReference("Humanizer");
                         });

            // Todo: soemething about nodatime 2.3 makes blazor toolchain fail to build
            registry.Add("blazor-nodatime.api",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("classlib");
                             packageBuilder.DeleteFile("Class1.cs");
                             packageBuilder.AddPackageReference("NodaTime", "2.4.4");
                             packageBuilder.AddPackageReference("NodaTime.Testing", "2.4.4");
                             packageBuilder.AddPackageReference("Newtonsoft.Json");
                             packageBuilder.EnableBlazor(registry);
                         });
                         
            registry.Add("blazor-ms.logging",
                         packageBuilder =>
                         {
                             packageBuilder.CreateUsingDotnet("classlib");
                             packageBuilder.DeleteFile("Class1.cs");
                             packageBuilder.AddPackageReference("Microsoft.Extensions.Logging", "2.2.0");
                             packageBuilder.EnableBlazor(registry);
                         });

            return registry;
        }

        public IEnumerator<Task<PackageBuilder>> GetEnumerator() =>
            _packageBuilders.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        private static IEnumerable<IPackageFinder> GetDefaultPackageFinders()
        {
            yield return new PackageNameIsFullyQualifiedPath();
            yield return new FindPackageInDefaultLocation(new FileSystemDirectoryAccessor(Package.DefaultPackagesDirectory));
        }

        Task<T> IPackageFinder.Find<T>(PackageDescriptor descriptor)
        {
            return Get<T>(descriptor.Name);
        }
    }
}
