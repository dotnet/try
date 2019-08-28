﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands
{
    public class DisplayValue : KernelCommandBase
    {
        public DisplayValue(FormattedValue formattedValue, string valueId = null)
        {
            FormattedValue = formattedValue;
            ValueId = valueId;
        }

        public FormattedValue FormattedValue { get; }

        public string ValueId { get; }
    }
}