using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using MLS.Agent;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.PackageRestore
{
    public class PackageRestoreContext : IDisposable
    {
        private readonly DirectoryInfo _workingDirectory;
        private readonly CompositeDisposable disposable = new CompositeDisposable();
        private readonly AsyncLazy<Package> _lazyPackage;

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

            _lazyPackage = new AsyncLazy<Package>(CreatePackage);
        }

        private async Task<Package> CreatePackage()
        {
            var packageBuilder = new PackageBuilder(Guid.NewGuid().ToString("N8"));
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        }

        public async Task<IEnumerable<MetadataReference>> AddPackage(string package, string version)
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
