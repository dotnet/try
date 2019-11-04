using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using WorkspaceServer.Kernel;

namespace MLS.Agent
{
    public class NativeAssemblyLoadHelper : INativeAssemblyLoadHelper
    {
        private AssemblyDependencyResolver _resolver;

        public void Configure(FileInfo componentAssemblyPath)
        {
            if (_resolver != null)
            {
                return;
            }

            _resolver = new AssemblyDependencyResolver(componentAssemblyPath.FullName);
        }

        public void Handle(FileInfo assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyLoad += AssemblyLoaded(assembly);
        }

        private AssemblyLoadEventHandler AssemblyLoaded(FileInfo assembly)
        {
            return (sender, args) =>
            {
                if (args.LoadedAssembly.Location == assembly.FullName)
                {
                    NativeLibrary.SetDllImportResolver(args.LoadedAssembly, Resolve);
                }
            };
        }

        private IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(libraryName);

            return NativeLibrary.Load(path);
        }
    }
}
