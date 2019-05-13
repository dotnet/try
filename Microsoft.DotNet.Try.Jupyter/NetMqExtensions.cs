// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using NetMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Try.Jupyter
{
    public  static class NetMQExtensions
    {
        public static Message GetMessage(this NetMQSocket socket)
        {
            var message = new Message();

            // There may be additional ZMQ identities attached; read until the delimiter <IDS|MSG>"
            // and store them in message.identifiers
            // http://ipython.org/ipython-doc/dev/development/messaging.html#the-wire-protocol
            var delimiterAsBytes = Encoding.ASCII.GetBytes(Constants.DELIMITER);
            while (true)
            {
                var delimiter = socket.ReceiveFrameBytes();
                if (delimiter.SequenceEqual(delimiterAsBytes))
                {
                    break;
                }
                message.Identifiers.Add(delimiter);
            }

            // Getting Hmac
            message.Signature = socket.ReceiveFrameString();
           
            // Getting Header
            var header = socket.ReceiveFrameString();
            
            message.Header = JsonConvert.DeserializeObject<Header>(header);

            // Getting parent header
            var parentHeader = socket.ReceiveFrameString();
           
            message.ParentHeader = JsonConvert.DeserializeObject<Header>(parentHeader);

            // Getting metadata
            var metadata = socket.ReceiveFrameString();
          
            message.MetaData = JObject.Parse(metadata);

            // Getting content
            var content = socket.ReceiveFrameString();
           
            message.Content = JObject.Parse(content);

            return message;
        }
    }
}