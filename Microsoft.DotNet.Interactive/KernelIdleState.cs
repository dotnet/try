// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Microsoft.DotNet.Interactive
{
    internal class KernelIdleState
    {
        private readonly BehaviorSubject<bool> _idleState = new BehaviorSubject<bool>(true);

        public IObservable<bool> IdleState => _idleState.DistinctUntilChanged();

        public void SetAsBusy() => _idleState.OnNext(false);

        public void SetAsIdle() => _idleState.OnNext(true);
    }
}