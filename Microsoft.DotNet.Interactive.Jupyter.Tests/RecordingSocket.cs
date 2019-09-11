// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using NetMQ;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class RecordingSocket : IOutgoingSocket
    {
        public List<string> DecodedMessages { get; } = new List<string>();

        public bool TrySend(ref Msg msg, TimeSpan timeout, bool more)
        {
            DecodedMessages.Add(SendReceiveConstants.DefaultEncoding.GetString(msg.Data));
            return true;
        }
     
    }
}