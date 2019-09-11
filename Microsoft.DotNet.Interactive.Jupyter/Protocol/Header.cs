// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class Header
    {
        [JsonProperty("msg_id")]
        public string MessageId { get;  }

        [JsonProperty("username")]
        public string Username { get; }

        [JsonProperty("session")]
        public string Session { get;  }

        [JsonProperty("date")]
        public string Date { get;  }

        [JsonProperty("msg_type")]
        public string MessageType { get;  }

        [JsonProperty("version")]
        public string Version { get; }

        public Header(string messageType, string messageId, string version, string session, string username, string date = null)
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(messageType));
            }

            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(messageId));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(version));
            }

            MessageType = messageType;
            MessageId = messageId;
            Version = version;
            Session = session;
            Username = username;
            Date = date;
        }

        public static Header Create(JupyterMessageContent messageContent, string session)
        {
            return Create(messageContent.MessageType, session);
        }

        public static Header Create(string messageType, string session)
        {
            var newHeader = new Header(
                messageType: messageType, 
                messageId: Guid.NewGuid().ToString(), 
                version: Constants.VERSION, 
                username: Constants.USERNAME, 
                session: session, 
                date: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            return newHeader;
        }
    }
}
