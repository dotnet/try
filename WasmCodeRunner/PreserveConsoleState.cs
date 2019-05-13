using System;
using System.IO;

namespace MLS.WasmCodeRunner
{
    public class PreserveConsoleState : IDisposable
    {
        private readonly TextWriter _originalConsoleOut;
        private readonly TextWriter _originalConsoleStdErr;

        public PreserveConsoleState()
        {
            _originalConsoleOut = Console.Out;
            _originalConsoleStdErr = Console.Error;
        }

        public bool OutputIsRedirected => Console.Out != _originalConsoleOut || Console.Error != _originalConsoleStdErr;

        public void Dispose()
        {
            Console.SetOut(_originalConsoleOut);
            Console.SetError(_originalConsoleStdErr);
        }
    }
}