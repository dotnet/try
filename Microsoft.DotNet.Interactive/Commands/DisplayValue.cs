// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class DisplayValue : KernelCommandBase
    {
        public DisplayValue(object value, FormattedValue formattedValue, string valueId = null)
        {
            Value = value;
            FormattedValue = formattedValue;
            ValueId = valueId;
        }

        public object Value { get; }

        public FormattedValue FormattedValue { get; }

        public string ValueId { get; }
    }
}