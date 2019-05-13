// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Assent;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using NetMQ;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Try.Jupyter.Tests
{
    public class JupyterMessageContractTests
    {
        private readonly Configuration _configuration;

        public JupyterMessageContractTests()
        {
            _configuration = new Configuration()
                .UsingExtension("json");

            _configuration = _configuration.SetInteractive(true);
        }

        [Fact]
        public void KernelInfoReply_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var kernelInfoReply = new KernelInfoReply
                                  {
                                      ProtocolVersion = "5.3",
                                      Implementation = ".NET",
                                      ImplementationVersion = "0.0.3",
                                      LanguageInfo = new LanguageInfo
                                                     {
                                                         Name = "C#",
                                                         Version = typeof(string).Assembly.ImageRuntimeVersion.Substring(1),
                                                         MimeType = "text/x-csharp",
                                                         FileExtension = ".cs",
                                                         PygmentsLexer = "c#"
                                                     }
                                  };
            var header = new Header
                         {
                             Username = Constants.USERNAME,
                             Session = "test session",
                             MessageId = Guid.Empty.ToString(),
                             MessageType = MessageTypeValues.KernelInfoReply,
                             Date = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                             Version = "5.3"
                         };
            var replyMessage = new Message
                               {
                                   Header = header,
                                   Content = kernelInfoReply
                               };
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Execute_result_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var transient = new Dictionary<string, object> { { "display_id", "none" } };
            var output = "some result";
            var executeResult = new ExecuteResult
            {
                Data = new JObject
                {
                    { "text/html", output },
                    { "text/plain", output }
                },
                Transient = transient,
                ExecutionCount = 12
            };

            var header = new Header
            {
                Username = Constants.USERNAME,
                Session = "test session",
                MessageId = Guid.Empty.ToString(),
                MessageType = MessageTypeValues.ExecuteResult,
                Date = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Version = "5.3"
            };
            var replyMessage = new Message
            {
                Header = header,
                Content = executeResult
            };
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Display_data_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var transient = new Dictionary<string, object> { { "display_id", "none" } };
            var output = "some result";
            var displayData = new DisplayData()
            {
                Data = new JObject
                {
                    { "text/html", output },
                    { "text/plain", output }
                },
                Transient = transient
            };

            var header = new Header
            {
                Username = Constants.USERNAME,
                Session = "test session",
                MessageId = Guid.Empty.ToString(),
                MessageType = MessageTypeValues.DisplayData,
                Date = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Version = "5.3"
            };
            var replyMessage = new Message
            {
                Header = header,
                Content = displayData
            };
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Update_data_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var transient = new Dictionary<string, object> { { "display_id", "none" } };
            var output = "some result";
            var displayData = new UpdateDisplayData()
            {
                Data = new JObject
                {
                    { "text/html", output },
                    { "text/plain", output }
                },
                Transient = transient
            };

            var header = new Header
            {
                Username = Constants.USERNAME,
                Session = "test session",
                MessageId = Guid.Empty.ToString(),
                MessageType = MessageTypeValues.UpdateDisplayData,
                Date = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Version = "5.3"
            };
            var replyMessage = new Message
            {
                Header = header,
                Content = displayData
            };
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        private class TextSocket : IOutgoingSocket
        {
            readonly StringBuilder _buffer = new StringBuilder();

            public bool TrySend(ref Msg msg, TimeSpan timeout, bool more)
            {
                var decoded = SendReceiveConstants.DefaultEncoding.GetString(msg.Data);
                _buffer.AppendLine($"data: {decoded} more: {more}");
                return true;
            }

            public string GetEncodedMessage()
            {
                return _buffer.ToString();
            }
        }
    }
}