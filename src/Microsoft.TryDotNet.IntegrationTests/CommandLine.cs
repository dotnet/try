using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using CommandLineInvocationException = Microsoft.TryDotNet.IntegrationTests.CommandLineInvocationException;

public static class CommandLine
{
    public static Task<CommandLineResult> Execute(
        FileInfo exePath,
        string args,
        DirectoryInfo? workingDir = null,
        TimeSpan? timeout = null) =>
        Execute(exePath.FullName,
            args,
            workingDir,
            timeout);


    public static async Task<CommandLineResult> Execute(
        string command,
        string args,
        DirectoryInfo? workingDir = null,
        TimeSpan? timeout = null)
    {
        args ??= "";

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        using (var operation = ConfirmOnExit(command, args))
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
            var exitCode = await process.Complete().Timeout(timeout ?? TimeSpan.FromMinutes(1));

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

            if (exitCode == 0)
            {
                operation.Succeed(
                    "> {command} {args} -> exited with {code}",
                    process.StartInfo.FileName,
                    process.StartInfo.Arguments,
                    process.ExitCode);
            }
            else
            {
                operation.Fail(new CommandLineInvocationException(result));
            }

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

    public static async Task<T> Timeout<T>(
        this Task<T> source,
        TimeSpan timeout, 
        string? message = null)
    {
        if (await Task.WhenAny(
                source,
                Task.Delay(timeout)) != source)
        {
            throw string.IsNullOrWhiteSpace(message) ? new TimeoutException():  new TimeoutException(message);
        }

        return await source;
    }

    public static async Task Timeout(
        this Task source,
        TimeSpan timeout,
        string? message = null)
    {
        if (await Task.WhenAny(
                source,
                Task.Delay(timeout)) != source)
        {
            throw string.IsNullOrWhiteSpace(message) ? new TimeoutException() : new TimeoutException(message);
        }

        await source;
    }

    public static Process StartProcess(
        string command,
        string args,
        DirectoryInfo? workingDir,
        Action<string>? output = null,
        Action<string>? error = null,
        params (string key, string value)[] environmentVariables)
    {
        using (var operation = Log.OnEnterAndExit())
        {
            args ??= "";

            var process = new Process
            {
                StartInfo =
                {
                    Arguments = args,
                    FileName = command,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = workingDir?.FullName ?? string.Empty,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            operation.Info("> {process} {args}", command, args);

            if (environmentVariables?.Length > 0)
            {
                foreach (var tuple in environmentVariables)
                {
                    operation.Trace("Adding environment variable {tuple}", tuple);
                    process.StartInfo.Environment.Add(tuple.key, tuple.value);
                }
            }

            if (output is not null)
            {
                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data is not null)
                    {
                        output(eventArgs.Data);
                    }
                };
            }

            if (error is not null)
            {
                process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data is not null)
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

    public static string AppendArgs(this string initial, string? append = null) =>
        string.IsNullOrWhiteSpace(append)
            ? initial
            : $"{initial} {append}";

    private static ConfirmationLogger ConfirmOnExit(
        object command,
        string args,
        [CallerMemberName] string? operationName = null)
    {
        return new ConfirmationLogger(
            operationName: operationName ?? "",
            category: Log.Category,
            message: "> {command} {args}",
            args: [command, args],
            logOnStart: true);
    }

    private static Logger Log { get; } = new Logger(nameof(CommandLine));
}