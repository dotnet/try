using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using MLS.Agent.Tools;

namespace WorkspaceServer.Packaging
{
    public class PackageRestoreContext
    {
        private readonly AsyncLazy<Package> _lazyPackage;

        public PackageRestoreContext()
        {
            _lazyPackage = new AsyncLazy<Package>(CreatePackage);
        }

        public async Task<string> OutputPath()
            => (await _lazyPackage.ValueAsync()).EntryPointAssemblyPath.FullName;

        private async Task<Package> CreatePackage()
        {
            var packageBuilder = new PackageBuilder(Guid.NewGuid().ToString("N"));
            packageBuilder.CreateRebuildablePackage = true;
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        }

        public async Task<IEnumerable<MetadataReference>> AddPackage(string packageName, string packageVersion)
        {
            var package = await _lazyPackage.ValueAsync();
            var currentWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            var currentRefs = new HashSet<string>(
                currentWorkspace.CurrentSolution.Projects.First().MetadataReferences
                .Select(m => m.Display));

            var dotnet = new Dotnet(package.Directory);
            var result = await dotnet.AddPackage(packageName, packageVersion);

            if (result.ExitCode != 0)
            {
                return Array.Empty<MetadataReference>();
            }

            var newWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            var newRefs = new HashSet<MetadataReference>(newWorkspace.CurrentSolution.Projects.First().MetadataReferences);

            var resultRefs = newRefs.Where(n => !currentRefs.Contains(n.Display));
            return resultRefs;
        }

        public async Task<IEnumerable<MetadataReference>> GetAllReferences()
        {
            var package = await _lazyPackage.ValueAsync();
            var currentWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return currentWorkspace.CurrentSolution.Projects.First().MetadataReferences;
        }
    }
}
