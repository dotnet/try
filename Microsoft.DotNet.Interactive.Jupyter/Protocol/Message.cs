// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
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

        public static Message Create(JupyterMessageContent content, Header parentHeader, IReadOnlyList<IReadOnlyList<byte>> identifiers = null, string signature = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var messageType = JupyterMessageContent.GetMessageType(content);
            var session = parentHeader?.Session ?? Guid.NewGuid().ToString();

            var message = new Message(Header.Create(messageType, session), parentHeader: parentHeader, content: content, identifiers: identifiers, signature: signature);


            return message;
        }

        public static Message CreateResponse(JupyterMessageContent content,
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

            var replyMessage = Create(content, request.Header, request.Identifiers, request.Signature);

            return replyMessage;
        }

    }
}