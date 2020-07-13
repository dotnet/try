// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.IO;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MLS.Agent.Tools;
using WorkspaceServer.Packaging;

namespace MLS.Agent.CommandLine
{
    public class StartupOptions : IDefaultCodeBlockAnnotations
    {
        private readonly ParseResult _parseResult;

        public static StartupOptions FromCommandLine(string commandLine)
        {
            StartupOptions startupOptions = null;

            CommandLineParser.Create(new ServiceCollection(), startServer: (options, context) =>
                {
                    startupOptions = options;
                })
                .InvokeAsync(commandLine);

            return startupOptions;
        }

        public StartupOptions(
            bool production = false,
            bool languageService = false,
            string key = null,
            string applicationInsightsKey = null,
            string id = null,
            string regionId = null,
            PackageSource addPackageSource = null,
            Uri uri = null,
            DirectoryInfo logPath = null,
            bool verbose = false,
            bool enablePreviewFeatures = false,
            string package = null,
            string packageVersion = null,
            ParseResult parseResult = null,
            ushort? port = null,
            IDirectoryAccessor rootDirectory = null)
        {
            _parseResult = parseResult;
            LogPath = logPath;
            Verbose = verbose;
            Id = id;
            Production = production;
            IsLanguageService = languageService;
            Key = key;
            ApplicationInsightsKey = applicationInsightsKey;
            RegionId = regionId;
            RootDirectory = rootDirectory?? new FileSystemDirectoryAccessor(new DirectoryInfo(Directory.GetCurrentDirectory()));
            AddPackageSource = addPackageSource;
            Uri = uri;
            EnablePreviewFeatures = enablePreviewFeatures;
            Package = package;
            PackageVersion = packageVersion;
            Port = port;
        }


        public bool EnablePreviewFeatures { get; }
        public string Id { get; }
        public string RegionId { get; }
        public IDirectoryAccessor RootDirectory { get; }
        public PackageSource AddPackageSource { get; }
        public Uri Uri { get; set; }
        public bool Production { get; }
        public bool IsLanguageService { get; set; }
        public string Key { get; }
        public string ApplicationInsightsKey { get; }

        public StartupMode Mode
        {
            get
            {
                switch (_parseResult?.CommandResult?.Command?.Name)
                {
                    case "hosted":
                        return StartupMode.Hosted;
                    default:
                        return StartupMode.Try;
                }
            }
        }

        public string EnvironmentName =>
            Production || Mode != StartupMode.Hosted
                ? Environments.Production
                : Environments.Development;

        public DirectoryInfo LogPath { get; }

        public bool Verbose { get; }

        public string Package { get; }

        public string PackageVersion { get; }
        public ushort? Port { get; }
    }
}