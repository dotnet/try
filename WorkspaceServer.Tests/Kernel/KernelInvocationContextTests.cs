// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class KernelInvocationContextTests
    {
        [Fact]
        public async Task Current_differs_per_async_context()
        {
            var barrier = new Barrier(2);

            IKernelCommand commandInTask1 = null;

            IKernelCommand commandInTask2 = null;

            await Task.Run(() =>
            {
                using (var x = KernelInvocationContext.Establish(new SubmitCode("")))
                {
                    barrier.SignalAndWait(1000);
                    commandInTask1 = KernelInvocationContext.Current.Command;
                }
            });

            await Task.Run(() =>
            {
                using (KernelInvocationContext.Establish(new SubmitCode("")))
                {
                    barrier.SignalAndWait(1000);
                    commandInTask2 = KernelInvocationContext.Current.Command;
                }
            });

            commandInTask1.Should()
                          .NotBe(commandInTask2)
                          .And
                          .NotBeNull();
        }
    }
}