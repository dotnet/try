// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class TerminalFormatter<T> : TypeFormatter<T>
    {
        public override void Format(T instance, TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string MimeType => TerminalFormatter.MimeType;
    }
}