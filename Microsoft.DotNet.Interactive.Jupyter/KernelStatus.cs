// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class KernelStatus : IKernelStatus
    {
        private readonly Header _requestHeader;
        private readonly MessageSender _messageSender;
        private readonly BehaviorSubject<bool> _idleState = new BehaviorSubject<bool>(true);

        public KernelStatus(Header requestHeader, MessageSender messageSender)
        {
            _requestHeader = requestHeader;
            _messageSender = messageSender;

            _idleState
                .DistinctUntilChanged()
                .Subscribe(value => SetStatus(value ? StatusValues.Idle : StatusValues.Busy));
        }

        public void SetAsBusy() => _idleState.OnNext(false);

        public void SetAsIdle() => _idleState.OnNext(true);

        public async Task Idle() => await _idleState.FirstAsync(value => value);

        private void SetStatus(string status)
        {
            var content = new Status(status);

            var statusMessage = Message.Create(content, _requestHeader);

            _messageSender.Send(statusMessage);
        }
    }
}