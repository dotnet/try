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
using Pocket;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive
{
    public class PackageRestoreContext : IDisposable
    {
        private readonly ConcurrentDictionary<string, PackageReference> _requestedPackageReferences = new ConcurrentDictionary<string, PackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ResolvedPackageReference> _resolvedPackageReferences = new Dictionary<string, ResolvedPackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly Lazy<DirectoryInfo> _lazyDirectory;

        public PackageRestoreContext()
        {
            _lazyDirectory = new Lazy<DirectoryInfo>(() =>
            {
                var dir = new DirectoryInfo(
                    Path.Combine(
                        Paths.UserProfile,
                        ".net-interactive-csharp",
                        Guid.NewGuid().ToString("N")));

                if (!dir.Exists)
                {
                    dir.Create();
                }

                return dir;
            });
        }

        public DirectoryInfo Directory => _lazyDirectory.Value;

        public bool AddPackagReference(
            string packageName,
            string packageVersion = null,
            string restoreSources = null)
        {
            var key = $"{packageName}:{restoreSources}";
       
            // we use a lock because we are going to be looking up and inserting
            if (!_requestedPackageReferences.TryGetValue(key, out PackageReference existingPackage))
            {
                if (!_resolvedPackageReferences.TryGetValue(key, out ResolvedPackageReference resolvedPackage))
                {
                    return _requestedPackageReferences.TryAdd(key, new PackageReference(packageName, packageVersion, restoreSources));
                }

                return resolvedPackage.PackageVersion.Trim() == packageVersion.Trim();
            }

            // Verify version numbers match note: wildcards/previews are considered distinct
            return existingPackage.PackageVersion.Trim() == packageVersion.Trim();
        }

        public IEnumerable<PackageReference> RequestedPackageReferences => _requestedPackageReferences.Values;

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences => _resolvedPackageReferences.Values;

        public ResolvedPackageReference GetResolvedPackageReference(string packageName) => _resolvedPackageReferences[packageName];

        public async Task<PackageRestoreResult> Restore()
        {
            WriteProjectFile();

            var dotnet = new Dotnet(Directory);
            var result = await dotnet.Execute("msbuild -restore /t:WriteNugetAssemblyPaths");

            if (result.ExitCode != 0)
            {
                return new PackageRestoreResult(
                    succeeded: false,
                    requestedPackages: _requestedPackageReferences.Values,
                    errors: result.Output.Concat(result.Error).ToArray());
            }
            else
            {
                ReadResolvedReferencesFromBuildOutput();

                return new PackageRestoreResult(
                    succeeded: true,
                    requestedPackages: _requestedPackageReferences.Values,
                    resolvedReferences: _resolvedPackageReferences.Values.ToList());
            }
        }

        private void ReadResolvedReferencesFromBuildOutput()
        {
            var resolvedreferenceFilename = "*.resolvedReferences.paths";
            var nugetPathsFile = Directory.GetFiles(resolvedreferenceFilename).SingleOrDefault();

            if (nugetPathsFile == null)
            {
                Log.Error($"File not found: {Directory.FullName}{Path.DirectorySeparatorChar}{resolvedreferenceFilename}");
                return;
            }

            var nugetPackageLines = File.ReadAllText(Path.Combine(Directory.FullName, nugetPathsFile.FullName))
                                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var probingPaths = new List<DirectoryInfo>();

            var resolved = nugetPackageLines
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
                       .ToArray();

            foreach (var reference in resolved)
            {
                _resolvedPackageReferences.TryAdd(reference.PackageName, reference);
            }
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
                string GetReferenceVersion(PackageReference reference)
                {
                    return string.IsNullOrEmpty(reference.PackageVersion) ? "*" : reference.PackageVersion;
                }

                var sb = new StringBuilder();

                sb.Append("  <ItemGroup>\n");

                _requestedPackageReferences
                    .Values
                    .Where(reference => !string.IsNullOrEmpty(reference.PackageName))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <PackageReference Include=\"{reference.PackageName}\" Version=\"{GetReferenceVersion(reference)}\"/>\n"));

                sb.Append("  </ItemGroup>\n");

                sb.Append("  <PropertyGroup>\n");

                _requestedPackageReferences
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

        public void Dispose()
        {
            try
            {
                if (_lazyDirectory.IsValueCreated)
                {
                    Directory.Delete(true);
                }
            }
            catch
            {
            }
        }
    }
}