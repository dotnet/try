// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CommandFailed : KernelEventBase
    {
        public CommandFailed(
            Exception exception,
            IKernelCommand command,
            string message = null) : base(command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Exception = exception;

            Message = string.IsNullOrWhiteSpace(message) ? exception.Message : message;
        }

        public CommandFailed(
            string message,
            IKernelCommand command) : this(null, command, message)
        {
        }

        [JsonConstructor]
        internal CommandFailed(string message)
        {
            Message = message;
        }

        public Exception Exception { get; }

        public string Message { get; }

        public override string ToString() => $"{base.ToString()}: {Message}";
    }
}