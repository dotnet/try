// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive
{
    public class FormattedValue
    {
        public FormattedValue(string mimeType, object value)
        {
            MimeType = mimeType;
            Value = value;
        }

        public string MimeType { get; }

        public object Value { get; }
    }
}