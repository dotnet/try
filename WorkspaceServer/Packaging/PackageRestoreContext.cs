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
using WorkspaceServer.Kernel;
using WorkspaceServer.Servers.Roslyn;

namespace WorkspaceServer.Packaging
{
    public class PackageRestoreContext  : IHaveADirectory
    {
        private readonly CSharpKernel _kernel;
        private readonly ScriptState _scriptState;
        private readonly Dictionary<NugetPackageReference, ResolvedNugetPackageReference> _nugetPackageReferences = new Dictionary<NugetPackageReference, ResolvedNugetPackageReference>();

        public PackageRestoreContext(CSharpKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

            _scriptState = kernel.ScriptState;

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

            if (_nugetPackageReferences.TryGetValue(requestedPackage, out var _))
            {
                return new AddNugetPackageResult(false, requestedPackage);
            }

            _nugetPackageReferences.Add(requestedPackage, null);

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

            FileInfo[] currentScriptStateReferences = GetScriptStateReferences();

            Dictionary<string, ResolvedNugetPackageReference> calculatedNugetReferences = GetResolvedNugetReferences();

            ResolvedNugetPackageReference[] addedReferences =
                calculatedNugetReferences
                    .Values
                    // .Where(calc =>
                    // {
                    //     if (!calc.AssemblyPaths.Select(a => a.Name)
                    //              .Any(n => currentScriptStateReferences.Any(
                    //                       pre => pre.Name.Equals(
                    //                           n,
                    //                           StringComparison.InvariantCultureIgnoreCase))))
                    //     {
                    //         return true;
                    //     }
                    //
                    //     return false;
                    // })
                    .ToArray();

            foreach (var addedReference in addedReferences)
            {
              //  _nugetPackageReferences.Add[requestedPackage] = addedReference;
            }

            return new AddNugetPackageResult(
                succeeded: true,
                requestedPackage: requestedPackage,
                addedReferences: addedReferences);
        }

        private FileInfo[] GetScriptStateReferences()
        {
            var compilation = _scriptState?.Script.GetCompilation();

            if (compilation == null)
            {
                return Array.Empty<FileInfo>();
            }

            return compilation.References
                              .Concat(_kernel.ScriptOptions.MetadataReferences)
                              .Select(r => new FileInfo(r.Display))
                              .ToArray();
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

                sb.Append("<ItemGroup>");

                foreach (var reference in _nugetPackageReferences.Keys)
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
          DependsOnTargets='ResolvePackageAssets; ResolveReferences' 
          AfterTargets='PrepareForBuild'>
    <ItemGroup>
      <ReferenceLines Remove='@(ReferenceLines)' />
      <ReferenceLines Include='%(ResolvedFile.NugetPackageId),%(ResolvedFile.NugetPackageVersion),%(ResolvedFile.HintPath),%(NativeIncludeRoots.Path)'
                      Condition = ""%(ResolvedFile.NugetPackageId) != 'Microsoft.NETCore.App' and Exists('%(ResolvedFile.HintPath)')"" />
    </ItemGroup>
    <WriteLinesToFile Lines='@(ReferenceLines)' 
                      File='$(MSBuildProjectFullPath).nuget.paths' 
                      Overwrite='True' WriteOnlyWhenDifferent='True' />

    <ItemGroup>
      <ResolvedReferenceLines Remove='*' />
      <ResolvedReferenceLines Include='%(ReferencePath.NugetPackageId),%(ReferencePath.NugetPackageVersion),%(ReferencePath.OriginalItemSpec),%(NativeIncludeRoots.Path)' />
    </ItemGroup>

    <WriteLinesToFile Lines='@(ResolvedReferenceLines)' 
                      File='$(MSBuildProjectFullPath).resolvedReferences.paths' 
                      Overwrite='True' WriteOnlyWhenDifferent='True' />
  </Target>
";
        }
    }
}