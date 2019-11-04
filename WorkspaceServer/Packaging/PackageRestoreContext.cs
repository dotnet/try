// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive;
using WorkspaceServer.Servers.Roslyn;

namespace WorkspaceServer.Packaging
{
    public class PackageRestoreContext  : IHaveADirectory
    {
        private readonly ScriptState _scriptState;
        private readonly List<NugetPackageReference> _requestedNugetPackageReferences = new List<NugetPackageReference>();
        private readonly List<ResolvedNugetPackageReference> _resolvedNugetPackageReferences = new List<ResolvedNugetPackageReference>();

        public PackageRestoreContext(ScriptState scriptState = null)
        {
            _scriptState = scriptState;

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

        public FileInfo EntryPointAssemblyPath() => this.GetEntryPointAssemblyPath(false);

        public async Task<ResolvedNugetPackageReference> GetResolvedNugetPackageReference(string packageName)
        {
            var references = GetResolvedNugetReferences();

            return references[packageName];
        }

        public async Task<AddNugetPackageResult> AddPackage(
            string packageName,
            string packageVersion = null)
        {
            var requestedPackage = new NugetPackageReference(packageName, packageVersion);

            _requestedNugetPackageReferences.Add(requestedPackage);

            WriteProjectFile();

            var dotnet = new Dotnet(Directory);

            var result = await dotnet.Build();

            if (result.ExitCode != 0)
            {
                return new AddNugetPackageResult(
                    succeeded: false,
                    requestedPackage,
                    errors: result.Output.Concat(result.Error).ToArray());
            }

            var preexistingReferences =
                _scriptState?.Script
                            .GetCompilation()
                            .DirectiveReferences
                            .Select(r => new FileInfo(r.Display))
                            .ToArray()
                ??
                Array.Empty<FileInfo>();

            var allReferences = GetResolvedNugetReferences();

            var addedReferences =
                allReferences
                    .Values
                    .Where(r =>
                               preexistingReferences.All(pre => !r.PackageRoot.FullName.Contains(pre.FullName)))
                    .ToArray();

            _resolvedNugetPackageReferences.AddRange(addedReferences);

            return new AddNugetPackageResult(
                succeeded: true,
                requestedPackage: requestedPackage,
                addedReferences: addedReferences,
                references: addedReferences);
        }

        private Dictionary<string, ResolvedNugetPackageReference> GetResolvedNugetReferences()
        {
            var nugetPathsFile = Directory.GetFiles("*.nuget.paths").SingleOrDefault();

            if (nugetPathsFile == null)
            {
                return new Dictionary<string, ResolvedNugetPackageReference>();
            }

            var nugetPackageLines = File.ReadAllText(Path.Combine(Directory.FullName, nugetPathsFile.FullName))
                                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return nugetPackageLines
                   .Select(line => line.Split(','))
                   .Select(s =>
                               (
                                   packageName: s[0].Trim(),
                                   packageVersion: s[1].Trim(),
                                   assemblyPath: new FileInfo(s[2].Trim()),
                                   packageRoot: !string.IsNullOrWhiteSpace(s[3])
                                                    ? new DirectoryInfo(s[3].Trim())
                                                    : null))
                   .GroupBy(x =>
                                (
                                    x.packageName,
                                    x.packageVersion,
                                    x.packageRoot))
                   .Select(xs => new ResolvedNugetPackageReference(
                               xs.Key.packageName,
                               xs.Key.packageVersion,
                               xs.Select(x => x.assemblyPath).ToArray(),
                               xs.Key.packageRoot))
                   .ToDictionary(r => r.PackageName, StringComparer.OrdinalIgnoreCase);
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

                sb.Append("<ItemGroup>");

                foreach (var reference in _requestedNugetPackageReferences)
                {
                    sb.Append($"<PackageReference Include=\"{reference.PackageName}\" Version=\"{reference.PackageVersion}\"/>");
                }

                sb.Append("</ItemGroup>");

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
          DependsOnTargets='ResolvePackageAssets' 
          AfterTargets='PrepareForBuild'>
    <ItemGroup>
      <ReferenceLines Remove='@(ReferenceLines)' />
      <ReferenceLines Include='%(ResolvedFile.NugetPackageId),%(ResolvedFile.NugetPackageVersion),%(ResolvedFile.HintPath),%(NativeIncludeRoots.Path)'
                      Condition = ""%(ResolvedFile.NugetPackageId) != 'Microsoft.NETCore.App' and Exists('%(ResolvedFile.HintPath)')"" />
    </ItemGroup>
    <WriteLinesToFile Lines='@(ReferenceLines)' 
                      File='$(MSBuildProjectFullPath).nuget.paths' 
                      Overwrite='True' WriteOnlyWhenDifferent='True' />
  </Target>
";
        }
    }
}