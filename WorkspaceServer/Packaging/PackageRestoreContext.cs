// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using MLS.Agent.Tools;

namespace WorkspaceServer.Packaging
{
    public partial class PackageRestoreContext
    {
        private readonly AsyncLazy<Package> _lazyPackage;

        public PackageRestoreContext(string name = null)
        {
            _lazyPackage = new AsyncLazy<Package>(
                () =>
                    CreatePackage(name
                                  ?? Guid.NewGuid().ToString("N")));
        }

        public async Task<string> OutputPath()
            => (await _lazyPackage.ValueAsync()).EntryPointAssemblyPath.FullName;

        private async Task<Package> CreatePackage(string name)
        {
            var packageBuilder = new PackageBuilder(name);
            packageBuilder.CreateRebuildablePackage = true;
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            var package = (Package)packageBuilder.GetPackage();
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            AddDirectoryProps(package);
            return package;
        }

        public async Task<AddReferenceResult> AddPackage(
            string packageName,
            string packageVersion = null)
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
                return new AddReferenceResult(succeeded: false, detailedErrors: result.DetailedErrors);
            }

            var newWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            var newRefs = new HashSet<MetadataReference>(newWorkspace.CurrentSolution.Projects.First().MetadataReferences);

            return new AddReferenceResult(succeeded: true, newRefs
                .Where(n => !currentRefs.Contains(n.Display))
                .ToArray(),
                references: newRefs.ToArray(),
                 installedVersion: result.InstalledVersion);
        }

        public async Task<IEnumerable<MetadataReference>> GetAllReferences()
        {
            var package = await _lazyPackage.ValueAsync();
            var currentWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return currentWorkspace.CurrentSolution.Projects.First().MetadataReferences;
        }

        public async Task<DirectoryInfo> GetDirectoryForPackage(string packageName)
        {
            var package = await _lazyPackage.ValueAsync();
            var nugetPathsFile = package.Directory.GetFiles("*.nuget.paths").Single();
            var nugetPackagePaths = File.ReadAllText(Path.Combine(package.Directory.FullName, nugetPathsFile.FullName)).Split(',', '\r', '\n')
                                        .Where(t => !string.IsNullOrWhiteSpace(t))
                                        .ToArray();
            var pathsDictionary = nugetPackagePaths
                                    .Select((v, i) => new { Index = i, Value = v })
                                    .GroupBy(p => p.Index / 2)
                                    .ToDictionary(g => g.First().Value, g => g.Last().Value, StringComparer.OrdinalIgnoreCase);

            return new DirectoryInfo(pathsDictionary[packageName.ToLower()]);
        }

        private void AddDirectoryProps(Package package)
        {
            const string generatePathsPropertyTarget =
@"  <Target Name=""AddGeneratePathsProperty"" BeforeTargets=""CollectPackageReferences"">
    <!--Show the properties-->
        <Message Text = ""Starting: Add GeneratePathProperty=true for package %(PackageReference.Identity)"" Importance = ""high"" />
       
       <ItemGroup>
         <PackageReference Condition = ""'%(PackageReference.GeneratePathProperty)' != 'true'"">
            <GeneratePathProperty>true </GeneratePathProperty>
          </PackageReference>
        </ItemGroup>

        <!--Show the changes -->
        <Message Text = ""Done:  GeneratePathProperty:%(PackageReference.GeneratePathProperty) for package %(PackageReference.Identity)"" Importance = ""high"" />
    </Target> ";

            const string computePackageRootsTarget =
@"  <Target Name='ComputePackageRoots' BeforeTargets='CoreCompile;PrintNuGetPackagesPaths' DependsOnTargets='CollectPackageReferences'>
        <ItemGroup>
        <!-- Read the package path from the Pkg{PackageName} properties that are present in the nuget.g.props file -->
            <AddedNuGetPackage Include='@(PackageReference)'>
                <PackageRootProperty>Pkg$([System.String]::Copy('%(PackageReference.Identity)').Replace('.','_'))</PackageRootProperty>
                <PackageRoot>$(%(AddedNuGetPackage.PackageRootProperty))</PackageRoot>
            </AddedNuGetPackage>
        </ItemGroup>

        <Message Text=""Done: Read package root : %(AddedNuGetPackage.PackageRoot) for %(AddedNuGetPackage.Identity)"" Condition=""%(AddedNuGetPackage.PackageRoot) != ''"" Importance=""high""/>
    </Target>";

            const string writePackageRootsToDiskTarget =
@"  <Target Name='PrintNuGetPackagesPaths' DependsOnTargets='ResolvePackageAssets;ComputePackageRoots' AfterTargets='PrepareForBuild'>
        <ItemGroup>
            <ReferenceLines Remove='@(ReferenceLines)' />
            <ReferenceLines Include='%(AddedNuGetPackage.Identity),%(AddedNuGetPackage.PackageRoot)' Condition=""%(AddedNuGetPackage.PackageRoot) != ''""/>
        </ItemGroup>

        <WriteLinesToFile Lines='@(ReferenceLines)' File='$(MSBuildProjectFullPath).nuget.paths' Overwrite='True' WriteOnlyWhenDifferent='True' />
    </Target>";

            string directoryPropsContent =
$@"<Project>
{generatePathsPropertyTarget}
{computePackageRootsTarget}
{writePackageRootsToDiskTarget}
</Project>";

            File.WriteAllText(Path.Combine(package.Directory.FullName, "Directory.Build.props"), directoryPropsContent);
        }
    }
}
