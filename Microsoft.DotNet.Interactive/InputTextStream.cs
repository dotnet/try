// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    internal class InputTextStream : IInputTextStream
    {
        private readonly TextReader _input;
        private readonly Subject<string> _channel = new Subject<string>();

        public InputTextStream(TextReader input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }
        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _channel.Subscribe(observer);
        }

        public Task Start(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var line = await _input.ReadLineAsync();
                    if (line == null)
                    {
                        await Task.Delay(100, token);
                    }
                    else
                    {
                        _channel.OnNext(line);
                    }
                }

                _channel.OnCompleted();
            }, token);
        }
    }
}