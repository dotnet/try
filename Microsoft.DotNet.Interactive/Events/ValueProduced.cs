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
            bool isReturnValue = false,
            IReadOnlyCollection<FormattedValue> formattedValues = null,
            string valueId= null,
            bool isUpdatedValue = false) : base(command)
        {
            Value = value;
            IsReturnValue = isReturnValue;
            FormattedValues = formattedValues;
            ValueId = valueId;
            IsUpdatedValue = valueId switch
            {
                null => false,
                _ => isUpdatedValue
            };
        }

        public object Value { get; }

        public bool IsReturnValue { get; }

        public IReadOnlyCollection<FormattedValue> FormattedValues { get; }

        public string ValueId { get; }

        public bool IsUpdatedValue { get; }
    }
}