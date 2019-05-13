// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace MLS.Agent.CommandLine
{
    public static class DemoCommand
    {
        public static Task<int> Do(
            DemoOptions options,
            IConsole console,
            CommandLineParser.StartServer startServer = null,
            InvocationContext context = null)
        {
            var extractDemoFiles = true;

            if (!options.Output.Exists)
            {
                options.Output.Create();
            }
            else
            {
                if (options.Output.GetFiles().Any())
                {
                    if (options.Output.GetFiles().All(f => f.Name != "QuickStart.md"))
                    {
                        console.Out.WriteLine($"Directory {options.Output} must be empty. To specify a directory to create the demo sample in, use: dotnet try demo --output <dir>");
                        return Task.FromResult(-1);
                    }

                    extractDemoFiles = false;
                }
            }
          
            if (extractDemoFiles)
            {
                using (var disposableDirectory = DisposableDirectory.Create())
                {
                    var assembly = typeof(Program).Assembly;

                    using (var resourceStream = assembly.GetManifestResourceStream("demo.zip"))
                    {
                        var zipPath = Path.Combine(disposableDirectory.Directory.FullName, "demo.zip");

                        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                        {
                            resourceStream.CopyTo(fileStream);
                        }

                        ZipFile.ExtractToDirectory(zipPath, options.Output.FullName);
                    }
                }
            }
          
            startServer?.Invoke(new StartupOptions(
                                    uri: new Uri("QuickStart.md", UriKind.Relative),
                                    dir: options.Output),
                                context);

            return Task.FromResult(0);
        }
    }
}