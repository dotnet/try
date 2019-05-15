// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;
using WorkspaceServer.WorkspaceFeatures;

namespace WorkspaceServer
{
    public class Dotnet
    {
        protected readonly DirectoryInfo _workingDirectory;

        public Dotnet(DirectoryInfo workingDirectory = null)
        {
            _workingDirectory = workingDirectory ??
                                new DirectoryInfo(Directory.GetCurrentDirectory());
        }

        public Task<CommandLineResult> New(string templateName, string args = null, Budget budget = null)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(templateName));
            }

            return Execute($@"new ""{templateName}"" {args}", budget);
        }

        public Task<CommandLineResult> AddPackage(string packageId, string version = null, Budget budget = null)
        {
            var versionArg = string.IsNullOrWhiteSpace(version)
                ? ""
                : $"--version {version}";

            return Execute($"add package {versionArg} {packageId}", budget);
        }

        public Task<CommandLineResult> AddReference(FileInfo projectToReference, Budget budget = null)
        {
            return Execute($@"add reference ""{projectToReference.FullName}""", budget);
        }

        public Task<CommandLineResult> Build(string args = null, Budget budget = null) =>
            Execute("build".AppendArgs(args), budget);

        public Task<CommandLineResult> Execute(string args, Budget budget = null) =>
            CommandLine.Execute(
                Path,
                args,
                _workingDirectory,
                budget);

        public Task<CommandLineResult> Publish(string args, Budget budget = null) =>
            Execute("publish".AppendArgs(args), budget);

        public Task<CommandLineResult> VSTest(string args, Budget budget = null) =>
            Execute("vstest".AppendArgs(args), budget);

        public Task<CommandLineResult> ToolInstall(string args = null, Budget budget = null) =>
            Execute("tool install".AppendArgs(args), budget);

        public async Task<IEnumerable<string>> ToolList(DirectoryInfo directory, Budget budget = null)
        {
            var result = await Execute("tool list".AppendArgs($@"--tool-path ""{directory.FullName}"""), budget);
            if (result.ExitCode != 0)
            {
                return Enumerable.Empty<string>();
            }

            // Output of dotnet tool list is:
            // Package Id        Version      Commands
            // -------------------------------------------
            // dotnettry.p1      1.0.0        dotnettry.p1

            string[] separator = new[] { " " };
            return result.Output
                .Skip(2)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Split(separator, StringSplitOptions.RemoveEmptyEntries)[2]);
        }

        public Task<CommandLineResult> ToolInstall(
            string packageName, 
            DirectoryInfo toolPath,
            DirectoryInfo addSource = null, 
            Budget budget = null)
        {
            var args = $@"{packageName} --tool-path ""{toolPath.FullName}"" --version 1.0.0";
            if (addSource != null)
            {
                args += $@" --add-source ""{addSource}""";
            }

            return Execute("tool install".AppendArgs(args), budget);
        }

        public Task<CommandLineResult> Pack(string args = null, Budget budget = null) =>
            Execute("pack".AppendArgs(args), budget);

        private static readonly Lazy<FileInfo> _getPath = new Lazy<FileInfo>(() =>
                                                                                 FindDotnetFromAppContext() ??
                                                                                 FindDotnetFromPath());

        public static FileInfo Path => _getPath.Value;

        private static FileInfo FindDotnetFromPath()
        {
            FileInfo fileInfo = null;

            using (var process = Process.Start("dotnet"))
            {
                if (process != null)
                {
                    fileInfo = new FileInfo(process.MainModule.FileName);
                }
            }

            return fileInfo;
        }

        private static FileInfo FindDotnetFromAppContext()
        {
            var muxerFileName = "dotnet".ExecutableName();

            var fxDepsFile = GetDataFromAppDomain("FX_DEPS_FILE");

            if (!string.IsNullOrEmpty(fxDepsFile))
            {
                var muxerDir = new FileInfo(fxDepsFile).Directory?.Parent?.Parent?.Parent;

                if (muxerDir != null)
                {
                    var muxerCandidate = new FileInfo(System.IO.Path.Combine(muxerDir.FullName, muxerFileName));

                    if (muxerCandidate.Exists)
                    {
                        return muxerCandidate;
                    }
                }
            }

            return null;
        }

        public static string GetDataFromAppDomain(string propertyName)
        {
            var appDomainType = typeof(object).GetTypeInfo().Assembly?.GetType("System.AppDomain");
            var currentDomain = appDomainType?.GetProperty("CurrentDomain")?.GetValue(null);
            var deps = appDomainType?.GetMethod("GetData")?.Invoke(currentDomain, new[] { propertyName });
            return deps as string;
        }
    }
}
