// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using NetMQ;
using Recipes;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class MessageSender : IMessageSender
    {
        private readonly IOutgoingSocket _socket;
        private readonly SignatureValidator _signatureValidator;

        public MessageSender(IOutgoingSocket socket, SignatureValidator signatureValidator)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        }

        public bool Send(Message message)
        {
            var hmac = _signatureValidator.CreateSignature(message);

            foreach (var ident in message.Identifiers)
            {
                _socket.TrySendFrame(ident, true);
            }

            Send(Constants.DELIMITER, _socket);
            Send(hmac, _socket);
            Send(message.Header.ToJson(), _socket);
            Send(message.ParentHeader.ToJson(), _socket);
            Send(message.MetaData.ToJson(), _socket);
            Send(message.Content.ToJson(), _socket, false);

            return true;
        }

        private static void Send(string message, IOutgoingSocket socket, bool sendMore = true)
        {
            socket.SendFrame(message, sendMore);
        }
    }
}