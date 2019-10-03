// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WorkspaceServer.Servers.Roslyn
{
    public class ConsoleOutput : IDisposable
    {
        private TextWriter _originalOutputWriter;
        private TextWriter _originalErrorWriter;
        private readonly TrackingStringWriter _outputWriter = new TrackingStringWriter();
        private readonly TrackingStringWriter _errorWriter = new TrackingStringWriter();

        private const int NOT_DISPOSED = 0;
        private const int DISPOSED = 1;

        private int _alreadyDisposed = NOT_DISPOSED;

        private static readonly SemaphoreSlim _consoleLock = new SemaphoreSlim(1, 1);

        private ConsoleOutput()
        {
        }

        public static async Task<ConsoleOutput> Capture()
        {
            var redirector = new ConsoleOutput();
            await _consoleLock.WaitAsync();

            try
            {
                redirector._originalOutputWriter = Console.Out;
                redirector._originalErrorWriter = Console.Error;

                Console.SetOut(redirector._outputWriter);
                Console.SetError(redirector._errorWriter);
            }
            catch
            {
                _consoleLock.Release();
                throw;
            }

            return redirector;
        }

        public IDisposable SubscribeToStandardOutput(Action<string> action)
        {
            return _outputWriter.Subscribe(action);
        }

        public IDisposable SubscribeToStandardError(Action<string> action)
        {
            return _errorWriter.Subscribe(action);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _alreadyDisposed, DISPOSED, NOT_DISPOSED) == NOT_DISPOSED)
            {
                if (_originalOutputWriter != null)
                {
                    Console.SetOut(_originalOutputWriter);
                }
                if (_originalErrorWriter != null)
                {
                    Console.SetError(_originalErrorWriter);
                }

                _consoleLock.Release();
            }
        }
       
        public string StandardOutput => _outputWriter.ToString();

        public string StandardError => _errorWriter.ToString();

        public void Clear()
        {
            _outputWriter.GetStringBuilder().Clear();
            _errorWriter.GetStringBuilder().Clear();
        }

        public bool IsEmpty() => _outputWriter.ToString().Length == 0 && _errorWriter.ToString().Length == 0;
     
    }
}
