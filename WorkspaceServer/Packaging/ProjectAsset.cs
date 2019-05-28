// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Buildalyzer;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Servers.Roslyn;
using static Pocket.Logger<WorkspaceServer.Packaging.ProjectAsset>;


namespace WorkspaceServer.Packaging
{
    public class ProjectAsset : PackageAsset,
        ICreateWorkspaceForLanguageServices,
        ICreateWorkspaceForRun,
        IHaveADirectory
    {
        private const string FullBuildBinlogFileName = "package_fullBuild.binlog";
        private readonly FileInfo _projectFile;
        private readonly FileInfo _lastBuildErrorLogFile;
        private readonly PipelineStep<AnalyzerResult> _projectBuildStep;
        private readonly PipelineStep<Workspace> _workspaceStep;
        private readonly PipelineStep<AnalyzerResult> _cleanupStep;
        private readonly FileSystemInfo _lockFile;

        public string Name { get; }


        public DirectoryInfo Directory { get; }

        public ProjectAsset(IDirectoryAccessor directoryAccessor, string csprojFileName = null) : base(directoryAccessor)
        {
            if (directoryAccessor == null)
            {
                throw new ArgumentNullException(nameof(directoryAccessor));
            }

            if (string.IsNullOrWhiteSpace(csprojFileName))
            {
                var firstProject = directoryAccessor.GetAllFiles().FirstOrDefault(f => f.Extension == ".csproj");
                if (firstProject != null)
                {
                    _projectFile = directoryAccessor.GetFullyQualifiedFilePath(firstProject.FileName);
                }
            }
            else
            {
                _projectFile = directoryAccessor.GetFullyQualifiedFilePath(csprojFileName);
            }

            
            Directory = DirectoryAccessor.GetFullyQualifiedPath(new RelativeDirectoryPath(".")) as DirectoryInfo;

            Name = _projectFile?.Name ?? Directory?.Name;

            _lastBuildErrorLogFile = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./.trydotnet-builderror")) as FileInfo;

            _cleanupStep = new PipelineStep<AnalyzerResult>(LoadResultOrCleanAsync);
            _projectBuildStep = _cleanupStep.Then(BuildProjectAsync);
            _workspaceStep = _projectBuildStep.Then(BuildWorkspaceAsync);
            _lockFile = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath(".trydotnet-lock"));
        }

        private async Task<AnalyzerResult> LoadResultOrCleanAsync()
        {
            FileStream fileStream = null;
            try
            {
                fileStream = File.Create(_lockFile.FullName, 1, FileOptions.DeleteOnClose);
                var binLog = this.FindLatestBinLog();
                if (binLog != null)
                {
                    var results = await TryLoadAnalyzerResultsAsync(binLog);
                    var result = results?.FirstOrDefault(p => p.ProjectFilePath == _projectFile.FullName);

                    var didCompile = DidPerformCoreCompile(result);
                    if (result != null)
                    {
                        if (result.Succeeded && didCompile)
                        {
                            return result;
                        }
                    }
                }

                binLog?.DoWhenFileAvailable(() => binLog.Delete());
                var toClean = Directory.GetDirectories("obj");
                foreach (var directoryInfo in toClean)
                {
                    directoryInfo.Delete(true);
                }

                return null;
            }
            finally
            {
                fileStream.Dispose();
            }
        }

        private bool DidPerformCoreCompile(AnalyzerResult result)
        {
            if (result == null)
            {
                return false;
            }

            var sourceCount = result.SourceFiles?.Length ?? 0;
            var compilerInputs = result.GetCompileInputs()?.Length ?? 0;

            return compilerInputs > 0 && sourceCount > 0;
        }

        private Task<Workspace> BuildWorkspaceAsync(AnalyzerResult result)
        {
            if (result.TryGetWorkspace(out var ws))
            {
                var projectId = ws.CurrentSolution.ProjectIds.FirstOrDefault();
                var references = result.References;
                var metadataReferences = references.GetMetadataReferences();
                var solution = ws.CurrentSolution;
                solution = solution.WithProjectMetadataReferences(projectId, metadataReferences);
                ws.TryApplyChanges(solution);
                return Task.FromResult(ws);
            }
            throw new InvalidOperationException("Failed creating workspace");
        }

        private async Task<AnalyzerResult> BuildProjectAsync(AnalyzerResult result)
        {
            if (result != null)
            {
                return result;
            }

            FileStream fileStream = null;
            try
            {
                fileStream = File.Create(_lockFile.FullName, 1, FileOptions.DeleteOnClose);
                using (var operation = Log.OnEnterAndConfirmOnExit())
                {
                    try
                    {
                        operation.Info("Attempting building package {name}", Name);
                        await DotnetBuild();
                        operation.Info("Workspace built");
                        operation.Succeed();
                    }
                    catch (Exception exception)
                    {
                        operation.Error("Exception building workspace", exception);
                        throw;
                    }

                    var binLog = this.FindLatestBinLog();

                    if (binLog == null)
                    {
                        throw new InvalidOperationException("Failed to build");
                    }

                    var results = await TryLoadAnalyzerResultsAsync(binLog);

                    if (results?.Count == 0)
                    {
                        throw new InvalidOperationException("The build log seems to contain no solutions or projects");
                    }

                    result = results?.FirstOrDefault(p => p.ProjectFilePath == _projectFile.FullName);
                    if (result != null)
                    {
                        if (result.Succeeded)
                        {
                            return result;
                        }

                        throw new InvalidOperationException("Failed to build");
                    }

                    throw new InvalidOperationException("Failed to build");
                }
            }
            finally
            {
                fileStream?.Dispose();
            }
        }

        private async Task<AnalyzerResults> TryLoadAnalyzerResultsAsync(FileInfo binLog)
        {
            AnalyzerResults results = null;
            await binLog.DoWhenFileAvailable(() =>
            {
                var manager = new AnalyzerManager();
                results = manager.Analyze(binLog.FullName);
            });
            return results;
        }

        public Task<Workspace> CreateRoslynWorkspaceAsync(Budget budget)
        {
            return _workspaceStep.GetLatestAsync();
        }

        public Task<Workspace> CreateRoslynWorkspaceForRunAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }

        public Task<Workspace> CreateRoslynWorkspaceForLanguageServicesAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }


        protected async Task DotnetBuild()
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                var args = $"/bl:{FullBuildBinlogFileName}";
                if (_projectFile?.Exists == true)
                {
                    args = $@"""{_projectFile.FullName}"" {args}";
                }

                operation.Info("Building package {name} in {directory}", Name, Directory);
                
                var result = await new Dotnet(Directory).Build(args: args);

                if (result.ExitCode != 0)
                {
                    File.WriteAllText(
                        _lastBuildErrorLogFile.FullName,
                        string.Join(Environment.NewLine, result.Error));
                }
                else if (_lastBuildErrorLogFile.Exists)
                {
                    _lastBuildErrorLogFile.Delete();
                }

                result.ThrowOnFailure();
                operation.Succeed();
            }
        }
    }
}