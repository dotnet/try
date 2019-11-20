// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Tests
{
    public static class ObservableExtensions
    {
        public static SubscribedList<T> ToSubscribedList<T>(this IObservable<T> source)
        {
            return new SubscribedList<T>(source);
        }

        public static IObservable<JObject> TakeUntilCommandFailed(this IObservable<string> source, TimeSpan? timeout = null)
        {
            return source
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(JObject.Parse)
                .TakeUntilCommandFailed(timeout);
        }

        public static IObservable<JObject> TakeUntilCommandFailed(this IObservable<JObject> source, TimeSpan? timeout = null)
        {
            return source.TakeUntilEvent<CommandFailed>(timeout);
        }

        public static IObservable<JObject> TakeUntilCommandHandled(this IObservable<string> source, TimeSpan? timeout = null)
        {
            return source
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(JObject.Parse)
                .TakeUntilCommandHandled(timeout);
        }

        public static IObservable<JObject> TakeUntilEvent<T>(this IObservable<string> source, TimeSpan? timeout = null) where T : IKernelEvent
        {
            return source
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(JObject.Parse)
                .TakeUntilEvent<T>(timeout);
        }

        public static IObservable<JObject> TakeUntilEvent<T>(this IObservable<JObject> source, TimeSpan? timeout = null) where T : IKernelEvent
        {
            var termination = new Subject<Unit>();
            return source.TakeUntil(termination)
                .Do(e =>
                {
                    if (e["eventType"].Value<string>() == typeof(T).Name)
                    {
                        termination.OnNext(Unit.Default);
                    }
                })
                .Timeout(timeout ?? 10.Seconds());
        }

        public static IObservable<JObject> TakeUntilCommandHandled(this IObservable<JObject> source, TimeSpan? timeout = null)
        {
            return source.TakeUntilEvent<CommandHandled>(timeout);
        }

        public static IObservable<JObject> TakeUntilCommandParseFailure(this IObservable<string> source, TimeSpan? timeout = null)
        {
            return source.TakeUntilEvent<CommandParseFailure>(timeout);
        }

        public static IObservable<JObject> TakeUntilCommandParseFailure(this IObservable<JObject> source, TimeSpan? timeout = null)
        {
            return source.TakeUntilEvent<CommandParseFailure>(timeout);
        }
    }
}