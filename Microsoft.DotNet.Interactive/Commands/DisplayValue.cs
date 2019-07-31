// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands
{
    public class DisplayValue : KernelCommandBase
    {
        public DisplayValue(FormattedValue formattedValue)
        {
            FormattedValue = formattedValue;
        }

        public FormattedValue FormattedValue { get; }
    }
}