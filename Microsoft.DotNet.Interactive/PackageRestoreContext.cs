// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{

    //#r nuget --- events

    //#r "nuget:one"
    //#r "nuget:two"
    //#r "nuget:three"

    // Events
    //  SubmitCode("")
    //  PackageAdded("one")
    //  PackageAdded("two")
    //  PackageAdded("three")
    //  PackageRestoreCompleted("One", Two", "Three", "TransitiveDependency_one", "TransitiveDependency_two")

    public class PackageRestoreContext 
    {
        private readonly Dictionary<string, (PackageReference, ResolvedPackageReference)> _packageReferences = new Dictionary<string, (PackageReference, ResolvedPackageReference)>();

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

        public bool AddPackagReference(
            string packageName,
            string packageVersion = null,
            string restoreSources = null,
            string sourceCode = null)
        {
            var requestedPackage = new PackageReference(packageName, packageVersion, restoreSources, sourceCode);
            var packageKey = requestedPackage.PackageKey;

            lock (_packageReferences)
            {
                if (!String.IsNullOrEmpty(packageName) && _packageReferences.TryGetValue(packageKey, out var _))
                {
                    return false;
                }

                if (!_packageReferences.ContainsKey(packageKey))
                {
                    _packageReferences.Add(packageKey, (requestedPackage, null));
                }
            }

            return true;
        }

        public IEnumerable<PackageReference> PackageReferences
        {
            get
            {
                lock (_packageReferences)
                {
                    return _packageReferences.Values.Select(t => t.Item1);
                }
            }
        }

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences
        {
            get
            {
                lock (_packageReferences)
                {
                    return _packageReferences.Values.Select(t => t.Item2);
                }
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
                lock (_packageReferences)
                {
                    return new PackageRestoreResult(
                        succeeded: false,
                        requestedPackages: _packageReferences.Values.Select(t => t.Item1),
                        errors: result.Output.Concat(result.Error).ToArray());
                }
            }
            else
            {
                return new PackageRestoreResult(
                    succeeded: true,
                    requestedPackages: _packageReferences.Values.Select(t => t.Item1),
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
                    .Select(v => v.Item1)
                    .Where(reference => !string.IsNullOrEmpty(reference.PackageName))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <PackageReference Include=\"{reference.PackageName}\" Version=\"{reference.PackageVersion}\"/>\n"));

                sb.Append("  </ItemGroup>\n");

                sb.Append("  <PropertyGroup>\n");

                _packageReferences
                    .Values
                    .Select(v => v.Item1)
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