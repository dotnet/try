// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class JupyterMessage
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

        public JupyterMessage(Header header,
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

        public static JupyterMessage Create<T>(
            T content,
            Header parentHeader = null,
            IReadOnlyList<IReadOnlyList<byte>> identifiers = null,
            string signature = null)
            where T : JupyterMessageContent
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var session = parentHeader?.Session ?? Guid.NewGuid().ToString();

            var message = new JupyterMessage(Header.Create(content, session), parentHeader: parentHeader, content: content, identifiers: identifiers, signature: signature);


            return message;
        }

        public static JupyterMessage CreateReply<T>(
            T content,
            JupyterMessage request)
            where T : JupyterReplyMessageContent
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request.Content is JupyterRequestMessageContent) && request.Content != JupyterMessageContent.Empty)
            {
                throw new ArgumentOutOfRangeException($"{request.Content.GetType()} is not a valid {nameof(JupyterRequestMessageContent)}");
            }

            var replyMessage = Create(content, request.Header, request.Identifiers, request.Signature);

            return replyMessage;
        }

        public static JupyterMessage CreatePubSub<T>(
            T content,
            JupyterMessage request, 
            string kernelIdentity = null)
            where T : JupyterPubSubMessageContent
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request.Content is JupyterRequestMessageContent) && request.Content != JupyterMessageContent.Empty)
            {
                throw new ArgumentOutOfRangeException($"{request.Content.GetType()} is nor a valid {nameof(JupyterRequestMessageContent)}");
            }

            var replyMessage = Create(content, request.Header, new[] { Topic(content, kernelIdentity) }, request.Signature);

            return replyMessage;
        }


        private static byte[] Topic<T>(T content, string kernelIdentity) where T : JupyterPubSubMessageContent
        {
            byte[] encodedTopic;
            var name = content.GetType().Name;
            switch (name)
            {

                case nameof(Status):
                    {
                        var fullTopic = GenerateFullTopic("status");
                        encodedTopic = Encoding.Unicode.GetBytes(fullTopic);
                    }
                    break;

                case nameof(ExecuteInput):
                    {
                        var fullTopic = GenerateFullTopic("execute_input");
                        encodedTopic = Encoding.Unicode.GetBytes(fullTopic);
                    }
                    break;

                case nameof(DisplayData):
                case nameof(UpdateDisplayData):
                    encodedTopic = Encoding.Unicode.GetBytes("display_data");
                    break;

                case nameof(ExecuteResult):
                    encodedTopic = Encoding.Unicode.GetBytes("execute_result");
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"type {name} is not supported");

            }

            string GenerateFullTopic(string topic)
            {
                if (kernelIdentity == null)
                {
                    throw new ArgumentNullException(nameof(kernelIdentity));
                }
                return $"kernel.{kernelIdentity}.{topic}";
            }

            return encodedTopic;
        }
    }
}