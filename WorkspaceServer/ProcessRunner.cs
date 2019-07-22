// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using WorkspaceServer.Kernel;

namespace WorkspaceServer
{
    public class ProcessRunner : IObservableRunner
    {
        private int _isRunning = 0;
        private readonly Subject<IKernelEvent> _channel;
        private readonly Process _process;
        public IObservable<IKernelEvent> KernelEvents { get; }

        public ProcessRunner(
            string command,
            string args = null,
            DirectoryInfo workingDir = null,
            params (string key, string value)[] environmentVariables)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
            }


            _process = new Process
            {
                StartInfo =
                {
                    Arguments = args ?? string.Empty,
                    FileName = command,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = workingDir?.FullName ?? string.Empty,
                    CreateNoWindow = false,
                    ErrorDialog = false,
                    StandardOutputEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            if (environmentVariables?.Length > 0)
            {
                foreach (var (key, value) in environmentVariables)
                {
                    _process.StartInfo.Environment.Add(key, value);
                }
            }

            _channel = new Subject<IKernelEvent>();
            KernelEvents = Observable
                .Create<IKernelEvent>(_channel.Subscribe)
                .Publish()
                .RefCount();
        }

        public Task StartAsync()
        {
            var prev = Interlocked.CompareExchange(ref _isRunning, 1, 0);
            switch (prev)
            {
                case 0:
                    _channel.OnNext(new Started());
                    try
                    {
                        _process.Exited += (sender, args) =>
                        {
                            if (_process.ExitCode == 0)
                            {
                                _channel.OnCompleted();
                            }
                            else
                            {
                                _channel.OnError(new KernelException($"process exited with code {_process.ExitCode}"));
                            }
                        };

                        _process.OutputDataReceived += (sender, eventArgs) =>
                        {
                            if (eventArgs.Data != null)
                            {
                                _channel.OnNext(new StandardOutputReceived(eventArgs.Data));
                            }
                        };

                        _process.ErrorDataReceived += (sender, eventArgs) =>
                        {
                            if (eventArgs.Data != null)
                            {
                                _channel.OnNext(new StandardErrorReceived(eventArgs.Data));
                            }
                        };


                        _process.Start();
                        _process.BeginOutputReadLine();
                        _process.BeginErrorReadLine();
                    }
                    catch (Exception e)
                    {
                        _channel.OnError(e);
                    }

                    return Task.CompletedTask;
                default:
                    throw new InvalidOperationException("Compute already started");
            }
        }

        public Task StopAsync()
        {
            var prev = Interlocked.CompareExchange(ref _isRunning, 0, 1);
            switch (prev)
            {
                case 1:
                    _channel.OnNext(new Stopped());
                    if (_process.HasExited)
                    {
                        _channel.OnCompleted();
                    }
                    else
                    {
                        _process.Kill();
                        _channel.OnCompleted();
                        return Task.Run(() => { _process.WaitForExit(); });
                    }
                    return Task.CompletedTask;
                default:
                    throw new InvalidOperationException("Compute already stopped");
            }
        }
    }
}