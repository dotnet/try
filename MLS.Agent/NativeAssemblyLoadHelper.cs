// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Kernel;
using WorkspaceServer.Packaging;

namespace MLS.Agent
{
    public class NativeAssemblyLoadHelper : INativeAssemblyLoadHelper
    {
        private readonly HashSet<DirectoryInfo> _probingPaths = new HashSet<DirectoryInfo>();

        private readonly Dictionary<string, ResolvedNugetPackageReference> _resolvers =
            new Dictionary<string, ResolvedNugetPackageReference>(StringComparer.OrdinalIgnoreCase);

        public NativeAssemblyLoadHelper()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void SetNativeDllProbingPaths(IReadOnlyList<DirectoryInfo> probingPaths)
        {
            _probingPaths.UnionWith(probingPaths);
        }

        public void Handle(ResolvedNugetPackageReference reference)
        {
            var assemblyFile = reference.AssemblyPaths.First();

            foreach (var dir in _probingPaths)
            {
                Logger.Log.Info("Probing: {dir}", dir);

                if (assemblyFile.FullName.Contains(dir.FullName))
                {
                    _resolvers.Add(assemblyFile.FullName, reference);
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

            NativeLibrary.SetDllImportResolver(
                args.LoadedAssembly,
                (libraryName, assembly, searchPath) =>
                {
                    if (_resolvers.TryGetValue(
                        args.LoadedAssembly.Location,
                        out var reference))
                    {
                        foreach (var path in _probingPaths)
                        {
                            var dll =
                                path.Subdirectory("runtimes")
                                    .Subdirectory(reference.RuntimeIdentifier)
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

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        }
    }
}