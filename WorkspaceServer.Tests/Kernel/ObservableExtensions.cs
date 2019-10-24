// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json.Linq;

namespace WorkspaceServer.Tests.Kernel
{
    public static class ObservableExtensions
    {
        public static IObservable<JObject> TakeUntilCommandFailed(this IObservable<JObject> source)
        {
            var termination = new Subject<Unit>();
            return source.TakeUntil(termination)
                .Do(e =>
                {
                    if (e["eventType"].Value<string>() == nameof(CommandFailed))
                    {
                        termination.OnNext(Unit.Default);
                    }
                });
        }

        public static IObservable<JObject> TakeUntilCommandHandled(this IObservable<JObject> source)
        {
            var termination = new Subject<Unit>();
            return source.TakeUntil(termination)
                .Do(e =>
                {
                    if (e["eventType"].Value<string>() == nameof(CommandHandled))
                    {
                        termination.OnNext(Unit.Default);
                    }
                });
        }
    }
}