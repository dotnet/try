using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using MLS.Agent;
using Pocket;

namespace WorkspaceServer.PackageRestore
{
    public class PackageRestoreContext : IDisposable
    {
        private readonly DirectoryInfo _workingDirectory;
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public PackageRestoreContext(DirectoryInfo workingDirectory = null)
        {
            if (workingDirectory != null)
            {
                _workingDirectory = workingDirectory;
            }
            else
            {
                var disposableDirectory = DisposableDirectory.Create();
                workingDirectory = disposableDirectory.Directory;
                disposable.Add(disposableDirectory);
            }
        }

        public IEnumerable<MetadataReference> AddPackage(string package, string version)
        {

        }

        public IEnumerable<MetadataReference> AccumulatedReferences()
        {

        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}
