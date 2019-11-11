// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;

namespace WorkspaceServer.Packaging
{
    public class PackageRestoreContext  : IHaveADirectory
    {
        private readonly Dictionary<NugetPackageReference, ResolvedNugetPackageReference> _nugetPackageReferences = new Dictionary<NugetPackageReference, ResolvedNugetPackageReference>();

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

        public async Task<ResolvedNugetPackageReference> GetResolvedNugetPackageReference(string packageName)
        {
            var references = GetResolvedNugetReferences();

            return references[packageName];
        }

        public async Task<AddNugetResult> AddPackage(
            string packageName,
            string packageVersion = null,
            string restoreSources = null)
        {
            var requestedPackage = new NugetPackageReference(packageName, packageVersion, restoreSources);

            if (!String.IsNullOrEmpty(packageName) && _nugetPackageReferences.TryGetValue(requestedPackage, out var _))
            {
                return new AddNugetPackageResult(false, requestedPackage);
            }

            _nugetPackageReferences.Add(requestedPackage, null);

            WriteProjectFile();

            var dotnet = new Dotnet(Directory);

            var result = await dotnet.Execute("msbuild -restore /t:WriteNugetAssemblyPaths");

            if (result.ExitCode != 0)
            {
                if (string.IsNullOrEmpty(packageName) && string.IsNullOrEmpty(restoreSources))
                {
                    return new AddNugetRestoreSourcesResult(
                        succeeded: false,
                        requestedPackage,
                        errors: result.Output.Concat(result.Error).ToArray());
                }
                else
                {
                    return new AddNugetPackageResult(
                        succeeded: false,
                        requestedPackage,
                        errors: result.Output.Concat(result.Error).ToArray());
                }
            }

            var addedReferences =
                GetResolvedNugetReferences()
                    .Values
                    .ToArray();

            if (string.IsNullOrEmpty(packageName) && !string.IsNullOrEmpty(restoreSources))
            {
                return new AddNugetRestoreSourcesResult(
                    succeeded: true,
                    requestedPackage: requestedPackage,
                    addedReferences: addedReferences);
            }
            else
            {
                return new AddNugetPackageResult(
                    succeeded: true,
                    requestedPackage: requestedPackage,
                    addedReferences: addedReferences);
            }
        }

        private Dictionary<string, ResolvedNugetPackageReference> GetResolvedNugetReferences()
        {
            var nugetPathsFile = Directory.GetFiles("*.resolvedReferences.paths").SingleOrDefault();

            if (nugetPathsFile == null)
            {
                return new Dictionary<string, ResolvedNugetPackageReference>();
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
                       .Select(xs => new ResolvedNugetPackageReference(
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

                _nugetPackageReferences
                    .Keys
                    .Where(reference => !string.IsNullOrEmpty(reference.PackageName))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <PackageReference Include=\"{reference.PackageName}\" Version=\"{reference.PackageVersion}\"/>\n"));

                sb.Append("  </ItemGroup>\n");

                sb.Append("  <PropertyGroup>\n");

                _nugetPackageReferences
                    .Keys
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