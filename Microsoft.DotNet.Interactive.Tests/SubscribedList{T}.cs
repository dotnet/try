// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class SubscribedList<T> : IReadOnlyList<T>, IDisposable
    {
        private readonly List<T> _list = new List<T>();
        private readonly IDisposable _subscription;

        public SubscribedList(IObservable<T> source)
        {
            _subscription = source.Subscribe(x => _list.Add(x));
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _list.Count;

        public T this[int index] => _list[index];

        public void Dispose() => _subscription.Dispose();
    }
}