// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands
{
    public class UpdateDisplayValue : KernelCommandBase
    {
        public UpdateDisplayValue(FormattedValue formattedValue, string displayId = null)
        {
            FormattedValue = formattedValue;
            DisplayId = displayId;
        }

        public FormattedValue FormattedValue { get; }
        public string DisplayId { get; }
    }
}