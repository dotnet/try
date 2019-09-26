// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using NetMQ;
using Recipes;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
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

        public void Send(JupyterMessage jupyterMessage)
        {
            var hmac = _signatureValidator.CreateSignature(jupyterMessage);

            if (jupyterMessage.Identifiers != null)
            {
                foreach (var ident in jupyterMessage.Identifiers)
                {
                    _socket.TrySendFrame(ident.ToArray(), true);
                }
            }

            Send(Constants.DELIMITER, _socket);
            Send(hmac, _socket);
            Send(jupyterMessage.Header.ToJson(), _socket);
            Send((jupyterMessage.ParentHeader?? new object()).ToJson(), _socket);
            Send(jupyterMessage.MetaData.ToJson(), _socket);
            Send(jupyterMessage.Content.ToJson(), _socket, false);
        }

        private static void Send(string message, IOutgoingSocket socket, bool sendMore = true)
        {
            socket.SendFrame(message, sendMore);
        }
    }
}