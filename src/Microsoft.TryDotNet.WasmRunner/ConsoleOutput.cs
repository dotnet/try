﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Reactive.Disposables;

namespace Microsoft.TryDotNet.WasmRunner;

internal static class ConsoleOutput
{
    private static readonly object _systemConsoleSwapLock = new();

    private static MultiplexingTextWriter _multiplexingOutputWriter;
    private static MultiplexingTextWriter _multiplexingErrorWriter;
    private static TextWriter _originalOutputWriter;
    private static TextWriter _originalErrorWriter;

    private static int _refCount = 0;

    public static IDisposable Subscribe(Func<ObservableConsole, IDisposable> subscribe)
    {

        lock (_systemConsoleSwapLock)
        {

            if (++_refCount == 1)
            {
                _originalOutputWriter = Console.Out;
                _originalErrorWriter = Console.Error;
                _multiplexingOutputWriter = new MultiplexingTextWriter("out");
                _multiplexingErrorWriter = new MultiplexingTextWriter("err");
                Console.SetOut(_multiplexingOutputWriter);
                Console.SetError(_multiplexingErrorWriter);
            }
        }

        var outWriterForContext = _multiplexingOutputWriter.EnsureInitializedForCurrentAsyncContext();
        var errWriterForContext = _multiplexingErrorWriter.EnsureInitializedForCurrentAsyncContext();

        var obsConsole = new ObservableConsole(
            _multiplexingOutputWriter.GetObservable(),
            _multiplexingErrorWriter.GetObservable());

        return new CompositeDisposable(
            subscribe(obsConsole),
            outWriterForContext,
            errWriterForContext,
            Disposable.Create(() =>
            {
                lock (_systemConsoleSwapLock)
                {
                    if (--_refCount == 0)
                    {
                        Console.SetOut(_originalOutputWriter);
                        Console.SetError(_originalErrorWriter);
                        _multiplexingOutputWriter.Dispose();
                        _multiplexingOutputWriter = null;
                        _multiplexingErrorWriter.Dispose();
                        _multiplexingErrorWriter = null;
                    }
                }
            }));
    }

    public record ObservableConsole(
        IObservable<string> Out,
        IObservable<string> Error);
}