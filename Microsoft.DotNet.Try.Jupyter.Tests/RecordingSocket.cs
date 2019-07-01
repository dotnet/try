// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using NetMQ;

namespace Microsoft.DotNet.Try.Jupyter.Tests
{
    internal class RecordingSocket : IOutgoingSocket
    {
        public List<Msg> Messages { get; } = new List<Msg>();

        public bool TrySend(ref Msg msg, TimeSpan timeout, bool more)
        {
            Messages.Add(msg);
            return true;
        }
     
    }
}