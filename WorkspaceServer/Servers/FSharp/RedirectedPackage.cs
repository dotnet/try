using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Servers.Roslyn;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Servers.FSharp
{
    internal class RedirectedPackage : Package, IDisposable
    {
        private Package _parentPackage;
        private DirectoryInfo _redirectedDirectory;
        private Workspace _workspace;

        public RedirectedPackage(Workspace workspace, Package parentPackage, DirectoryInfo directory)
            : base(parentPackage.Name, parentPackage.Initializer, directory)
        {
            _parentPackage = parentPackage;
            _redirectedDirectory = directory;
            _workspace = workspace;
        }

        public string[] GetFiles()
        {
            var sourcePath = _parentPackage.Directory.FullName.EnsureTrailingSeparator();
            var destPath = _redirectedDirectory.FullName.EnsureTrailingSeparator();
            return _workspace.Files.Select(f => f.Name.Replace(sourcePath, destPath)).ToArray();
        }

        public void Clean()
        {
            try
            {
                Directory.Delete(true);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Clean();
        }
    }
}
