// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public static class CommandLineBuilderExtensions
    {
        public static CommandLineBuilder ResponseFile(
            this CommandLineBuilder builder,
            ResponseFileHandling responseFileHandling)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ResponseFileHandling = responseFileHandling;

            return builder;
        }
    }
}
