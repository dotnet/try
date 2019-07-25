// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueProduced : KernelEventBase
    {
        public ValueProduced(object value,
            IKernelCommand command,
            bool isLastValue = false,
            IReadOnlyCollection<FormattedValue> formattedValues = null) : base(command)
        {
            Value = value;
            IsLastValue = isLastValue;
            FormattedValues = formattedValues;
        }

        public object Value { get; }
        public bool IsLastValue { get; }

        public IReadOnlyCollection<FormattedValue> FormattedValues { get; }
    }
}