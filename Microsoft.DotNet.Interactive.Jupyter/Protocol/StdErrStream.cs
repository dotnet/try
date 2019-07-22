// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.Stream)]
    public class StdErrStream : Stream
    {
        public StdErrStream(string text) : base("stderr", text)
        {
        }
    }
}