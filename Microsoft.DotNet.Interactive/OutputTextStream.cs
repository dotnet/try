// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive
{
    internal class OutputTextStream : IOutputTextStream
    {
        private readonly TextWriter _output;

        public OutputTextStream(TextWriter output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Write(string text)
        {
            _output.WriteLine(text);
            _output.Flush();
        }
    }
}