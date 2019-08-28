// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueUpdated : KernelEventBase
    {
        public ValueUpdated(
            object value,
            string valueId,
            IKernelCommand command = null,
            IReadOnlyCollection<FormattedValue> formattedValues = null) : base(command)
        {
            if (string.IsNullOrWhiteSpace(valueId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(valueId));
            }

            Value = value;
            FormattedValues = formattedValues ?? Array.Empty<FormattedValue>();
            ValueId = valueId;
           
        }

        public object Value { get; }

        public IReadOnlyCollection<FormattedValue> FormattedValues { get; }

        public string ValueId { get; }
    }
}