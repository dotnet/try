﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using static WorkspaceServer.Kernel.CSharpKernelExtensions;

namespace MLS.Agent
{
    public class NativeAssemblyLoadHelper : INativeAssemblyLoadHelper
    {
        private readonly string _tfm;
        private readonly string _suffix;
        private AssemblyDependencyResolver _resolver;

        public NativeAssemblyLoadHelper()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _tfm = "osx-x64";
                _suffix = ".dylib";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _tfm = "linux-x64";
                _suffix = ".so";
            }
            else if (RuntimeInformation.OSArchitecture == Architecture.X86)
            {
                _tfm = "win-x86";
                _suffix = ".dll";
            }
            else
            {
                _tfm = "win-x64";
                _suffix = ".dll";
            }
        }

        public void Configure(string path)
        {
            if (_resolver != null)
                return;

            _resolver = new AssemblyDependencyResolver(path);
        }

        public void Handle(string assembly)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyLoad += AssemblyLoaded(assembly);
        }

        private AssemblyLoadEventHandler AssemblyLoaded(string assembly)
        {
            return (object sender, AssemblyLoadEventArgs args) =>
            {
                if (args.LoadedAssembly.Location == assembly)
                {
                    NativeLibrary.SetDllImportResolver(args.LoadedAssembly, Resolve);
                }
            };
        }

        private IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(libraryName);

            //var basePath = Path.GetDirectoryName(assembly.Location);
            //var nativeAssembly = Path.Combine(basePath, "..", "..", "runtimes", _tfm, "native", libraryName) + _suffix;
            return NativeLibrary.Load(path);
        }
    }
}
