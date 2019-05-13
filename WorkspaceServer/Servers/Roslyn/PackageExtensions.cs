// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Servers.Roslyn.Instrumentation;
using static System.Environment;
using Package = WorkspaceServer.Packaging.Package;
using Workspace = Microsoft.DotNet.Try.Protocol.Workspace;

namespace WorkspaceServer.Servers.Roslyn
{
    public static class PackageExtensions
    {
        public static async Task<Compilation> Compile(
            this Package package, 
            Workspace workspace, 
            Budget budget, 
            BufferId activeBufferId)
        {
            var sourceFiles = workspace.GetSourceFiles().ToArray();

            var (compilation, documents) = await package.GetCompilationForRun(sourceFiles, SourceCodeKind.Regular, workspace.Usings, budget);

            var viewports = workspace.ExtractViewPorts();

            var diagnostics = compilation.GetDiagnostics();

            if (workspace.IncludeInstrumentation && !diagnostics.ContainsError())
            {
                var activeDocument = GetActiveDocument(documents, activeBufferId);
                compilation = await AugmentCompilationAsync(viewports, compilation, activeDocument, activeBufferId, package);
            }

            return compilation;
        }

        private static async Task<Compilation> AugmentCompilationAsync(
            IEnumerable<Viewport> viewports,
            Compilation compilation,
            Document document,
            BufferId activeBufferId,
            Package build)
        {
            var regions = InstrumentationLineMapper.FilterActiveViewport(viewports, activeBufferId)
                .Where(v => v.Destination?.Name != null)
                .GroupBy(v => v.Destination.Name,
                         v => v.Region,
                        (name, region) => new InstrumentationMap(name, region))
                .ToArray();

            var solution = document.Project.Solution;
            var newCompilation = compilation;
            foreach (var tree in newCompilation.SyntaxTrees)
            {
                var replacementRegions = regions.FirstOrDefault(r => tree.FilePath.EndsWith(r.FileToInstrument))?.InstrumentationRegions;

                var subdocument = solution.GetDocument(tree);
                var visitor = new InstrumentationSyntaxVisitor(subdocument, await subdocument.GetSemanticModelAsync(), replacementRegions);
                var linesWithInstrumentation = visitor.Augmentations.Data.Keys;

                var activeViewport = viewports.DefaultIfEmpty(null).First();

                var (augmentationMap, variableLocationMap) =
                    await InstrumentationLineMapper.MapLineLocationsRelativeToViewportAsync(
                        visitor.Augmentations,
                        visitor.VariableLocations,
                        document,
                        activeViewport);

                var rewrite = new InstrumentationSyntaxRewriter(
                    linesWithInstrumentation,
                    variableLocationMap,
                    augmentationMap);
                var newRoot = rewrite.Visit(tree.GetRoot());
                var newTree = tree.WithRootAndOptions(newRoot, tree.Options);

                newCompilation = newCompilation.ReplaceSyntaxTree(tree, newTree);
            }

            var instrumentationSyntaxTree = build.GetInstrumentationEmitterSyntaxTree();
            newCompilation = newCompilation.AddSyntaxTrees(instrumentationSyntaxTree);

            var augmentedDiagnostics = newCompilation.GetDiagnostics();
            if (augmentedDiagnostics.ContainsError())
            {
                throw new InvalidOperationException(
                    $@"Augmented source failed to compile

Diagnostics
-----------
{string.Join(NewLine, augmentedDiagnostics)}

Source
------
{newCompilation.SyntaxTrees.Select(s => $"// {s.FilePath ?? "(anonymous)"}{NewLine}//---------------------------------{NewLine}{NewLine}{s}").Join(NewLine + NewLine)}");
            }

            return newCompilation;
        }

        public static async Task<(Compilation compilation, IReadOnlyCollection<Document> documents)> GetCompilation(
            this Package package,
            IReadOnlyCollection<SourceFile> sources,
            SourceCodeKind sourceCodeKind,
            IEnumerable<string> defaultUsings,
            Func<Task<Microsoft.CodeAnalysis.Workspace>> workspaceFactory,
            Budget budget)
        {
            var workspace = await workspaceFactory();

            var currentSolution = workspace.CurrentSolution;
            var project = currentSolution.Projects.First();
            var projectId = project.Id;
            foreach (var source in sources)
            {
                if (currentSolution.Projects
                    .SelectMany(p => p.Documents)
                    .FirstOrDefault(d => d.IsMatch(source)) is Document document)
                {
                    // there's a pre-existing document, so overwrite its contents
                    document = document.WithText(source.Text);
                    document = document.WithSourceCodeKind(sourceCodeKind);
                    currentSolution = document.Project.Solution;
                }
                else
                {
                    var docId = DocumentId.CreateNewId(projectId, $"{package.Name}.Document");

                    currentSolution = currentSolution.AddDocument(docId, source.Name, source.Text);
                    currentSolution = currentSolution.WithDocumentSourceCodeKind(docId, sourceCodeKind);
                }
            }


            project = currentSolution.GetProject(projectId);
            var usings = defaultUsings?.ToArray() ?? Array.Empty<string>();
            if (usings.Length > 0)
            {
                var options = (CSharpCompilationOptions) project.CompilationOptions;
                project = project.WithCompilationOptions(options.WithUsings(usings));
            }

            var compilation = await project.GetCompilationAsync().CancelIfExceeds(budget);

            return (compilation, project.Documents.ToArray());
        }

        public static  Task<(Compilation compilation, IReadOnlyCollection<Document> documents)> GetCompilationForRun(
            this Package package,
            IReadOnlyCollection<SourceFile> sources,
            SourceCodeKind sourceCodeKind,
            IEnumerable<string> defaultUsings,
            Budget budget) =>
            package.GetCompilation(sources, sourceCodeKind, defaultUsings, () => package.CreateRoslynWorkspaceForRunAsync(budget), budget);

        public static Task<(Compilation compilation, IReadOnlyCollection<Document> documents)> GetCompilationForLanguageServices(
          this Package package,
          IReadOnlyCollection<SourceFile> sources,
          SourceCodeKind sourceCodeKind,
          IEnumerable<string> defaultUsings,
          Budget budget) =>
            package.GetCompilation(sources, sourceCodeKind, defaultUsings, () => package.CreateRoslynWorkspaceForLanguageServicesAsync(budget), budget);

        private static Document GetActiveDocument(IEnumerable<Document> documents, BufferId activeBufferId)
        {
            return documents.First(d => d.Name.Equals(activeBufferId.FileName));
        }
    }
}

