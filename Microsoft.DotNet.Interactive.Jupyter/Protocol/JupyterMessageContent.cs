// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public abstract class JupyterMessageContent
    {
        private static readonly IReadOnlyDictionary<string, Type> _messageTypeToClrType;
        private static readonly IReadOnlyDictionary<Type, string> _clrTypeToMessageType;

        private string _messageType;

        static JupyterMessageContent()
        {
            var messageImplementations = typeof(JupyterMessageContent).Assembly.GetExportedTypes().Where(t =>
                t.IsAbstract == false && typeof(JupyterMessageContent).IsAssignableFrom(t)).ToList();

            var messageTypeToClrType = new Dictionary<string, Type>();
            var clrTypeToMessageType = new Dictionary<Type, string>();
            foreach (var messageImplementation in messageImplementations)
            {
                var messageType = messageImplementation.GetCustomAttribute<JupyterMessageTypeAttribute>(true);
                if (messageType != null)
                {
                    messageTypeToClrType[messageType.Type] = messageImplementation;
                    clrTypeToMessageType[messageImplementation] = messageType.Type;
                }
            }

            _messageTypeToClrType = messageTypeToClrType;
            _clrTypeToMessageType = clrTypeToMessageType;
        }

        [JsonIgnore]
        public string MessageType => _messageType ?? (_messageType  = _clrTypeToMessageType[GetType()]);

        public static JupyterMessageContent FromJsonString(string jsonString, string messageType)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(jsonString));
            }

            if (string.IsNullOrWhiteSpace(messageType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(messageType));
            }

            if (!_messageTypeToClrType.ContainsKey(messageType))
            {
                throw new ArgumentOutOfRangeException(nameof(jsonString), $"Message type {messageType} is not supported.");
            }

            return TryFromJsonString(jsonString,messageType);
        }

        public static JupyterMessageContent TryFromJsonString(string jsonString, string messageType)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(jsonString));
            }

            if (string.IsNullOrWhiteSpace(messageType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(messageType));
            }

            if (_messageTypeToClrType.TryGetValue(messageType, out var supportedType))
            {
                return JsonConvert.DeserializeObject(jsonString, supportedType) as JupyterMessageContent;
            }

            return Empty;
        }


        public static JupyterMessageContent Empty { get; } = new EmptyMessageContent();

        public static string GetMessageType(JupyterMessageContent source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return GetMessageType(source.GetType());
        }

        public static string GetMessageType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var attribute = type.GetCustomAttribute<JupyterMessageTypeAttribute>() ?? throw new InvalidOperationException($"{type.Name} is not annotated with JupyterMessageTypeAttribute");

            return attribute.Type;
        }

        private class EmptyMessageContent : JupyterMessageContent
        {

        }
    }
}