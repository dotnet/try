// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using MLS.Agent.Tools.Tests;
using Newtonsoft.Json.Linq;
using Recipes;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class KernelStatusTests
    {
        private readonly KernelStatus _kernelStatus;
        private readonly RecordingSocket _recordingSocket = new RecordingSocket();

        public KernelStatusTests()
        {
            _kernelStatus = new KernelStatus(
                Header.Create(
                    typeof(ExecuteRequest), "test"),
                new MessageSender(_recordingSocket, new SignatureValidator("key", "HMACSHA256")
                ));
        }

        [Fact]
        public void When_idle_then_awaiting_idle_returns_immediately()
        {
            var task = _kernelStatus.Idle();

            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task When_not_idle_then_idle_returns_after_SetAsIdle_is_called()
        {
            _kernelStatus.SetAsBusy();

            var task = _kernelStatus.Idle();

            task.IsCompleted.Should().BeFalse();

            await Task.WhenAll(
                Task.Run(() => _kernelStatus.SetAsIdle()),
                task).Timeout(3.Seconds());
        }

        [Fact]
        public void Status_message_is_only_sent_on_state_change()
        {
            _kernelStatus.SetAsBusy();
            _kernelStatus.SetAsBusy();
            _kernelStatus.SetAsIdle();
            _kernelStatus.SetAsIdle();
            _kernelStatus.SetAsBusy();

            _recordingSocket
                .DecodedMessages
                .Where(m => m.StartsWith("{"))
                .Select(JObject.Parse)
                .SelectMany(jobj => jobj.Properties()
                                        .Where(p => p.Name == "execution_state")
                                        .Select(p => p.Value.Value<string>()))
                .Should()
                .BeEquivalentSequenceTo("idle", "busy", "idle", "busy");
        }
    }
}