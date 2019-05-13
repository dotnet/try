// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Jupyter.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Try.Jupyter
{
    internal class RequestHandlerStatus : IRequestHandlerStatus
    {
        private readonly Header _requestHeader;
        private readonly MessageSender _messageSender;
        private readonly MessageBuilder _messageBuilder;

        public RequestHandlerStatus(Header requestHeader, MessageSender messageSender)
        {
            _requestHeader = requestHeader;
            _messageSender = messageSender;
            _messageBuilder = new MessageBuilder();
        }
        public void SetAsBusy()
        {
            SetStatus(StatusValues.Busy);
        }

        public void SetAsIdle()
        {
            SetStatus(StatusValues.Idle);
        }

        private void SetStatus(string status)
        {
            var content = new Status
            {
                ExecutionState = status
            };

            var statusMessage = _messageBuilder.CreateMessage(MessageTypeValues.Status, JObject.FromObject(content), _requestHeader);

            _messageSender.Send(statusMessage);
        }
    }
}