// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.Extensions.Hosting;

namespace MLS.Agent
{
    public abstract class HostedService : IHostedService, IDisposable
    {
        private Task _executingTask;
        private Budget _budget;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _budget = new Budget(cancellationToken);

            _executingTask = ExecuteAsync(_budget);

            // If the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted
                       ? _executingTask
                       : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            _budget.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(
                _executingTask,
                Task.Delay(-1, cancellationToken));

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        protected async Task ExecuteAsync()
        {
            if (_budget.IsExceeded)
            {
                return;
            }

            using (SchedulerContext.Establish(_budget))
            {
                await Task.Yield();

                await ExecuteAsync(_budget);
            }
        }

        protected abstract Task ExecuteAsync(Budget budget);

        public void Dispose() => _budget?.Cancel();
    }
}
