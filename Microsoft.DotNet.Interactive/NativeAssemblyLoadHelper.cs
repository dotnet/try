// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Pocket;

namespace Microsoft.DotNet.Interactive
{
    public class NativeAssemblyLoadHelper : IDisposable
    {
        private static readonly HashSet<DirectoryInfo> globalProbingPaths = new HashSet<DirectoryInfo>();
        private readonly HashSet<DirectoryInfo> _probingPaths = new HashSet<DirectoryInfo>();

        private readonly Dictionary<string, ResolvedPackageReference> _resolvers =
            new Dictionary<string, ResolvedPackageReference>(StringComparer.OrdinalIgnoreCase);

        public NativeAssemblyLoadHelper()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void SetNativeLibraryProbingPaths(IReadOnlyList<DirectoryInfo> probingPaths)
        {
            _probingPaths.UnionWith(probingPaths);
            lock (globalProbingPaths)
            {
                globalProbingPaths.UnionWith(probingPaths);
            }   
        }

        public void Handle(ResolvedPackageReference reference)
        {
            var assemblyFile = reference.AssemblyPaths.First();

            foreach (var dir in _probingPaths)
            {
                Logger.Log.Info("Probing: {dir}", dir);

                if (assemblyFile.FullName.StartsWith(dir.FullName))
                {
                    _resolvers[assemblyFile.FullName] = reference;
                }
            }
        }

        private IEnumerable<string> ProbingFilenames(string name)
        {
            // Try the name supplied by the pinvoke
            yield return name;

            if (!Path.IsPathRooted(name))
            {
                var usePrefix = ProbingUsePrefix(name);

                // Name is not rooted so we can try with prefix and suffix
                foreach (var suffix in ProbingSuffixes())
                {
                    if (ProbingUseSuffix(name, suffix))
                    {
                        yield return $"{name}{suffix}";
                        if (usePrefix)
                        {
                            yield return $"lib{name}{suffix}";
                        }
                    }
                    else
                    {
                        yield return $"lib{name}";
                    }
                }
            }

            // Probe for necessary suffixes --- these suffix' are platform specific
            IEnumerable<string> ProbingSuffixes()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    yield return ".dll";
                    yield return ".exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    yield return ".dylib";
                }
                else
                {
                    yield return ".so";
                }
            }

            // linux developers often append a version number to libraries I.e mydll.so.5.3.2
            static bool ProbingUseSuffix(string name, string s)
            {
                return !(name.Contains(s + ".") || name.EndsWith(s));
            }

            // If the name looks like a path or a volume name then dont prefix 'lib'
            static bool ProbingUsePrefix(string name)
            {
                return name.IndexOf(Path.DirectorySeparatorChar) == -1
                       && name.IndexOf(Path.AltDirectorySeparatorChar) == -1
                       && name.IndexOf(Path.PathSeparator) == -1
                       && name.IndexOf(Path.VolumeSeparatorChar) == -1;
            }
        }

        private IEnumerable<string> ProbingPaths(string probingPath, string name)
        {
            // if name is rooted then it's an absolute path to the dll
            if (Path.IsPathRooted(name))
            {
                if (File.Exists(name))
                    yield return name;
            }
            else
            {
                // Check if the dll exists in the probe path root
                foreach(var pname in ProbingFilenames(name))
                {
                    var path = Path.Combine(probingPath, pname);
                    if (File.Exists(path))
                        yield return path;
                }
                // Grovel through the platform specific subdirectory
                foreach(var rid in ProbingRids())
                {
                    var path = Path.Combine(probingPath, "runtimes", rid, "native");

                    // Check if the dll exists in the rid specific native directory
                    foreach (var pname in ProbingFilenames(name))
                    {
                        var p = Path.Combine(path, pname);
                        if (File.Exists(p))
                            yield return p;
                    }
                }
            }

            // Computer valid dotnet-rids for this environment: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
            // Where rid is: win, win-x64, win-x86, osx-x64, linux-x64 etc ...
            IEnumerable<string> ProbingRids()
            {
                var processArchitecture = RuntimeInformation.ProcessArchitecture;
                var baseRid =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
                    "linux";

                var platformRid =
                    processArchitecture == Architecture.X64 ? "-x64" :
                    processArchitecture == Architecture.X86 ? "-x86" :
                    processArchitecture == Architecture.Arm64 ? "-arm64" :
                    "arm";

                yield return baseRid + platformRid;
                yield return baseRid;
                yield return "any";
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
                    var ptr = IntPtr.Zero;
                    if (_resolvers.TryGetValue(
                        args.LoadedAssembly.Location,
                        out var reference))
                    {
                        ptr = _probingPaths.SelectMany(dir => ProbingPaths(dir.FullName, libraryName).Select(nativeLoader)).FirstOrDefault();
                    }
                    if (ptr == IntPtr.Zero)
                    {
                        lock (globalProbingPaths)
                        {
                            ptr = globalProbingPaths.SelectMany(dir => ProbingPaths(dir.FullName, libraryName).Select(nativeLoader)).FirstOrDefault();
                        }
                    }
                    return ptr;
                });

            IntPtr nativeLoader(string dll)
            {
                var ptr = IntPtr.Zero;
                try
                {
                    ptr = NativeLibrary.Load(dll);
                    Logger.Log.Info("NativeLibrary.Load({dll})", args.LoadedAssembly.Location);
                }
                catch (Exception)
                {
                }
                return ptr;
            }
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        }
    }
}