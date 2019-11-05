// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Kernel;

namespace MLS.Agent
{
    public class NativeAssemblyLoadHelper : INativeAssemblyLoadHelper
    {
        private readonly HashSet<DirectoryInfo> _probingPaths = new HashSet<DirectoryInfo>();

        private readonly Dictionary<string, AssemblyDependencyResolver> _resolvers =
            new Dictionary<string, AssemblyDependencyResolver>(StringComparer.OrdinalIgnoreCase);

        public NativeAssemblyLoadHelper()
        {
            // var currentDomain = AppDomain.CurrentDomain;
            // currentDomain.AssemblyLoad += DoTheThing;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void SetNativeDllProbingPaths(
            FileInfo assemblyPath,
            IReadOnlyList<DirectoryInfo> probingPaths)
        {
            _probingPaths.UnionWith(probingPaths);
        }

        public void Handle(FileInfo assemblyFile)
        {
            foreach (var dir in _probingPaths)
            {
                Logger.Log.Info($"Probing: {dir}");

                if (assemblyFile.FullName.Contains(dir.FullName))
                {
                    var resolver = new AssemblyDependencyResolver(assemblyFile.FullName);

                    _resolvers.Add(assemblyFile.FullName, resolver);
                }
            }
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.IsDynamic ||
                string.IsNullOrWhiteSpace(args.LoadedAssembly.Location))
            {
                return;
            }

            Logger.Log.Info("OnAssemblyLoad: {location}", args.LoadedAssembly.Location);
            Console.WriteLine($"OnAssemblyLoad: {args.LoadedAssembly.Location}");

            NativeLibrary.SetDllImportResolver(
                args.LoadedAssembly,
                (libraryName, assembly, searchPath) =>
                {
                    if (_resolvers.TryGetValue(
                        args.LoadedAssembly.Location,
                        out var resolver))
                    {
                        foreach (var path in _probingPaths)
                        {
                            var dll =
                                path.Subdirectory("runtimes")
                                    .Subdirectory("win-x64")
                                    .GetFiles($"{libraryName}.dll", SearchOption.AllDirectories);

                            if (dll.Length == 1)
                            {
                                var ptr = NativeLibrary.Load(dll[0].FullName);

                                return ptr;
                            }
                        }
                    }

                    return IntPtr.Zero;
                });
        }

        //
        // private AssemblyLoadEventHandler AssemblyLoaded(FileInfo assembly)
        // {
        //     return (_, args) =>
        //     {
        //         if (args.LoadedAssembly.Location == assembly.FullName)
        //         {
        //             NativeLibrary.SetDllImportResolver(args.LoadedAssembly, Resolve);
        //         }
        //     };
        // }
        //
        // private IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        // {
        //     var path = _resolver.ResolveUnmanagedDllToPath(libraryName);
        //
        //     return NativeLibrary.Load(path);
        // }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            //
            // _resolvers.Clear();
        }
    }
}