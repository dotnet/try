// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Microsoft.DotNet.Try.Markdown;
using WorkspaceServer;

namespace MLS.Agent.CommandLine
{
    public class StartupOptions : IDefaultCodeBlockAnnotations
    {
        private readonly ParseResult _parseResult;

        public static StartupOptions FromCommandLine(string commandLine)
        {
            StartupOptions startupOptions = null;

            CommandLineParser.Create(startServer: (options, context) =>
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
            DirectoryInfo dir = null,
            PackageSource addPackageSource = null,
            Uri uri = null,
            DirectoryInfo logPath = null,
            bool verbose = false,
            bool enablePreviewFeatures = false,
            string package = null,
            string packageVersion = null,
            ParseResult parseResult = null,
            int port = 5000)
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
            Dir = dir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
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
        public DirectoryInfo Dir { get; }
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
                switch (_parseResult?.CommandResult?.Name)
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
                ? Microsoft.AspNetCore.Hosting.EnvironmentName.Production
                : Microsoft.AspNetCore.Hosting.EnvironmentName.Development;

        public DirectoryInfo LogPath { get;  }

        public bool Verbose { get; }

        public string Package { get; }

        public string PackageVersion { get; }
        public int Port { get; }
    }
}