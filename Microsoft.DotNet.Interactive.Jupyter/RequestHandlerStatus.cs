// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal class RequestHandlerStatus : IRequestHandlerStatus
    {
        private readonly Header _requestHeader;
        private readonly MessageSender _messageSender;

        public RequestHandlerStatus(Header requestHeader, MessageSender messageSender)
        {
            _requestHeader = requestHeader;
            _messageSender = messageSender;
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
            var content = new Status(status);

            var statusMessage = Message.Create(content, _requestHeader);

            _messageSender.Send(statusMessage);
        }
    }
}