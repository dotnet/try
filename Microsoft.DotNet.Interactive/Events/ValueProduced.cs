﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueProduced : KernelEventBase
    {
        public ValueProduced(
            object value,
            IKernelCommand command = null,
            bool isReturnValue = false,
            IReadOnlyCollection<FormattedValue> formattedValues = null,
            string valueId = null,
            bool isUpdatedValue = false) : base(command)
        {
            if (isUpdatedValue && valueId == null)
            {
                throw new ArgumentException($"{nameof(isUpdatedValue)} cannot be true with a null {nameof(valueId)}", nameof(valueId));
            }

            Value = value;
            IsReturnValue = isReturnValue;
            FormattedValues = formattedValues ?? Array.Empty<FormattedValue>();
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