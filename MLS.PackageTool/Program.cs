// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLS.PackageTool
{
    public class PackageToolConstants
    {
        public const string LocateProjectAsset = "locate-project-asset";
        public const string LocateWasmAsset = "locate-wasm-asset";
        public const string PreparePackage = "prepare-package";
    }

    public class Program
    {
        static async Task Main(string[] args)
        {
            var parser = CommandLineParser.Create(
                LocateBuildAssetHandler,
                LocateWasmAssetHandler,
                Prepare);

            await parser.InvokeAsync(args);
        }

        private static async Task Prepare(IConsole console)
        {
            await UnzipProjectAsset(console);
        }

        private static async Task UnzipProjectAsset(IConsole console)
        { 
            var directory = AssemblyDirectory();
            var zipFilePath = Path.Combine(directory, "project.zip");

            File.Delete(zipFilePath);

            string targetDirectory = Path.Combine(directory, "project");
            try
            {
                Directory.Delete(targetDirectory, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }

            var resource = typeof(Program).Assembly.GetManifestResourceNames()[0];

            using (var stream = typeof(Program).Assembly.GetManifestResourceStream(resource))
            {
                using (var zipFileStream = File.OpenWrite(zipFilePath))
                {
                    await stream.CopyToAsync(zipFileStream);
                    await zipFileStream.FlushAsync();
                }
            }

            ZipFile.ExtractToDirectory(zipFilePath, targetDirectory);
            File.Delete(zipFilePath);
        }

        public static void LocateBuildAssetHandler(IConsole console)
        {
            console.Out.WriteLine(BuildDirectoryLocation());
        }

        public static void LocateWasmAssetHandler(IConsole console)
        {
            console.Out.WriteLine(WasmDirectoryLocation());
        }

        public static string BuildDirectoryLocation() =>
            Path.Combine(Path.GetDirectoryName(AssemblyLocation()), "project", "build");

        public static string WasmDirectoryLocation() =>
            Path.Combine(Path.GetDirectoryName(AssemblyLocation()), "project", "wasm", "MLS.Blazor", "dist");

        public static string AssemblyLocation()
        {
            return typeof(Program).Assembly.Location;
        }

        public static string AssemblyDirectory()
        {
            return Path.GetDirectoryName(AssemblyLocation());
        }
    }


    public class CommandLineParser
    {
        public static Parser Create(
            Action<IConsole> getBuildAsset,
            Action<IConsole> getWasmAsset,
            Func<IConsole, Task> prepare)
        {
            var rootCommand = new RootCommand
                              {
                                  LocateBuildAsset(),
                                  LocateWasmAsset(),
                                  PreparePackage()
                              };

            var parser = new CommandLineBuilder(rootCommand)
                         .UseDefaults()
                         .Build();

            Command LocateBuildAsset()
            {
                return new Command(PackageToolConstants.LocateProjectAsset)
                {
                    Handler = CommandHandler.Create(getBuildAsset)
                };
            }

            Command LocateWasmAsset()
            {
                return new Command(PackageToolConstants.LocateWasmAsset)
                {
                    Handler = CommandHandler.Create(getWasmAsset)
                };
            }

            Command PreparePackage()
            {
                return new Command(PackageToolConstants.PreparePackage, "Prepares the packages")
                {
                    Handler = CommandHandler.Create(prepare)
                };
            }

            return parser;
        }
    }
}