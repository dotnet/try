using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using static WorkspaceServer.Kernel.CSharpKernelExtensions;

namespace MLS.Agent
{
    public class NativeAssemblyLoadHelper : INativeAssemblyLoadHelper
    {
        private AssemblyDependencyResolver _resolver;

        public NativeAssemblyLoadHelper()
        {
        }

        public void Configure(string path)
        {
            if (_resolver != null)
            {
                return;
            }

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
            return NativeLibrary.Load(path);
        }
    }
}
