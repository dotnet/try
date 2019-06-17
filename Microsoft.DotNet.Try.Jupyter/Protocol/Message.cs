// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class Message
    {
        [JsonIgnore]
        public IReadOnlyList<IReadOnlyList<byte>> Identifiers { get; }

        [JsonIgnore]
        public string Signature { get; }

        [JsonProperty("header")]
        public Header Header { get; }

        [JsonProperty("parent_header")]
        public Header ParentHeader { get; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public JupyterMessageContent Content { get; }

        [JsonProperty("buffers")]
        public IReadOnlyList<IReadOnlyList<byte>> Buffers { get; }

        public Message(Header header,
            JupyterMessageContent content = null,
            Header parentHeader = null,
            string signature = null,
            IReadOnlyDictionary<string, object> metaData = null,
            IReadOnlyList<IReadOnlyList<byte>> identifiers = null,
            IReadOnlyList<IReadOnlyList<byte>> buffers = null)
        {
            Header = header;
            ParentHeader = parentHeader;
            Buffers = buffers ?? new List<IReadOnlyList<byte>>();
            Identifiers = identifiers ?? new List<IReadOnlyList<byte>>();
            MetaData = metaData ?? new Dictionary<string, object>();
            Content = content ?? JupyterMessageContent.Empty;
            Signature = signature ?? string.Empty;
        }

        public static Message CreateMessage(JupyterMessageContent content, Header parentHeader, IReadOnlyList<IReadOnlyList<byte>> identifiers = null, string signature = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var messageType = GetMessageType(content);
            var session = parentHeader.Session;

            var message = new Message(CreateHeader(messageType, session), parentHeader: parentHeader, content: content, identifiers: identifiers, signature: signature);


            return message;
        }

        public static Message CreateResponseMessage(JupyterMessageContent content,
            Message request)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var replyMessage = CreateMessage(content, request.Header, request.Identifiers, request.Signature);

            return replyMessage;
        }

        private static string GetMessageType(JupyterMessageContent source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var attribute = source.GetType().GetCustomAttribute<JupyterMessageTypeAttribute>() ?? throw new InvalidOperationException("source is not annotated with JupyterMessageTypeAttribute");

            return attribute.Type;
        }
        private static Header CreateHeader(string messageType, string session)
        {
            var newHeader = new Header(messageType: messageType, messageId: Guid.NewGuid().ToString(), version: "5.3", username: Constants.USERNAME, session: session, date: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            return newHeader;
        }
    }
}