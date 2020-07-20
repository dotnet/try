// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLS.Agent.CommandLine
{
    internal class VerifyResult
    {
        public VerifyResult(int errorCount)
        {
            ErrorCount = errorCount;
        }

        public int ErrorCount { get;  }
    }
}