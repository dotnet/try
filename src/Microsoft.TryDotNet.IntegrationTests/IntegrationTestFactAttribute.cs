// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests
{
    internal class IntegrationTestFactAttribute : FactAttribute
    {
        private const string EnvironmentVariableName = "RunIntegrationTests";

        public IntegrationTestFactAttribute(string? skipReason = null)
        {
            var variableValue = Environment.GetEnvironmentVariable(EnvironmentVariableName) ?? "false";
            switch (variableValue.ToLowerInvariant())
            {
                case "1":
                case "true":
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Skip = string.IsNullOrWhiteSpace(skipReason) ? "Ignored on Linux" : skipReason;
                    }
                    break;
                default:
                    Skip = $"Skipping integration tests because environment variable '{EnvironmentVariableName}' was not 'true' or '1'.";
                    break;
            }
        }
    }
}
