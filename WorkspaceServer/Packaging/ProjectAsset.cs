// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Buildalyzer;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Servers.Roslyn;
using static Pocket.Logger<WorkspaceServer.Packaging.ProjectAsset>;
using Disposable = System.Reactive.Disposables.Disposable;

namespace WorkspaceServer.Packaging
{
    public class ProjectAsset : PackageAsset,
        ICreateWorkspaceForLanguageServices,
        ICreateWorkspaceForRun,
        IHaveADirectory
    {
        private readonly IDirectoryAccessor _directoryAccessor;
        private const string FullBuildBinlogFileName = "package_fullBuild.binlog";

        private Workspace _workspace;
        private readonly FileInfo _projectFile;
        private readonly SemaphoreSlim _buildSemaphore;
        private readonly object _fullBuildCompletionSourceLock = new object();
        private TaskCompletionSource<Workspace> _fullBuildCompletionSource = new TaskCompletionSource<Workspace>();
        private readonly Subject<Budget> _fullBuildRequestChannel;

        private readonly IScheduler _buildThrottleScheduler;
        private readonly SerialDisposable _fullBuildThrottlerSubscription;
       
        private readonly FileInfo _lastBuildErrorLogFile;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> PackageBuildSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

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

            Name = _projectFile.Name;
            Directory = DirectoryAccessor.GetFullyQualifiedPath(new RelativeDirectoryPath(".")) as DirectoryInfo;

            _buildThrottleScheduler =  TaskPoolScheduler.Default;
            _fullBuildRequestChannel = new Subject<Budget>();
            _fullBuildThrottlerSubscription = new SerialDisposable();
            _buildSemaphore = PackageBuildSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));
            _lastBuildErrorLogFile = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./.trydotnet-builderror")) as FileInfo;
            SetupWorkspaceCreationFromBuildChannel();
        }
        public Task<Workspace> CreateRoslynWorkspaceAsync(Budget budget)
        {
            if (_workspace != null)
            {
                return Task.FromResult(_workspace);
            }

            CreateCompletionSourceIfNeeded(ref _fullBuildCompletionSource, _fullBuildCompletionSourceLock);

            _fullBuildRequestChannel.OnNext(budget);

            return _fullBuildCompletionSource.Task;
        }

        public Task<Workspace> CreateRoslynWorkspaceForRunAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }

        public Task<Workspace> CreateRoslynWorkspaceForLanguageServicesAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }

        private void SetupWorkspaceCreationFromBuildChannel()
        {
            _fullBuildThrottlerSubscription.Disposable = _fullBuildRequestChannel
                .Throttle(TimeSpan.FromSeconds(0.5), _buildThrottleScheduler)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    async (budget) =>
                    {
                        try
                        {
                            await ProcessFullBuildRequest(budget);
                        }
                        catch (Exception e)
                        {
                            SetCompletionSourceException(_fullBuildCompletionSource, e, _fullBuildCompletionSourceLock);
                        }
                    },
                    error =>
                    {
                        SetCompletionSourceException(_fullBuildCompletionSource, error, _fullBuildCompletionSourceLock);
                        SetupWorkspaceCreationFromBuildChannel();
                    });
        }

        private void SetCompletionSourceResult(TaskCompletionSource<Workspace> completionSource, Workspace result, object lockObject)
        {
            lock (lockObject)
            {
                switch (completionSource.Task.Status)
                {
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                    case TaskStatus.RanToCompletion:
                        return;
                    default:
                        completionSource.SetResult(result);
                        break;
                }
            }
        }
        private void CreateCompletionSourceIfNeeded(ref TaskCompletionSource<Workspace> completionSource, object lockObject)
        {
            lock (lockObject)
            {
                switch (completionSource.Task.Status)
                {
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                    case TaskStatus.RanToCompletion:
                        completionSource = new TaskCompletionSource<Workspace>();
                        break;
                }
            }
        }

        private void SetCompletionSourceException(TaskCompletionSource<Workspace> completionSource, Exception exception, object lockObject)
        {
            lock (lockObject)
            {
                switch (completionSource.Task.Status)
                {
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                    case TaskStatus.RanToCompletion:
                        return;
                    default:
                        completionSource.SetException(exception);
                        break;
                }
            }
        }

        private async Task ProcessFullBuildRequest(Budget budget)
        {
            await FullBuild().CancelIfExceeds(budget);
            SetCompletionSourceResult(_fullBuildCompletionSource, _workspace, _fullBuildCompletionSourceLock);
        }

        public async Task FullBuild()
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                try
                {
                    operation.Info("Attempting building package {name}", Name);

                    var buildInProgress = _buildSemaphore.CurrentCount == 0;
                    await _buildSemaphore.WaitAsync();

                    using (Disposable.Create(() => _buildSemaphore.Release()))
                    {
                        if (buildInProgress)
                        {
                            operation.Info("Skipping build for package {name}", Name);
                            return;
                        }

                        await DotnetBuild();
                    }

                    operation.Info("Workspace built");

                    operation.Succeed();
                }
                catch (Exception exception)
                {
                    operation.Error("Exception building workspace", exception);
                }

                var binLog = this.FindLatestBinLog();
                await binLog.WaitForFileAvailable();
                var manager = new AnalyzerManager();
                var results = manager.Analyze(binLog.FullName);

                if (results.Count == 0)
                {
                    throw new InvalidOperationException("The build log seems to contain no solutions or projects");
                }

                var result = results.FirstOrDefault(p => p.ProjectFilePath == _projectFile.FullName);
                if (result != null)
                {
                    if (result.Succeeded)
                    {
                        if (result.TryGetWorkspace(out var ws))
                        {
                            var projectId = ws.CurrentSolution.ProjectIds.FirstOrDefault();
                            var references = result.References;
                            var metadataReferences = references.GetMetadataReferences();
                            var solution = ws.CurrentSolution;
                            solution = solution.WithProjectMetadataReferences(projectId, metadataReferences);
                            ws.TryApplyChanges(solution);
                            _workspace = ws;
                        }
                    }
                }
            }
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