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
using Process = System.Diagnostics.Process;

namespace MLS.PackageTool
{
    public class PackageToolConstants
    {
        public const string LocateProjects = "locate-projects";
        public const string PreparePackage = "prepare-package";
    }

    public class Program
    {
        static async Task Main(string[] args)
        {
            var parser = CommandLineParser.Create(
                LocateAssemblyHandler,
                Prepare);

            await parser.InvokeAsync(args);
        }

        private static async Task Prepare(IConsole console)
        {
            await UnzipProjectAsset(console);
            var projectDirectory = new DirectoryInfo(ProjectDirectoryLocation());
            
            var runnerDir = projectDirectory.GetDirectories("runner-*").First().GetDirectories("MLS.Blazor").First();
            await CommandLine.Execute("dotnet", "build -o runtime /bl", runnerDir);
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

        public static void LocateAssemblyHandler(IConsole console)
        {
            console.Out.WriteLine(ProjectDirectoryLocation());
        }

        public static string ProjectDirectoryLocation() =>
            Path.Combine(Path.GetDirectoryName(AssemblyLocation()), "project");
        
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
        public static Parser Create(Action<IConsole> getAssembly, Func<IConsole, Task> prepare)
        {
            var rootCommand = new RootCommand
                              {
                                  LocateAssembly(),
                                  PreparePackage()
                              };

            var parser = new CommandLineBuilder(rootCommand)
                         .UseDefaults()
                         .Build();

            Command LocateAssembly()
            {
                return new Command(PackageToolConstants.LocateProjects)
                {
                    Handler = CommandHandler.Create(getAssembly)
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

    static class CommandLine
    {
        public static async Task<CommandLineResult> Execute(
            string command,
            string args,
            DirectoryInfo workingDir = null)
        {
            args = args ?? "";

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            using (var process = StartProcess(
                command,
                args,
                workingDir,
                output: data =>
                {
                    stdOut.AppendLine(data);
                },
                error: data =>
                {
                    stdErr.AppendLine(data);
                }))
            {
                var exitCode = await process.Complete();

                var output = stdOut.Replace("\r\n", "\n").ToString().Split('\n');

                var error = stdErr.Replace("\r\n", "\n").ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (error.All(string.IsNullOrWhiteSpace))
                {
                    error = null;
                }

                var result = new CommandLineResult(
                    exitCode: exitCode,
                    output: output,
                    error: error);

                return result;
            }
        }

        public static async Task<int> Complete(
            this Process process) =>
            await Task.Run(() =>
                      {
                          process.WaitForExit();

                          return process.ExitCode;
                      });

        public static Process StartProcess(
            string command,
            string args,
            DirectoryInfo workingDir,
            Action<string> output = null,
            Action<string> error = null,
            params (string key, string value)[] environmentVariables)
        {
            {
                args = args ?? "";

                var process = new Process
                {
                    StartInfo =
                    {
                        Arguments = args,
                        FileName = command,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        WorkingDirectory = workingDir?.FullName
                    }
                };

                if (environmentVariables?.Length > 0)
                {
                    foreach (var tuple in environmentVariables)
                    {
                        process.StartInfo.Environment.Add(tuple.key, tuple.value);
                    }
                }

                if (output != null)
                {
                    process.OutputDataReceived += (sender, eventArgs) =>
                    {
                        if (eventArgs.Data != null)
                        {
                            output(eventArgs.Data);
                        }
                    };
                }

                if (error != null)
                {
                    process.ErrorDataReceived += (sender, eventArgs) =>
                    {
                        if (eventArgs.Data != null)
                        {
                            error(eventArgs.Data);
                        }
                    };
                }

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                return process;
            }
        }
    }

    internal class CommandLineResult
    {
        private object exitCode;
        private string[] output;
        private string[] error;

        public CommandLineResult(object exitCode, string[] output, string[] error)
        {
            this.exitCode = exitCode;
            this.output = output;
            this.error = error;
        }
    }
}