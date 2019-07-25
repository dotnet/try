// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class PlainTextFormatter
    {
        public static ITypeFormatter Create(Type type)
        {
            return null;
        }

        public const string MimeType = "text/plain";
    }
}