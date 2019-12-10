// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public class PackageRestoreContext 
    {
        private object _lockObject = new object();
        private readonly ConcurrentDictionary<string, PackageReference> _packageReferences = new ConcurrentDictionary<string, PackageReference>();
        private Dictionary<string, ResolvedPackageReference> _resolvedReferences = new Dictionary<string, ResolvedPackageReference>();

        public PackageRestoreContext()
        {
            Name = Guid.NewGuid().ToString("N");

            Directory = new DirectoryInfo(
                Path.Combine(
                    Paths.UserProfile,
                    ".net-interactive-csharp",
                    Name));

            if (!Directory.Exists)
            {
                Directory.Create();
            }
        }

        public DirectoryInfo Directory { get; }

        public string Name { get; }

        public async Task<ResolvedPackageReference> GetResolvedPackageReference(string packageName)
        {
            await Restore();

            var references = GetResolvedReferences();
            return references[packageName];
        }

        // Add a package reference return false if unable to add.
        public bool AddPackagReference(
            string packageName,
            string packageVersion = null,
            string restoreSources = null)
        {
            var key = string.IsNullOrWhiteSpace(packageName)
                          ? $"RestoreSources={restoreSources}"
                          : $"PackageName=: {packageName} RestoreSources={restoreSources}";

            if (string.IsNullOrEmpty(key)) return false;

            // we use a lock because we are going to be looking up and inserting
            lock (_lockObject)
            {
                if (!_packageReferences.TryGetValue(key, out PackageReference existingPackage))
                {
                    if (!_resolvedReferences.TryGetValue(packageName, out ResolvedPackageReference resolvedPackage))
                    {
                        return _packageReferences.TryAdd(key, new PackageReference(packageName, packageVersion, restoreSources));
                    }
                    return resolvedPackage.PackageVersion.Trim() == packageVersion.Trim();
                }

                // Verify version numbers match note: wildcards/previews are considered distinct
                return existingPackage.PackageVersion.Trim() == packageVersion.Trim();
            }
        }

        public IEnumerable<PackageReference> PackageReferences
        {
            get
            {
                return _packageReferences.Values;
            }
        }

        public async Task<PackageRestoreResult> Restore()
        {
            WriteProjectFile();

            var dotnet = new Dotnet(Directory);
            var result = await dotnet.Execute("msbuild -restore /t:WriteNugetAssemblyPaths");
            var resolvedReferences =
                GetResolvedReferences()
                    .Values
                    .ToArray();

            if (result.ExitCode != 0)
            {
                return new PackageRestoreResult(
                    succeeded: false,
                    requestedPackages: _packageReferences.Values,
                    errors: result.Output.Concat(result.Error).ToArray());
            }
            else
            {
                return new PackageRestoreResult(
                    succeeded: true,
                    requestedPackages: _packageReferences.Values,
                    resolvedReferences: resolvedReferences);
            }
        }

        private Dictionary<string, ResolvedPackageReference> GetResolvedReferences()
        {
            var nugetPathsFile = Directory.GetFiles("*.resolvedReferences.paths").SingleOrDefault();

            if (nugetPathsFile == null)
            {
                return new Dictionary<string, ResolvedPackageReference>();
            }

            var nugetPackageLines = File.ReadAllText(Path.Combine(Directory.FullName, nugetPathsFile.FullName))
                                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var probingPaths = new List<DirectoryInfo>();

            var dict = nugetPackageLines
                       .Select(line => line.Split(','))
                       .Where(line =>
                       {
                           if (string.IsNullOrWhiteSpace(line[0]))
                           {
                               probingPaths.Add(new DirectoryInfo(line[3]));

                               return false;
                           }

                           return true;
                       })
                       .Select(line =>
                                   (
                                       packageName: line[0].Trim(),
                                       packageVersion: line[1].Trim(),
                                       assemblyPath: new FileInfo(line[2].Trim()),
                                       packageRoot: !string.IsNullOrWhiteSpace(line[3])
                                                        ? new DirectoryInfo(line[3].Trim())
                                                        : null,
                                       runtimeIdentifier: line[4].Trim()))
                       .GroupBy(x =>
                                    (
                                        x.packageName,
                                        x.packageVersion,
                                        x.packageRoot))
                       .Select(xs => new ResolvedPackageReference(
                                   xs.Key.packageName,
                                   xs.Key.packageVersion,
                                   xs.Select(x => x.assemblyPath).ToArray(),
                                   xs.Key.packageRoot,
                                   probingPaths))
                       .ToDictionary(r => r.PackageName, StringComparer.OrdinalIgnoreCase);

            lock (_lockObject)
            {
                _resolvedReferences = dict;
            }
            return dict;
        }

        private void WriteProjectFile()
        {
            var directoryPropsContent =
                $@"
<Project Sdk='Microsoft.NET.Sdk'>
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>netcoreapp3.0</TargetFramework>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    {PackageReferences()}
    {Targets()}
    
</Project>";

            File.WriteAllText(
                Path.Combine(
                    Directory.FullName,
                    "r.csproj"),
                directoryPropsContent);

            File.WriteAllText(
                Path.Combine(
                    Directory.FullName,
                    "Program.cs"),
                @"
using System;

namespace s
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
");

            string PackageReferences()
            {
                var sb = new StringBuilder();

                sb.Append("  <ItemGroup>\n");

                _packageReferences
                    .Values
                    .Where(reference => !string.IsNullOrEmpty(reference.PackageName))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <PackageReference Include=\"{reference.PackageName}\" Version=\"{reference.PackageVersion}\"/>\n"));

                sb.Append("  </ItemGroup>\n");

                sb.Append("  <PropertyGroup>\n");

                _packageReferences
                    .Values
                    .Where(reference => !string.IsNullOrEmpty(reference.RestoreSources))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources){reference.RestoreSources}</RestoreAdditionalProjectSources>\n"));
                sb.Append("  </PropertyGroup>\n");

                return sb.ToString();
            }

            string Targets() => @"
  <Target Name='ComputePackageRoots'
          BeforeTargets='CoreCompile;WriteNugetAssemblyPaths'
          DependsOnTargets='CollectPackageReferences'>
      <ItemGroup>
        <ResolvedFile Include='@(ResolvedCompileFileDefinitions)'>
           <PackageRootProperty>Pkg$([System.String]::Copy('%(ResolvedCompileFileDefinitions.NugetPackageId)').Replace('.','_'))</PackageRootProperty>
           <PackageRoot>$(%(ResolvedFile.PackageRootProperty))</PackageRoot>
           <InitializeSourcePath>$(%(ResolvedFile.PackageRootProperty))\content\%(ResolvedCompileFileDefinitions.FileName)%(ResolvedCompileFileDefinitions.Extension).fsx</InitializeSourcePath>
        </ResolvedFile>
        <NativeIncludeRoots
            Include='@(RuntimeTargetsCopyLocalItems)'
            Condition=""'%(RuntimeTargetsCopyLocalItems.AssetType)' == 'native'"">
           <Path>$([System.String]::Copy('%(FullPath)').Substring(0, $([System.String]::Copy('%(FullPath)').LastIndexOf('runtimes'))))</Path>
        </NativeIncludeRoots>
      </ItemGroup>
  </Target>

  <Target Name='WriteNugetAssemblyPaths' 
          DependsOnTargets='ResolvePackageAssets; ResolveReferences; ProcessFrameworkReferences' 
          AfterTargets='PrepareForBuild'>

    <ItemGroup>
      <ResolvedReferenceLines Remove='*' />
      <ResolvedReferenceLines Include='%(ReferencePath.NugetPackageId),%(ReferencePath.NugetPackageVersion),%(ReferencePath.OriginalItemSpec),%(NativeIncludeRoots.Path),$(AppHostRuntimeIdentifier)' />
    </ItemGroup>

    <WriteLinesToFile Lines='@(ResolvedReferenceLines)' 
                      File='$(MSBuildProjectFullPath).resolvedReferences.paths' 
                      Overwrite='True' WriteOnlyWhenDifferent='True' />
  </Target>
";
        }
    }
}