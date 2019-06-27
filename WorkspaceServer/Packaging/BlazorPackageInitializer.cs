// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;

namespace WorkspaceServer.Packaging
{
    public class BlazorPackageInitializer : PackageInitializer
    {
        private readonly string _name;
        private readonly List<string> _addPackages;

        public BlazorPackageInitializer(string name, List<string> addPackages) :
            base("blazor", "MLS.Blazor")
        {
            _name = name;
            _addPackages = addPackages ?? throw new ArgumentNullException(nameof(addPackages));

            var requiredPackages = new List<string>
            {
                "Newtonsoft.Json"
            };

            _addPackages = addPackages.Concat(requiredPackages).Distinct().ToList();
        }

        public override async Task Initialize(DirectoryInfo directory, Budget budget = null)
        {
            if (directory.Name != "MLS.Blazor")
            {
                throw new ArgumentException(@"Directory must be called `MLS.Blazor` but is actually called {nameof(directory)}");
            }

            await base.Initialize(directory, budget);
            await MakeBlazorProject(directory, budget);
        }

        private async Task MakeBlazorProject(DirectoryInfo directory, Budget budget)
        {
            var dotnet = new Dotnet(directory);
            var root = directory.FullName;

            AddRootNamespaceAndBlazorLinkerDirective();
            DeleteUnusedFilesFromTemplate(root);
            AddEmbeddedResourceContentToProject(root);
            UpdateFileText(root, Path.Combine("wwwroot","index.html"), "/LocalCodeRunner/blazor-console", $"/LocalCodeRunner/{_name}");
            
            foreach (var packageId in _addPackages)
            {
                await dotnet.AddPackage(packageId);
            }

            await dotnet.AddPackage("System.CommandLine.Experimental", "0.3.0-alpha.19317.1");

            var result = await dotnet.Build("-o runtime /bl", budget: budget);
            var stuff = string.Concat(string.Join("\n", result.Output), (string.Join("\n", result.Error)));
            result.ThrowOnFailure(stuff);

            void AddRootNamespaceAndBlazorLinkerDirective()
            {
                UpdateFileText(root, "MLS.Blazor.csproj", "</PropertyGroup>",
                    @"</PropertyGroup>
<PropertyGroup>
    <RootNamespace>MLS.Blazor</RootNamespace>
</PropertyGroup>
<ItemGroup>
  <BlazorLinkerDescriptor Include=""Linker.xml"" />
</ItemGroup>");
            }
        }

        private void AddEmbeddedResourceContentToProject(string root)
        {
            var wwwRootFiles = new[] { "index.html", "interop.js" };
            var pagesFiles = new[] { "Index.razor" };
            var rootFiles = new[] { "Program.cs", "Startup.cs", "CodeRunner.cs", "InteropMessage.cs", "SerializableDiagnostic.cs", "WasmCodeRunnerRequest.cs", "WasmCodeRunnerResponse.cs", "CommandLineBuilderExtensions.cs", "EntryPointDiscoverer.cs", "PreserveConsoleState.cs", "_Imports.razor", "Linker.xml" };

            WriteResourcesToLocation(wwwRootFiles, Path.Combine(root, "wwwroot"));
            WriteResourcesToLocation(pagesFiles, Path.Combine(root, "Pages"));
            WriteResourcesToLocation(rootFiles, root);
        }

        private static void DeleteUnusedFilesFromTemplate(string root)
        {
            var filesAndDirectoriestoDelete = new[] { "Pages", "Shared", "wwwroot", "_Imports.razor" };
            foreach (var fOrD in filesAndDirectoriestoDelete)
            {
                Path.Combine(root, fOrD).DeleteFileSystemObject();
            }
        }

        private void UpdateFileText(string root, string file, string toReplace, string replacement)
        {
            file = Path.Combine(root, file);
            var text = File.ReadAllText(file);
            var updated = text.Replace(toReplace, replacement);
            File.WriteAllText(file, updated);
        }

        private void WriteResourcesToLocation(string[] resources, string targetDirectory)
        {
            foreach (var resource in resources)
            {
                WriteResource(resource, targetDirectory);
            }
        }

        private void WriteResource(string resourceName, string targetDirectory)
        {
            var text = this.GetType().ReadManifestResource($"WorkspaceServer.{resourceName}");
            System.IO.Directory.CreateDirectory(targetDirectory);
            var path = Path.Combine(targetDirectory, resourceName);
            File.WriteAllText(path, text);
        }
    }
}
