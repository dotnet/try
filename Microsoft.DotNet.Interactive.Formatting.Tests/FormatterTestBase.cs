// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class FormatterTestBase : IDisposable
    {
        public FormatterTestBase()
        {
            Formatter.ResetToDefault();
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}